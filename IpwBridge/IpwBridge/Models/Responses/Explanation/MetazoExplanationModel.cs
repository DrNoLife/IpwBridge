using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

/// <summary>A datatype-specific model, as described by <c>/explain</c>.</summary>
public sealed class MetazoExplanationModel
{
    /// <summary>Gets the model id, which can be passed as a <c>CrudModel</c>.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Gets the display name of the model.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
