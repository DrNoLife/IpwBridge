using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses;

/// <summary>The response from <c>/authenticate</c>.</summary>
internal sealed class MetazoAuthenticationResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; init; }
}
