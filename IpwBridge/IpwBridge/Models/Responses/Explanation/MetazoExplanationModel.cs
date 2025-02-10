using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

public class MetazoExplanationModel
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name{ get; set; }
}
