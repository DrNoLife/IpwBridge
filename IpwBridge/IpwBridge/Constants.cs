namespace IpwBridge;

internal static class Constants
{
    /// <summary>Name of the <see cref="HttpClient"/> registered for all Metazo traffic.</summary>
    public const string HttpClientName = "Metazo";

    /// <summary>Server messages that mean the token is unknown, expired or revoked.</summary>
    public static readonly string[] TokenInvalidMessages =
    [
        "Token doesn't exist in the database",
        "Expired token",
        "Invalid token",
    ];

    /// <summary>Number of leading file bytes the Metazo upload checksum covers.</summary>
    public const int FileChecksumPrefixLength = 256;

    /// <summary>Token lifetime used when the token is not a JWT with a readable <c>exp</c> claim.</summary>
    public static readonly TimeSpan FallbackTokenLifetime = TimeSpan.FromMinutes(25);

    /// <summary>A token is treated as expired this long before its <c>exp</c> claim.</summary>
    public static readonly TimeSpan TokenExpiryMargin = TimeSpan.FromSeconds(60);

    /// <summary>Upper bound on how much of an error response body is kept for exceptions.</summary>
    public const int MaxErrorBodyLength = 4096;

    /// <summary>Upper bound on how much of an error response body is read to classify it.</summary>
    public const int MaxErrorReadLength = 65536;

    /// <summary>Upper bound on server error text placed in exception messages.</summary>
    public const int MaxErrorMessageLength = 200;
}
