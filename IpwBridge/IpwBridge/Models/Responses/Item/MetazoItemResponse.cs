using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Item;

/// <summary>The response from <c>/read</c>.</summary>
/// <typeparam name="T">The type the item's fields are deserialized into.</typeparam>
public sealed class MetazoItemResponse<T>
{
    /// <summary>Gets the item.</summary>
    [JsonPropertyName("result")]
    public required MetazoItemResult<T> Result { get; init; }
}
