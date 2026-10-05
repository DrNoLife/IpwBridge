namespace IpwBridge.Exceptions;

/// <summary>
/// Base type for every exception thrown by IpwBridge, so callers can catch all library failures in one place.
/// </summary>
public class IpwBridgeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="IpwBridgeException"/> class.</summary>
    public IpwBridgeException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public IpwBridgeException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the underlying cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public IpwBridgeException(string message, Exception innerException) : base(message, innerException) { }
}
