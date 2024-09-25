namespace IpwBridge.Models;

public class TokenInvalidException(string message) : Exception(message)
{
}
