using System.Text.Json;
using IpwBridge.Exceptions;
using IpwBridge.Models;
using IpwBridge.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IpwBridge.Services;

/// <summary>
/// Obtains and caches the Metazo token. The token lives until 60 seconds before its JWT <c>exp</c> claim (or until
/// <c>exp</c> itself for very short-lived tokens), 25 minutes for tokens without a readable or future <c>exp</c>,
/// or until the server rejects it.
/// </summary>
internal sealed class TokenProvider(
    IOptions<MetazoApiOptions> options,
    ApiRequestSender sender,
    ChecksumService checksumService,
    UrlBuilder urlBuilder,
    IpwBridgeClock clock,
    ILogger<TokenProvider> logger) : IDisposable
{
    private readonly MetazoApiOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Token and expiry are swapped as one immutable object, so readers never see a torn pair.
    private volatile TokenState? _state;

    /// <summary>Gets the cached token, or <see langword="null"/> when none is cached.</summary>
    public string? CachedToken => _state?.Token;

    /// <summary>Returns a valid token, authenticating if none is cached or the cached one has expired.</summary>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_state is { } state && state.ExpiresAt > clock.Time.GetUtcNow())
        {
            return state.Token;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_state is { } current && current.ExpiresAt > clock.Time.GetUtcNow())
            {
                return current.Token;
            }

            TokenState fresh = await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
            _state = fresh;
            Log.TokenAcquired(logger, fresh.ExpiresAt);
            return fresh.Token;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Drops the cached token if it is still <paramref name="rejectedToken"/>. When several calls are rejected
    /// with the same token at once, only the first drops it and the rest reuse the token it obtains.
    /// </summary>
    public void Invalidate(string rejectedToken)
    {
        var state = _state;
        if (state is not null && string.Equals(state.Token, rejectedToken, StringComparison.Ordinal))
        {
            Interlocked.CompareExchange(ref _state, null, state);
        }
    }

    public void Dispose() => _lock.Dispose();

    private async Task<TokenState> AuthenticateAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, string> parameters = new(StringComparer.Ordinal)
        {
            ["pass"] = _options.IpwPassword,
            ["site"] = _options.Site.ToInvariantString(),
            ["user"] = _options.IpwUser,
        };

        if (!string.IsNullOrWhiteSpace(_options.Language))
        {
            parameters["language"] = _options.Language;
        }

        parameters["checksum"] = checksumService.Calculate(parameters);

        MetazoRequest request = _options.UseLegacyGetAuthentication
            ? new MetazoRequest(HttpMethod.Get, urlBuilder.Build("authenticate", parameters), "authenticate", _options.RequestTimeout, Retryable: true)
            : new MetazoRequest(HttpMethod.Post, urlBuilder.Build("authenticate"), "authenticate", _options.RequestTimeout, Retryable: true)
            {
                ContentFactory = () => CreateForm(parameters),
            };

        MetazoAuthenticationResponse response;
        try
        {
            response = await sender.SendJsonAsync<MetazoAuthenticationResponse>(request, cancellationToken).ConfigureAwait(false);
        }
        catch (IpwBridgeException ex) when (ex is not IpwBridgeAuthenticationException)
        {
            throw new IpwBridgeAuthenticationException($"Authentication against Metazo failed: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(response.Token))
        {
            throw new IpwBridgeAuthenticationException("Authentication against Metazo returned no token.");
        }

        return new TokenState(response.Token, GetExpiry(response.Token));
    }

    // The documentation sends the login as multipart form data, so that is what is sent here too.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The parts are owned by the returned content, which the request disposes.")]
    private static MultipartFormDataContent CreateForm(Dictionary<string, string> parameters)
    {
        MultipartFormDataContent form = new();
        foreach (var (key, value) in parameters)
        {
            form.Add(new StringContent(value), key);
        }

        return form;
    }

    private DateTimeOffset GetExpiry(string token)
    {
        DateTimeOffset now = clock.Time.GetUtcNow();
        DateTimeOffset fallback = now + Constants.FallbackTokenLifetime;

        if (TryReadJwtExpiry(token) is not { } expiresAt)
        {
            return fallback;
        }

        // Short-lived tokens still expire at their own exp; only an exp that already passed (clock skew between
        // client and server) falls back, and a rejected token is then refreshed on first use.
        DateTimeOffset withMargin = expiresAt - Constants.TokenExpiryMargin;
        return withMargin > now ? withMargin : expiresAt > now ? expiresAt : fallback;
    }

    /// <summary>Reads the <c>exp</c> claim of a JWT without validating it (the server does that).</summary>
    internal static DateTimeOffset? TryReadJwtExpiry(string token)
    {
        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return null;
        }

        try
        {
            byte[] payload = DecodeBase64Url(segments[1]);
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("exp", out var exp))
            {
                long? seconds = exp.ValueKind switch
                {
                    JsonValueKind.Number when exp.TryGetInt64(out long n) => n,
                    JsonValueKind.String when long.TryParse(exp.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long s) => s,
                    _ => null,
                };

                return seconds is { } value ? DateTimeOffset.FromUnixTimeSeconds(value) : null;
            }
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            // Not a JWT we can read; the fallback lifetime applies.
        }

        return null;
    }

    private static byte[] DecodeBase64Url(string value)
    {
        string base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64,
        };
        return Convert.FromBase64String(base64);
    }

    private sealed record TokenState(string Token, DateTimeOffset ExpiresAt);
}
