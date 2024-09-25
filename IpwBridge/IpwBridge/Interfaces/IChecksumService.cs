namespace IpwBridge.Interfaces;

public interface IChecksumService
{
    string CalculateChecksum(Dictionary<string, string> parameters, string secret, string? jsonPayload = null);
}
