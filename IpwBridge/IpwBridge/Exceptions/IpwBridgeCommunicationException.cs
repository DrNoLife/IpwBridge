namespace IpwBridge.Exceptions;

public class IpwBridgeCommunicationException : Exception
{
    public IpwBridgeCommunicationException(string message) : base(message) { }
    public IpwBridgeCommunicationException(string message, Exception innerException) : base(message, innerException) { }
}
