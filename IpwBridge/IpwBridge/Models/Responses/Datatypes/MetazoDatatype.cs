using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Datatypes;

public class MetazoDatatype
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
