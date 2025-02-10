using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Datatypes;

public class MetazoDatatypesResponse
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; set; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("datatypes")]
    public required List<MetazoDatatype> Datatypes { get; set; }
}
