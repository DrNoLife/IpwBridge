using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

public class MetazoExplanationModel
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name{ get; init; }
}
