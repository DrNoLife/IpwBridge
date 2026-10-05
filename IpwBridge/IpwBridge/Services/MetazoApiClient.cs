using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using IpwBridge.Contracts;
using IpwBridge.Exceptions;
using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using IpwBridge.Models.Responses;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;
using IpwBridge.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IpwBridge.Services;

/// <inheritdoc cref="IMetazoApiClient"/>
internal sealed class MetazoApiClient(
    IOptions<MetazoApiOptions> options,
    TokenProvider tokenProvider,
    ChecksumService checksumService,
    UrlBuilder urlBuilder,
    ApiRequestSender sender,
    ILogger<MetazoApiClient> logger) : IMetazoApiClient
{
    private readonly MetazoApiOptions _options = options.Value;

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        MetazoRequest request = new(HttpMethod.Get, urlBuilder.Build("ping"), "ping", _options.RequestTimeout, Retryable: false);
        try
        {
            await sender.SendJsonAsync<JsonElement>(request, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (IpwBridgeException)
        {
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default)
    {
        string token = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            JsonElement response = await sender
                .SendJsonAsync<JsonElement>(SignedGet("validate", [], token), cancellationToken)
                .ConfigureAwait(false);

            bool valid = response.TryGetProperty("validtoken", out var validToken) && MetazoEnvelope.ReadBoolean(validToken) == true;
            if (!valid)
            {
                tokenProvider.Invalidate(token);
            }

            return valid;
        }
        catch (IpwBridgeTokenInvalidException)
        {
            tokenProvider.Invalidate(token);
            return false;
        }
    }

    public async Task RevokeTokenAsync(CancellationToken cancellationToken = default)
    {
        if (tokenProvider.CachedToken is not { } token)
        {
            return;
        }

        try
        {
            await sender.SendJsonAsync<JsonElement>(SignedGet("revoke", [], token), cancellationToken).ConfigureAwait(false);
            Log.TokenRevoked(logger);
        }
        catch (IpwBridgeTokenInvalidException)
        {
            // Already unknown to the server; nothing left to revoke.
        }
        finally
        {
            tokenProvider.Invalidate(token);
        }
    }

    public Task<MetazoDatatypesResponse> GetDatatypesAsync(CancellationToken cancellationToken = default)
        => GetAsync<MetazoDatatypesResponse>("datatypes", [], cancellationToken);

    public Task<MetazoExplanationResponse> GetExplanationAsync(string datatype, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datatype);
        return GetAsync<MetazoExplanationResponse>("explain", new() { ["datatype"] = datatype }, cancellationToken);
    }

    public Task<JsonElement> GetListAsync(ListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return GetAsync<JsonElement>("list", request.ToQueryParameters(), cancellationToken);
    }

    public Task<MetazoListResponse<T>> GetListAsync<T>(ListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return GetAsync<MetazoListResponse<T>>("list", request.ToQueryParameters(), cancellationToken);
    }

    public IAsyncEnumerable<T> GetAllAsync<T>(ListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.ToQueryParameters(); // Validate before the first MoveNextAsync.
        return GetAllCoreAsync<T>(request, cancellationToken);
    }

    private async IAsyncEnumerable<T> GetAllCoreAsync<T>(ListRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int offset = request.Offset;
        while (true)
        {
            var page = await GetListAsync<T>(request.WithOffset(offset), cancellationToken).ConfigureAwait(false);
            foreach (var item in page.Items)
            {
                yield return item;
            }

            // The server may cap the page size; it echoes the limit it applied, so compare with that.
            int pageSize = page.Limit > 0 ? Math.Min(page.Limit, request.Limit) : request.Limit;
            if (request.Limit == 0 || page.Items.Count == 0 || page.Items.Count < pageSize)
            {
                yield break;
            }

            offset += page.Items.Count;
        }
    }

    public Task<JsonElement> GetItemAsync(int objectId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectId);
        return GetAsync<JsonElement>("read", new() { ["objectid"] = objectId.ToInvariantString() }, cancellationToken);
    }

    public Task<MetazoItemResponse<T>> GetItemAsync<T>(int objectId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectId);
        return GetAsync<MetazoItemResponse<T>>("read", new() { ["objectid"] = objectId.ToInvariantString() }, cancellationToken);
    }

    public async Task<MetazoModelResponse> SendModelAsync(CrudRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        Dictionary<string, string> parameters = new(StringComparer.Ordinal)
        {
            ["datatype"] = request.Datatype,
            ["model"] = request.Model.Value!.ToLowerInvariant(),
        };

        if (request.ObjectId is { } objectId)
        {
            parameters["objectid"] = objectId.ToInvariantString();
        }

        string json = request.JsonData;
        JsonElement response = await ExecuteAuthenticatedAsync("model", (token, ct) =>
        {
            string url = SignedUrl("model", parameters, token, json);
            MetazoRequest message = new(HttpMethod.Post, url, "model", _options.RequestTimeout, Retryable: false)
            {
                ContentFactory = () => new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return sender.SendJsonAsync<JsonElement>(message, ct);
        }, cancellationToken).ConfigureAwait(false);

        return new MetazoModelResponse(response);
    }

    public async Task<MetazoUploadResponse> UploadBinfileAsync(BinfileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        PreparedUpload upload = await PreparedUpload.PrepareAsync(request, cancellationToken).ConfigureAwait(false);
        await using (upload.ConfigureAwait(false))
        {
            Dictionary<string, string> parameters = new(StringComparer.Ordinal)
            {
                ["parentid"] = request.ParentId.ToInvariantString(),
            };

            foreach (var (key, checksum) in upload.FileChecksums)
            {
                parameters[key] = checksum;
            }

            JsonElement response = await ExecuteAuthenticatedAsync("binfile/upload", (token, ct) =>
            {
                string url = SignedUrl("binfile/upload", parameters, token);
                MetazoRequest message = new(HttpMethod.Post, url, "binfile/upload", _options.BinfileTimeout, Retryable: false)
                {
                    ContentFactory = upload.CreateContent,
                };
                return sender.SendJsonAsync<JsonElement>(message, ct);
            }, cancellationToken).ConfigureAwait(false);

            return new MetazoUploadResponse(response);
        }
    }

    public Task<byte[]> DownloadBinfileAsync(int objectId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectId);
        Dictionary<string, string> parameters = new(StringComparer.Ordinal) { ["objectid"] = objectId.ToInvariantString() };

        return ExecuteAuthenticatedAsync("binfile/download", (token, ct) =>
            sender.SendForBytesAsync(SignedGet("binfile/download", parameters, token, _options.BinfileTimeout), ct),
            cancellationToken);
    }

    public Task DownloadBinfileAsync(int objectId, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectId);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }

        Dictionary<string, string> parameters = new(StringComparer.Ordinal) { ["objectid"] = objectId.ToInvariantString() };

        return ExecuteAuthenticatedAsync("binfile/download", async (token, ct) =>
        {
            // Retries and the token refresh happen before anything is written: a rejected token is detected from
            // the status code, or from a JSON error body, before any byte reaches the destination.
            await sender.SendToStreamAsync(SignedGet("binfile/download", parameters, token, _options.BinfileTimeout), destination, ct)
                .ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }

    public Task<JsonElement> GetFilterAsync(int filterId, bool advanced = false, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(filterId);
        Dictionary<string, string> parameters = new(StringComparer.Ordinal)
        {
            ["advanced"] = advanced ? "1" : "0",
            ["includeinactive"] = includeInactive ? "1" : "0",
        };

        return SendFilterAsync($"filter/{filterId.ToInvariantString()}", parameters, cancellationToken);
    }

    public Task<JsonElement> GetFilterCountAsync(int filterId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(filterId);
        return SendFilterAsync($"filter/{filterId.ToInvariantString()}/count", [], cancellationToken);
    }

    private Task<JsonElement> SendFilterAsync(string endpoint, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DatasourceToken))
        {
            throw new InvalidOperationException(
                $"Quickfilter endpoints require {nameof(MetazoApiOptions)}.{nameof(MetazoApiOptions.DatasourceToken)} to be configured.");
        }

        MetazoRequest request = new(HttpMethod.Get, urlBuilder.Build(endpoint, parameters), "filter", _options.RequestTimeout, Retryable: true)
        {
            Headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["site"] = _options.Site.ToInvariantString(),
                ["username"] = _options.IpwUser,
                ["usertoken"] = _options.DatasourceToken,
            },
        };

        return sender.SendJsonAsync<JsonElement>(request, cancellationToken);
    }

    private Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> parameters, CancellationToken cancellationToken)
        => ExecuteAuthenticatedAsync(endpoint, (token, ct) =>
            sender.SendJsonAsync<T>(SignedGet(endpoint, parameters, token), ct), cancellationToken);

    private MetazoRequest SignedGet(string endpoint, Dictionary<string, string> parameters, string token, TimeSpan? timeout = null)
        => new(HttpMethod.Get, SignedUrl(endpoint, parameters, token), endpoint, timeout ?? _options.RequestTimeout, Retryable: true);

    private string SignedUrl(string endpoint, Dictionary<string, string> parameters, string token, string? jsonPayload = null)
    {
        Dictionary<string, string> signed = new(parameters, StringComparer.Ordinal) { ["token"] = token };
        signed["checksum"] = checksumService.Calculate(signed, jsonPayload);
        return urlBuilder.Build(endpoint, signed);
    }

    /// <summary>
    /// Runs <paramref name="action"/> with the current token. If the server rejects the token, the token is
    /// dropped and the action runs once more with a fresh one.
    /// </summary>
    private async Task<T> ExecuteAuthenticatedAsync<T>(
        string endpoint,
        Func<string, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        string token = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(token, cancellationToken).ConfigureAwait(false);
        }
        catch (IpwBridgeTokenInvalidException)
        {
            Log.TokenRejected(logger, endpoint);
            tokenProvider.Invalidate(token);
        }

        token = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(token, cancellationToken).ConfigureAwait(false);
        }
        catch (IpwBridgeTokenInvalidException ex)
        {
            Log.RequestFailed(logger, endpoint, $"a freshly obtained token was rejected as well: {ex.ServerMessage}");
            throw;
        }
    }
}
