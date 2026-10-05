using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using IpwBridge.Exceptions;
using IpwBridge.Models;
using IpwBridge.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IpwBridge.Services;

/// <summary>Describes one HTTP request to the Metazo API.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Url">The absolute URL, including token and checksum.</param>
/// <param name="Endpoint">The endpoint name used in logs and exception messages (never the URL).</param>
/// <param name="Timeout">The timeout for the whole exchange, including reading the body.</param>
/// <param name="Retryable">
/// Whether transient failures may be retried (only for idempotent requests). Retries only happen before a
/// successful response's body is read, so nothing is consumed or written twice.
/// </param>
internal sealed record MetazoRequest(HttpMethod Method, string Url, string Endpoint, TimeSpan Timeout, bool Retryable)
{
    /// <summary>Creates the request body; called once per attempt. <see langword="null"/> for no body.</summary>
    public Func<HttpContent>? ContentFactory { get; init; }

    /// <summary>Extra request headers.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Sends requests to the Metazo API: per-request timeouts that cover the body, retries with backoff for
/// transient failures of idempotent requests, and translation of every failure into an IpwBridge exception.
/// </summary>
internal sealed class ApiRequestSender(
    IHttpClientFactory httpClientFactory,
    IOptions<MetazoApiOptions> options,
    MetazoJsonSerializer serializer,
    IpwBridgeClock clock,
    ILogger<ApiRequestSender> logger)
{
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    private readonly MetazoApiOptions _options = options.Value;

    /// <summary>Sends the request and deserializes the JSON response.</summary>
    public Task<T> SendJsonAsync<T>(MetazoRequest request, CancellationToken cancellationToken)
        => SendAsync(request, (response, ct) => ReadJsonAsync<T>(request, response, ct), cancellationToken);

    /// <summary>Sends the request and returns the raw response body.</summary>
    public Task<byte[]> SendForBytesAsync(MetazoRequest request, CancellationToken cancellationToken)
        => SendAsync(request, async (response, ct) =>
        {
            byte[] body = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            if (IsJson(response))
            {
                ThrowIfUnsuccessful(request, response, MetazoEnvelope.TryRead(StripBom(body)));
            }

            return body;
        }, cancellationToken);

    /// <summary>Sends the request and copies the response body to <paramref name="destination"/>.</summary>
    public Task SendToStreamAsync(MetazoRequest request, Stream destination, CancellationToken cancellationToken)
        => SendAsync(request, async (response, ct) =>
        {
            // A small JSON answer to a binfile request may be an error with status 200, so it is checked before
            // anything reaches the destination. Error bodies are far below MaxErrorReadLength; anything larger
            // (or not JSON) is a file and is streamed as it arrives.
            Stream source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using (source.ConfigureAwait(false))
            {
                if (IsJson(response) && !(response.Content.Headers.ContentLength > Constants.MaxErrorReadLength))
                {
                    byte[] prefix = new byte[Constants.MaxErrorReadLength + 1];
                    int read = await source.ReadAtLeastAsync(prefix, prefix.Length, throwOnEndOfStream: false, ct).ConfigureAwait(false);
                    if (read <= Constants.MaxErrorReadLength)
                    {
                        ThrowIfUnsuccessful(request, response, MetazoEnvelope.TryRead(StripBom(prefix.AsSpan(0, read))));
                    }

                    await WriteToDestinationAsync(request, () => destination.WriteAsync(prefix.AsMemory(0, read), ct)).ConfigureAwait(false);
                }

                await CopyToDestinationAsync(request, source, destination, ct).ConfigureAwait(false);
            }

            return true;
        }, cancellationToken);

    private async Task<TResult> SendAsync<TResult>(
        MetazoRequest request,
        Func<HttpResponseMessage, CancellationToken, Task<TResult>> readSuccess,
        CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient(Constants.HttpClientName);
        int maxAttempts = request.Retryable ? _options.MaxRetryAttempts + 1 : 1;

        for (int attempt = 1; ; attempt++)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (request.Timeout != Timeout.InfiniteTimeSpan)
            {
                timeout.CancelAfter(request.Timeout);
            }

            string? transientReason = null;
            TimeSpan? retryAfter = null;
            long started = Stopwatch.GetTimestamp();

            try
            {
                using HttpRequestMessage message = new(request.Method, request.Url);
                if (request.ContentFactory is not null)
                {
                    message.Content = request.ContentFactory();
                }

                foreach (var (name, value) in request.Headers ?? new Dictionary<string, string>())
                {
                    message.Headers.TryAddWithoutValidation(name, value);
                }

                Log.SendingRequest(logger, request.Method.Method, request.Endpoint, attempt);
                using HttpResponseMessage response = await client
                    .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                    .ConfigureAwait(false);
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    long elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    Log.ReceivedResponse(logger, request.Endpoint, (int)response.StatusCode, elapsed);
                }

                if (response.IsSuccessStatusCode)
                {
                    // Failures while reading the body are not retried: part of it may already have been consumed.
                    return await ReadSuccessAsync(request, response, readSuccess, timeout.Token, cancellationToken).ConfigureAwait(false);
                }

                ErrorDetails error = await ReadErrorAsync(response, timeout.Token).ConfigureAwait(false);
                if (IsTokenInvalid(error.Message))
                {
                    throw new IpwBridgeTokenInvalidException(
                        $"Metazo rejected the token on '{request.Endpoint}': {error.Message}",
                        response.StatusCode, error.Message, error.Body);
                }

                if (attempt < maxAttempts && IsTransient(response.StatusCode, error))
                {
                    transientReason = $"HTTP {(int)response.StatusCode}";
                    retryAfter = GetRetryAfter(response);
                }
                else
                {
                    string reason = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {error.Message}";
                    Log.RequestFailed(logger, request.Endpoint, reason);
                    throw new IpwBridgeCommunicationException(
                        $"Metazo request to '{request.Endpoint}' failed with HTTP {(int)response.StatusCode}: {error.Message}",
                        response.StatusCode, error.Message, error.Body);
                }
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < maxAttempts)
                {
                    transientReason = "timeout";
                }
                else
                {
                    string reason = $"timed out after {request.Timeout.TotalSeconds:0.#} s";
                    Log.RequestFailed(logger, request.Endpoint, reason);
                    throw new IpwBridgeCommunicationException(
                        $"Metazo request to '{request.Endpoint}' {reason}.", new TimeoutException(reason, ex));
                }
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxAttempts)
                {
                    transientReason = ex.HttpRequestError.ToString();
                }
                else
                {
                    Log.RequestFailed(logger, request.Endpoint, ex.Message);
                    throw new IpwBridgeCommunicationException(
                        $"Metazo request to '{request.Endpoint}' failed: {ex.Message}", ex);
                }
            }

            TimeSpan delay = retryAfter ?? GetBackoff(attempt);
            Log.Retrying(logger, request.Endpoint, transientReason!, attempt, (long)delay.TotalMilliseconds);
            await Task.Delay(delay, clock.Time, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<TResult> ReadSuccessAsync<TResult>(
        MetazoRequest request,
        HttpResponseMessage response,
        Func<HttpResponseMessage, CancellationToken, Task<TResult>> readSuccess,
        CancellationToken timeoutToken,
        CancellationToken callerToken)
    {
        try
        {
            return await readSuccess(response, timeoutToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!callerToken.IsCancellationRequested)
        {
            string reason = $"timed out after {request.Timeout.TotalSeconds:0.#} s while transferring the response";
            Log.RequestFailed(logger, request.Endpoint, reason);
            throw new IpwBridgeCommunicationException($"Metazo request to '{request.Endpoint}' {reason}.", new TimeoutException(reason, ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            Log.RequestFailed(logger, request.Endpoint, ex.Message);
            throw new IpwBridgeCommunicationException($"Reading the response from '{request.Endpoint}' failed: {ex.Message}", ex);
        }
    }

    private async Task<T> ReadJsonAsync<T>(MetazoRequest request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // The body is buffered so the envelope (success/error/message) can be checked independently of how the
        // caller's type is deserialized; JSON responses are materialized in full either way.
        byte[] body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        ReadOnlySpan<byte> json = StripBom(body);
        T? result;
        try
        {
            ThrowIfUnsuccessful(request, response, MetazoEnvelope.Read(json));
            result = JsonSerializer.Deserialize(json, serializer.GetTypeInfo<T>());
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            Log.RequestFailed(logger, request.Endpoint, $"invalid JSON for {typeof(T).Name}: {ex.Message}");
            throw new IpwBridgeDeserializationException(
                $"The response from '{request.Endpoint}' could not be deserialized into '{typeof(T).Name}': {ex.Message}", ex);
        }

        if (result is null)
        {
            Log.RequestFailed(logger, request.Endpoint, "empty JSON response");
            throw new IpwBridgeDeserializationException($"The response from '{request.Endpoint}' was JSON null.");
        }

        return result;
    }

    /// <summary>Copies the response to the caller's stream, telling read failures and write failures apart.</summary>
    private static async Task CopyToDestinationAsync(MetazoRequest request, Stream source, Stream destination, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        while (true)
        {
            int read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return;
            }

            await WriteToDestinationAsync(request, () => destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken)).ConfigureAwait(false);
        }
    }

    private static async Task WriteToDestinationAsync(MetazoRequest request, Func<ValueTask> write)
    {
        try
        {
            await write().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ObjectDisposedException or UnauthorizedAccessException)
        {
            throw new IpwBridgeException($"Writing the response from '{request.Endpoint}' to the destination stream failed: {ex.Message}", ex);
        }
    }

    /// <summary>Throws when the envelope reports <c>"success": "false"</c>.</summary>
    private void ThrowIfUnsuccessful(MetazoRequest request, HttpResponseMessage response, MetazoEnvelope? envelope)
    {
        if (envelope?.Success != false)
        {
            return;
        }

        string message = Truncate(envelope.Value.ServerMessage) ?? "the API reported \"success\": \"false\"";
        if (IsTokenInvalid(message))
        {
            throw new IpwBridgeTokenInvalidException(
                $"Metazo rejected the token on '{request.Endpoint}': {message}", response.StatusCode, message, null);
        }

        Log.RequestFailed(logger, request.Endpoint, message);
        throw new IpwBridgeCommunicationException(
            $"Metazo request to '{request.Endpoint}' was not successful: {message}", response.StatusCode, message, null);
    }

    private static bool IsJson(HttpResponseMessage response)
        => response.Content.Headers.ContentType?.MediaType is { } mediaType
            && (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));

    /// <summary>Skips a UTF-8 byte order mark, which the stream-based reader of 1.x tolerated.</summary>
    private static ReadOnlySpan<byte> StripBom(ReadOnlySpan<byte> bytes)
        => bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? bytes[3..] : bytes;

    private static async Task<ErrorDetails> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // Read enough to classify a structured error; keep only MaxErrorBodyLength of it.
        byte[] buffer = new byte[Constants.MaxErrorReadLength];
        int read = 0;
        try
        {
            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            // Keep whatever was read; the status code alone still describes the failure.
        }

        ReadOnlySpan<byte> bytes = StripBom(buffer.AsSpan(0, read));
        string body = Encoding.UTF8.GetString(bytes[..Math.Min(bytes.Length, Constants.MaxErrorBodyLength)]);
        MetazoEnvelope? envelope = MetazoEnvelope.TryRead(bytes);
        string message = envelope?.ServerMessage
            ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "no details" : body.Trim());

        return new ErrorDetails(Truncate(message)!, body, envelope is not null);
    }

    // Only the server's error text is inspected (the "message"/"error" fields, or the raw text of a non-JSON body),
    // so user data echoed in other JSON fields cannot trigger a refresh.
    private static bool IsTokenInvalid(string message)
        => Constants.TokenInvalidMessages.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));

    // A 500 with a structured Metazo error ({"error": ...}) is a deterministic application error, not a
    // transient fault, so it is not retried; a bare 500 from a proxy or crashed worker is.
    private static bool IsTransient(HttpStatusCode statusCode, ErrorDetails error)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
            || (statusCode == HttpStatusCode.InternalServerError && !error.IsStructured);

    private TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        TimeSpan? delay = retryAfter?.Delta
            ?? (retryAfter?.Date is { } date ? date - clock.Time.GetUtcNow() : null);

        if (delay is null)
        {
            return null;
        }

        return delay.Value < TimeSpan.Zero ? TimeSpan.Zero : delay.Value > MaxRetryDelay ? MaxRetryDelay : delay.Value;
    }

    private TimeSpan GetBackoff(int attempt) => GetBackoff(_options.RetryBaseDelay, attempt, Random.Shared.NextDouble());

    /// <summary>Exponential backoff with up to 20 % jitter, capped at 30 seconds.</summary>
    internal static TimeSpan GetBackoff(TimeSpan baseDelay, int attempt, double random)
    {
        double baseMilliseconds = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        double jitter = baseMilliseconds * 0.2 * random;
        return TimeSpan.FromMilliseconds(Math.Min(baseMilliseconds + jitter, MaxRetryDelay.TotalMilliseconds));
    }

    private static string? Truncate(string? text)
        => text is null || text.Length <= Constants.MaxErrorMessageLength
            ? text
            : string.Concat(text.AsSpan(0, Constants.MaxErrorMessageLength), "…");

    private sealed record ErrorDetails(string Message, string? Body, bool IsStructured);
}

internal static class FormattingExtensions
{
    public static string ToInvariantString(this int value) => value.ToString(CultureInfo.InvariantCulture);
}
