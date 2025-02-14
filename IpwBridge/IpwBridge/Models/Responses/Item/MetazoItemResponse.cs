using IpwBridge.Interfaces.Models;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

public class MetazoItemResponse<T> where T : IMetazoItemObject
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; init; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("result")]
    public required MetazoItemResult<T> Result { get; init; }
}
