using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.List;

/// <summary>The response from <c>/list</c>.</summary>
/// <typeparam name="T">The type each item is deserialized into.</typeparam>
public sealed class MetazoListResponse<T>
{
    private readonly IReadOnlyList<T> _items = [];

    /// <summary>Gets the datatype that was listed.</summary>
    [JsonPropertyName("datatype")]
    public required string Datatype { get; init; }

    /// <summary>Gets the id of the datatype's primary field.</summary>
    [JsonPropertyName("primaryfield")]
    public string? PrimaryField { get; init; }

    /// <summary>Gets the number of items in this page. Accepts both <c>"20"</c> and <c>20</c>.</summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>Gets the limit the page was requested with.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    /// <summary>Gets the offset the page was requested with.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    /// <summary>Gets the items in this page.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items
    {
        get => _items;
        init => _items = value ?? [];
    }
}
