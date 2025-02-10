using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

public class MetazoExplanationResponse
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; set; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("datatype")]
    public required string Datatype { get; set; }

    [JsonPropertyName("primary_field")]
    public required string PrimaryField { get; set; }

    [JsonPropertyName("fields")]
    public required List<MetazoExplanationField> Fields { get; set; }

    [JsonPropertyName("models")]
    public required List<MetazoExplanationModel> Models { get; set; }
}
