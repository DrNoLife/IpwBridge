using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

public class MetazoExplanationResponse
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; init; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("datatype")]
    public required string Datatype { get; init; }

    [JsonPropertyName("primary_field")]
    public required string PrimaryField { get; init; }

    [JsonPropertyName("fields")]
    public required List<MetazoExplanationField> Fields { get; init; }

    [JsonPropertyName("models")]
    public required List<MetazoExplanationModel> Models { get; init; }
}
