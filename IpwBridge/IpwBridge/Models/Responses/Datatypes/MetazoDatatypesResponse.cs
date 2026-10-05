using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Datatypes;

/// <summary>The response from <c>/datatypes</c>.</summary>
public sealed class MetazoDatatypesResponse
{
    private readonly IReadOnlyList<MetazoDatatype> _datatypes = [];

    /// <summary>Gets the number of datatypes reported by the API.</summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>Gets the available datatypes.</summary>
    [JsonPropertyName("datatypes")]
    public IReadOnlyList<MetazoDatatype> Datatypes
    {
        get => _datatypes;
        init => _datatypes = value ?? [];
    }
}
