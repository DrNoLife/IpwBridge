using IpwBridge.Contracts.Enums;
using IpwBridge.Contracts.Models;

namespace IpwBridge.Contracts;

/// <summary>
/// Represents a CRUD (Create, Read, Update, Delete) request to the IPW Metazo API.
/// </summary>
/// <remarks>
/// The <see cref="IpwCrudRequest"/> class contains information required to perform a CRUD operation,
/// including the data type, the type of model operation, and the JSON payload.
/// </remarks>
public sealed class IpwCrudRequest
{
    /// <summary>
    /// Gets or sets the data type on which the CRUD operation is to be performed.
    /// </summary>
    public string Datatype { get; set; } = String.Empty;

    /// <summary>
    /// Gets or sets the model option indicating the type of operation (e.g., Create, Update, Delete).
    /// </summary>
    public CrudModel Model { get; set; }

    /// <summary>
    /// Gets or sets the JSON payload containing the data for the operation.
    /// </summary>
    public string JsonData { get; set; } = String.Empty;

    /// <summary>
    /// Gets or sets the object identifier.
    /// </summary>
    /// <remarks>
    /// This is required for update and delete operations.
    /// </remarks>
    public int? ObjectId { get; set; }

    public static IpwCrudRequest Create(string dataType, string jsonData) => new()
    {
        Datatype = dataType,
        JsonData = jsonData,
        Model = ModelOptions.Create
    };

    /// <summary>
    /// Created a <see cref="IpwCrudRequest"/> prefilled with the model for deletion.
    /// If used on a form, it also deleted all associated binfiles.
    /// </summary>
    /// <param name="dataType">Datatype of object to delete (except not really e.g. when deleting an object you can also say "binfile" and it will still work. I am not entirely sure how this works. Metazo is weird here. I've also tried just random characters, but that don't work, seems like it needs to be something in the system that exists?)</param>
    /// <param name="objectId">Object id of the item to delete.</param>
    public static IpwCrudRequest Delete(string dataType, int objectId) => new()
    {
        Datatype = dataType,
        ObjectId = objectId,
        Model = ModelOptions.Delete
    };
}

