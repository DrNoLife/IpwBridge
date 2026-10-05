using Microsoft.Extensions.Logging;

namespace IpwBridge.Services;

/// <summary>
/// All log messages, source-generated so nothing is formatted when the level is disabled. Messages never contain
/// URLs, tokens, credentials or request/response bodies.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Sending {Method} request to Metazo endpoint '{Endpoint}' (attempt {Attempt})")]
    public static partial void SendingRequest(ILogger logger, string method, string endpoint, int attempt);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Metazo endpoint '{Endpoint}' responded {StatusCode} after {ElapsedMilliseconds} ms")]
    public static partial void ReceivedResponse(ILogger logger, string endpoint, int statusCode, long elapsedMilliseconds);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Transient failure calling Metazo endpoint '{Endpoint}' ({Reason}); retry {Retry} in {DelayMilliseconds} ms")]
    public static partial void Retrying(ILogger logger, string endpoint, string reason, int retry, long delayMilliseconds);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Metazo request to '{Endpoint}' failed: {Reason}")]
    public static partial void RequestFailed(ILogger logger, string endpoint, string reason);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Acquired a Metazo token valid until {ExpiresAt:O}")]
    public static partial void TokenAcquired(ILogger logger, DateTimeOffset expiresAt);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Metazo rejected the token on '{Endpoint}'; re-authenticating and retrying once")]
    public static partial void TokenRejected(ILogger logger, string endpoint);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Revoked the Metazo token")]
    public static partial void TokenRevoked(ILogger logger);
}
