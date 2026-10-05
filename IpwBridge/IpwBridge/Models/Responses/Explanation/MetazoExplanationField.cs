using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

/// <summary>A field of a datatype, as described by <c>/explain</c>.</summary>
public sealed class MetazoExplanationField
{
    /// <summary>Gets the field id, for example <c>f276474</c>.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Gets the field label.</summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>Gets the field type, for example <c>LITERAL</c>.</summary>
    [JsonPropertyName("fieldtype")]
    public required string FieldType { get; init; }

    /// <summary>Gets the input type, for example <c>string</c>.</summary>
    [JsonPropertyName("inputtype")]
    public required string InputType { get; init; }

    /// <summary>Gets the related field or datatype, if any.</summary>
    [JsonPropertyName("relation")]
    public required string Relation { get; init; }

    /// <summary>Gets the validation rule, for example <c>blank</c>.</summary>
    [JsonPropertyName("validate")]
    public required string Validate { get; init; }
}
