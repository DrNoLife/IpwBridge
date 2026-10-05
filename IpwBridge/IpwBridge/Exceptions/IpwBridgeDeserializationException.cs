namespace IpwBridge.Exceptions;

/// <summary>
/// Thrown when a Metazo response cannot be deserialized into the requested type.
/// </summary>
public class IpwBridgeDeserializationException : IpwBridgeException
{
    /// <summary>Initializes a new instance of the <see cref="IpwBridgeDeserializationException"/> class.</summary>
    public IpwBridgeDeserializationException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public IpwBridgeDeserializationException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the underlying cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public IpwBridgeDeserializationException(string message, Exception innerException) : base(message, innerException) { }
}
