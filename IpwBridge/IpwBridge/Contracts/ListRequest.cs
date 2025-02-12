using IpwBridge.Contracts.Enums;
using IpwBridge.Contracts.Models;

namespace IpwBridge.Contracts;

/// <summary>
/// Represents a request to retrieve a list of items from the IPW Metazo API using filtering, sorting, and pagination.
/// </summary>
/// <remarks>
/// The <see cref="ListRequest"/> class contains properties for specifying the data type, which fields to retrieve,
/// limits and offsets for pagination, as well as filtering options such as search criteria and date ranges.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// // Example: Creating a ListRequest using enums (implicit conversion to structs)
/// ListRequest request = new ListRequest
/// {
///     DataType = "form121889",
///     FieldsToGet = "f276474,f1628152,f2605112",
///     Limit = 20,
///     Offset = 0,
///     SearchAndOr = SearchConnector.And,           // Implicit conversion to SearchConnection.
///     SearchField = DefaultSearchFields.ObjectId,    // Implicit conversion to SearchField.
///     SearchOperation = SearchOperator.GreaterEqual, // Implicit conversion to SearchOperation.
///     SearchAfter = "2604436",
///     FromDate = DateTime.UtcNow.AddDays(-30)
/// };
/// ]]></code>
/// </example>
public class ListRequest
{
    /// <summary>
    /// Gets or sets the data type to be queried.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a comma-separated list of fields to retrieve in the response.
    /// </summary>
    public string FieldsToGet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of items to return.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Gets or sets the zero-based offset from which to start retrieving items.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Gets or sets the logical connector to use when combining multiple search criteria.
    /// </summary>
    /// <remarks>
    /// This property accepts both a string value and a <see cref="SearchConnector"/> value via implicit conversion.
    /// </remarks>
    public SearchConnection SearchAndOr { get; set; } = SearchConnector.And;

    /// <summary>
    /// Gets or sets the field by which the search is to be performed.
    /// </summary>
    /// <remarks>
    /// This property accepts both a string value and a <see cref="DefaultSearchFields"/> value via implicit conversion.
    /// </remarks>
    public SearchField SearchField { get; set; } = DefaultSearchFields.Created;

    /// <summary>
    /// Gets or sets the operator to use when comparing the search value.
    /// </summary>
    /// <remarks>
    /// This property accepts both a string value and a <see cref="SearchOperator"/> value via implicit conversion.
    /// </remarks>
    public SearchOperation SearchOperation { get; set; } = SearchOperator.GreaterEqual;

    /// <summary>
    /// Gets or sets the search value used for filtering.
    /// </summary>
    /// <remarks>
    /// This can be an identifier, a part of a name, or any other value against which the search is performed.
    /// </remarks>
    public string SearchAfter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting date from which items should be considered.
    /// </summary>
    /// <remarks>
    /// The default is set to 30 days before the current UTC date.
    /// </remarks>
    public DateTime FromDate { get; set; } = DateTime.UtcNow.AddDays(-30);
}
