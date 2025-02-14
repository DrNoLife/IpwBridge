using IpwBridge.Interfaces;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

public class MetazoItemResult<T> where T : IMetazoItemObject
{
    [JsonPropertyName("objectid")]
    public required int ObjectId { get; init; }

    [JsonPropertyName("fields")]
    public required T Object { get; init; }
}