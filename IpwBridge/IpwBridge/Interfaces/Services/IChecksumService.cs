namespace IpwBridge.Interfaces.Services;

public interface IChecksumService
{
    string CalculateChecksum(Dictionary<string, string> parameters, string secret, string? jsonPayload = null);
}
