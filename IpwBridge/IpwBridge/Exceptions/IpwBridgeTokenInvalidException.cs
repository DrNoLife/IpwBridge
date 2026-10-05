namespace IpwBridge.Exceptions;

/// <summary>
/// Thrown when the server rejects a token as unknown, expired or revoked. For token-authenticated calls,
/// IpwBridge re-authenticates and retries once first, so this means the fresh token was rejected as well. For the
/// quickfilter endpoints it means the configured datasource token was rejected.
/// </summary>
public class IpwBridgeTokenInvalidException : IpwBridgeCommunicationException
{
    /// <summary>Initializes a new instance of the <see cref="IpwBridgeTokenInvalidException"/> class.</summary>
    public IpwBridgeTokenInvalidException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public IpwBridgeTokenInvalidException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the underlying cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public IpwBridgeTokenInvalidException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance describing the rejected HTTP exchange.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code the server returned.</param>
    /// <param name="serverMessage">The error text reported by the server.</param>
    /// <param name="responseBody">The (truncated) response body.</param>
    public IpwBridgeTokenInvalidException(string message, System.Net.HttpStatusCode? statusCode, string? serverMessage, string? responseBody)
        : base(message, statusCode, serverMessage, responseBody) { }
}
