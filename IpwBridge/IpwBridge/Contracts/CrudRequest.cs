using System.Text.Json;
using IpwBridge.Contracts.Enums;
using IpwBridge.Contracts.Models;

namespace IpwBridge.Contracts;

/// <summary>
/// A request to run a model (create, update, delete, copy or a datatype-specific model) on an object through
/// <c>/model</c>.
/// </summary>
/// <remarks>Use the static builders (<see cref="Create"/>, <see cref="Update"/>, <see cref="Delete"/>,
/// <see cref="CreateCopy"/>) for the built-in models; they set the required properties for you.</remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// string json = new JsonObject { ["state"] = "2" }.ToJsonString();
/// var response = await client.SendModelAsync(CrudRequest.Update("form121889", 2605115, json));
/// ]]></code>
/// </example>
public sealed class CrudRequest
{
    private static readonly string[] ReservedFields = ["datatype", "model", "objectid", "token", "checksum"];

    /// <summary>Gets or sets the datatype of the object, for example <c>form121889</c>.</summary>
    public required string Datatype { get; set; }

    /// <summary>Gets or sets the model to run.</summary>
    public required CrudModel Model { get; set; }

    /// <summary>
    /// Gets or sets the field values as a JSON object, for example <c>{"f276474":"value"}</c>. May be empty for
    /// models that take no data, such as delete. Every top-level property is part of the request checksum.
    /// </summary>
    public string JsonData { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the object to run the model on. Required for every model except create.</summary>
    public int? ObjectId { get; set; }

    /// <summary>Creates a request that creates a new object.</summary>
    /// <param name="dataType">The datatype of the new object.</param>
    /// <param name="jsonData">The field values as a JSON object.</param>
    /// <returns>The request.</returns>
    public static CrudRequest Create(string dataType, string jsonData) => new()
    {
        Datatype = dataType,
        Model = ModelOptions.Create,
        JsonData = jsonData,
    };

    /// <summary>Creates a request that updates an existing object.</summary>
    /// <param name="dataType">The datatype of the object.</param>
    /// <param name="objectId">The id of the object to update.</param>
    /// <param name="jsonData">The field values to change, as a JSON object.</param>
    /// <returns>The request.</returns>
    public static CrudRequest Update(string dataType, int objectId, string jsonData) => new()
    {
        Datatype = dataType,
        Model = ModelOptions.Update,
        ObjectId = objectId,
        JsonData = jsonData,
    };

    /// <summary>Creates a request that deletes an object. Deleting a form object also deletes its binfiles.</summary>
    /// <param name="dataType">
    /// The datatype of the object. The API requires an existing datatype here, but in practice accepts other
    /// existing datatypes (such as <c>binfile</c>) as well.
    /// </param>
    /// <param name="objectId">The id of the object to delete.</param>
    /// <returns>The request.</returns>
    public static CrudRequest Delete(string dataType, int objectId) => new()
    {
        Datatype = dataType,
        Model = ModelOptions.Delete,
        ObjectId = objectId,
    };

    /// <summary>Creates a request that copies an existing object.</summary>
    /// <param name="dataType">The datatype of the object.</param>
    /// <param name="objectId">The id of the object to copy.</param>
    /// <param name="jsonData">Optional field values for the copy, as a JSON object.</param>
    /// <returns>The request.</returns>
    public static CrudRequest CreateCopy(string dataType, int objectId, string jsonData = "") => new()
    {
        Datatype = dataType,
        Model = ModelOptions.CreateCopy,
        ObjectId = objectId,
        JsonData = jsonData,
    };

    /// <summary>Throws if the request cannot be sent.</summary>
    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Datatype, nameof(Datatype));

        if (string.IsNullOrWhiteSpace(Model.Value))
        {
            throw new ArgumentException("The model must be set.", nameof(Model));
        }

        bool isCreate = string.Equals(Model.Value, ((CrudModel)ModelOptions.Create).Value, StringComparison.OrdinalIgnoreCase);
        if (!isCreate && ObjectId is null)
        {
            throw new ArgumentException($"The model '{Model.Value}' requires an object id.", nameof(ObjectId));
        }

        if (!string.IsNullOrWhiteSpace(JsonData))
        {
            try
            {
                using var document = JsonDocument.Parse(JsonData);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException("The JSON data must be a JSON object.", nameof(JsonData));
                }

                // Every field is part of the checksum next to the query parameters, so names must be unique.
                HashSet<string> names = new(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (ReservedFields.Contains(property.Name, StringComparer.Ordinal))
                    {
                        throw new ArgumentException($"The JSON field '{property.Name}' collides with a query parameter.", nameof(JsonData));
                    }

                    if (!names.Add(property.Name))
                    {
                        throw new ArgumentException($"The JSON field '{property.Name}' appears more than once.", nameof(JsonData));
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("The JSON data is not valid JSON.", nameof(JsonData), ex);
            }
        }
    }
}
