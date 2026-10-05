using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

/// <summary>The item part of a <c>/read</c> response.</summary>
/// <typeparam name="T">The type the item's fields are deserialized into.</typeparam>
public sealed class MetazoItemResult<T>
{
    /// <summary>Gets the object id. Accepts both <c>"5045"</c> and <c>5045</c>.</summary>
    [JsonPropertyName("objectid")]
    public required int ObjectId { get; init; }

    /// <summary>Gets the item's fields.</summary>
    [JsonPropertyName("fields")]
    public required T Fields { get; init; }
}
