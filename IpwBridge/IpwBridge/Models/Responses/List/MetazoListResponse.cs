using IpwBridge.Interfaces.Models;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.List;

public class MetazoListResponse<T> where T : IMetazoListItem
{
    [JsonPropertyName("success")]
    public required string SuccessAsString { get; init; }

    [JsonIgnore]
    public bool Success => SuccessAsString.Equals("true", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("datatype")]
    public required string Datatype { get; init; }

    [JsonPropertyName("primaryfield")]
    public required string PrimaryField { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("limit")]
    public required string Limit { get; init; }

    [JsonPropertyName("offset")]
    public required int Offset { get; init; }

    [JsonPropertyName("items")]
    public required List<T> Items { get; init; }
}
