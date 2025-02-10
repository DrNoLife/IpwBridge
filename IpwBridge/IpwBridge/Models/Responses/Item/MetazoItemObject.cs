using IpwBridge.Interfaces;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

public class MetazoItemObject : IMetazoItemObject
{
    [JsonPropertyName("objectid")]
    public required string ObjectId { get; set; }

    [JsonPropertyName("site")]
    public required string Site { get; set; }
}