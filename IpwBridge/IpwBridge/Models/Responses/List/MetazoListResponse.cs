using IpwBridge.Interfaces;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.List;

public class MetazoListResponse<T> where T : IMetazoListItem
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; set; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("datatype")]
    public required string Datatype { get; set; }

    [JsonPropertyName("primaryfield")]
    public required string PrimaryField { get; set; }

    [JsonPropertyName("count")]
    public required int Count { get; set; }

    [JsonPropertyName("limit")]
    public required string Limit { get; set; }

    [JsonPropertyName("offset")]
    public required int Offset { get; set; }

    [JsonPropertyName("items")]
    public required List<T> Items { get; set; }
}
