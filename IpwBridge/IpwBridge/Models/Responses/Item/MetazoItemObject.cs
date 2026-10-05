using System.Text.Json;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

/// <summary>
/// The default item type for <c>GetItemAsync&lt;T&gt;</c>: the standard fields, plus every other field in
/// <see cref="AdditionalFields"/>.
/// </summary>
public sealed class MetazoItemObject
{
    /// <summary>Gets the object id.</summary>
    [JsonPropertyName("objectid")]
    public required string ObjectId { get; init; }

    /// <summary>Gets the site the object belongs to.</summary>
    [JsonPropertyName("site")]
    public string? Site { get; init; }

    /// <summary>Gets all other fields of the object, keyed by field id.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement> AdditionalFields { get; set; } = new Dictionary<string, JsonElement>();
}
