using IpwBridge.Interfaces;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

public class MetazoItemResponse<T> where T : IMetazoItemObject
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; set; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("result")]
    public required MetazoItemResult<T> Result { get; set; }
}
