using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Datatypes;

/// <summary>A datatype (form or subtable) available in Metazo.</summary>
public sealed class MetazoDatatype
{
    /// <summary>Gets the datatype id, for example <c>form1376</c>.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Gets the display name of the datatype.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
