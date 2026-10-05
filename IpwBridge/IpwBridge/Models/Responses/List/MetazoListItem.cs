using System.Text.Json;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.List;

/// <summary>
/// The default item type for <c>GetListAsync&lt;T&gt;</c>: the standard fields, plus every requested field in
/// <see cref="AdditionalFields"/>.
/// </summary>
public sealed class MetazoListItem
{
    /// <summary>Gets the object id.</summary>
    [JsonPropertyName("objectid")]
    public required string ObjectId { get; init; }

    /// <summary>Gets the language code of the item.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    /// <summary>Gets all other returned fields, keyed by field id.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement> AdditionalFields { get; set; } = new Dictionary<string, JsonElement>();
}
