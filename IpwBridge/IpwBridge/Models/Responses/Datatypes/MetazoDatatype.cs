using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Datatypes;

public class MetazoDatatype
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
