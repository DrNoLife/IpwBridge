namespace IpwBridge.Exceptions;

/// <summary>
/// Thrown when IpwBridge cannot obtain a token from <c>/authenticate</c>, for example because the
/// credentials, site or checksum secret are wrong.
/// </summary>
public class IpwBridgeAuthenticationException : IpwBridgeException
{
    /// <summary>Initializes a new instance of the <see cref="IpwBridgeAuthenticationException"/> class.</summary>
    public IpwBridgeAuthenticationException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public IpwBridgeAuthenticationException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the underlying cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public IpwBridgeAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}
