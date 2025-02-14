using IpwBridge.Interfaces.Models;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.List;

public class MetazoListItem : IMetazoListItem
{
    [JsonPropertyName("objectid")]
    public required string ObjectId { get; set; }

    [JsonPropertyName("language")]
    public required string Language { get; set; }
}
