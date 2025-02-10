using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

public class MetazoExplanationField
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("label")]
    public required string Label { get; set; }

    [JsonPropertyName("fieldtype")]
    public required string FieldType { get; set; }

    [JsonPropertyName("inputtype")]
    public required string InputType { get; set; }

    [JsonPropertyName("relation")]
    public required string Relation { get; set; }

    [JsonPropertyName("validate")]
    public required string Validate { get; set; }
}
