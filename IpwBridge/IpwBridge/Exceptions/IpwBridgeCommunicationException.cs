using System.Net;

namespace IpwBridge.Exceptions;

/// <summary>
/// Thrown when a call to the Metazo API fails: a non-success HTTP status, a response with
/// <c>"success": "false"</c>, a network error that persisted through retries, or a timeout.
/// </summary>
public class IpwBridgeCommunicationException : IpwBridgeException
{
    /// <summary>Initializes a new instance of the <see cref="IpwBridgeCommunicationException"/> class.</summary>
    public IpwBridgeCommunicationException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public IpwBridgeCommunicationException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the underlying cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public IpwBridgeCommunicationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance describing a failed HTTP exchange.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code the server returned.</param>
    /// <param name="serverMessage">The error text reported by the server, if any.</param>
    /// <param name="responseBody">The (truncated) response body.</param>
    public IpwBridgeCommunicationException(string message, HttpStatusCode? statusCode, string? serverMessage, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ServerMessage = serverMessage;
        ResponseBody = responseBody;
    }

    /// <summary>Gets the HTTP status code, or <see langword="null"/> when no response was received.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets the error text reported by the server (its <c>message</c> or <c>error</c> field), if any.</summary>
    public string? ServerMessage { get; }

    /// <summary>
    /// Gets the response body, truncated to 4 KB. It is kept out of <see cref="Exception.Message"/>
    /// so that raw server output does not end up in telemetry by default.
    /// </summary>
    public string? ResponseBody { get; }
}
