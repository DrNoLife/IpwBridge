using System.Text.Json.Serialization;

namespace IpwBridge.Models;

public class IpwAuthenticationSuccessMessage
{
    public bool Success 
        => Status.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("success")]
    public string Status { get; set; } = String.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = String.Empty;
}
