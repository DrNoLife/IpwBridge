using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts;

/// <summary>
/// Represents a CRUD (Create, Read, Update, Delete) request to the IPW Metazo API.
/// </summary>
/// <remarks>
/// The <see cref="IpwCrudRequest"/> class contains information required to perform a CRUD operation,
/// including the data type, the type of model operation, and the JSON payload.
/// </remarks>
public class IpwCrudRequest
{
    /// <summary>
    /// Gets or sets the data type on which the CRUD operation is to be performed.
    /// </summary>
    public string Datatype { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model option indicating the type of operation (e.g., Create, Update, Delete).
    /// </summary>
    public ModelOptions Model { get; set; }

    /// <summary>
    /// Gets or sets the JSON payload containing the data for the operation.
    /// </summary>
    public string JsonData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object identifier.
    /// </summary>
    /// <remarks>
    /// This is required for update and delete operations.
    /// </remarks>
    public int? ObjectId { get; set; }
}

