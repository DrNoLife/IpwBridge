using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses.Explanation;

/// <summary>The response from <c>/explain</c>: the fields and models of a datatype.</summary>
public sealed class MetazoExplanationResponse
{
    private readonly IReadOnlyList<MetazoExplanationModel> _models = [];

    private readonly IReadOnlyList<MetazoExplanationField> _fields = [];

    /// <summary>Gets the datatype that was explained.</summary>
    [JsonPropertyName("datatype")]
    public required string Datatype { get; init; }

    /// <summary>Gets the id of the datatype's primary field.</summary>
    [JsonPropertyName("primary_field")]
    public required string PrimaryField { get; init; }

    /// <summary>Gets the fields of the datatype.</summary>
    [JsonPropertyName("fields")]
    public IReadOnlyList<MetazoExplanationField> Fields
    {
        get => _fields;
        init => _fields = value ?? [];
    }

    /// <summary>Gets the datatype-specific models. The built-in create, update and delete models are not listed.</summary>
    [JsonPropertyName("models")]
    public IReadOnlyList<MetazoExplanationModel> Models
    {
        get => _models;
        init => _models = value ?? [];
    }
}
