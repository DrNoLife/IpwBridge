using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

public class MetazoExplanationField
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("fieldtype")]
    public required string FieldType { get; init; }

    [JsonPropertyName("inputtype")]
    public required string InputType { get; init; }

    [JsonPropertyName("relation")]
    public required string Relation { get; init; }

    [JsonPropertyName("validate")]
    public required string Validate { get; init; }
}
