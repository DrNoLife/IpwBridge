namespace IpwBridge.Exceptions;

public class IpwBridgeDeserializationException : Exception
{
    public IpwBridgeDeserializationException(string message) : base(message) { }
    public IpwBridgeDeserializationException(string message, Exception innerException) : base(message, innerException) { }
}
