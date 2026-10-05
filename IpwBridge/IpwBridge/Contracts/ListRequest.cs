using System.Globalization;
using IpwBridge.Contracts.Enums;
using IpwBridge.Contracts.Models;

namespace IpwBridge.Contracts;

/// <summary>
/// A request for a page of items of one datatype, with optional search conditions.
/// </summary>
/// <remarks>
/// Without conditions and without <see cref="FromDate"/>, no search parameters are sent and the API returns all
/// items (paged by <see cref="Limit"/> and <see cref="Offset"/>).
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// var request = new ListRequest
/// {
///     DataType = "form121889",
///     FieldsToGet = "f276474,f1628152",
///     Limit = 50,
/// }
/// .Where(DefaultSearchFields.ObjectId, SearchOperator.GreaterEqual, "2604436")
/// .Where("f276474", SearchOperator.Like, "Api");
/// ]]></code>
/// </example>
public sealed class ListRequest
{
    /// <summary>Gets or sets the datatype to list, for example <c>form121889</c>.</summary>
    public required string DataType { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of fields to return. The API always returns <c>objectid</c> and the
    /// datatype's primary field. When empty, the parameter is omitted.
    /// </summary>
    public string FieldsToGet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of items to return. <c>0</c> returns all items. Defaults to 20.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>Gets or sets how many items to skip. Defaults to 0.</summary>
    public int Offset { get; set; }

    /// <summary>Gets or sets how the search conditions are combined. Defaults to <see cref="SearchConnector.And"/>.</summary>
    public SearchConnection SearchAndOr { get; set; } = SearchConnector.And;

    /// <summary>Gets the search conditions. All conditions are combined with <see cref="SearchAndOr"/>.</summary>
    public IList<SearchCondition> Conditions { get; } = [];

    /// <summary>
    /// Gets or sets an optional lower bound on the <c>created</c> date. When set, a
    /// <c>created GREATEREQUAL yyyy-MM-dd</c> condition is added to <see cref="Conditions"/> when the request is sent.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Gets or sets whether inactive objects are included in the result.</summary>
    public bool IncludeInactive { get; set; }

    /// <summary>Adds a search condition and returns this request, so conditions can be chained.</summary>
    /// <param name="field">The field to search on.</param>
    /// <param name="operator">The comparison operator.</param>
    /// <param name="value">The value to compare with.</param>
    /// <returns>This request.</returns>
    public ListRequest Where(SearchField field, SearchOperation @operator, string value)
    {
        Conditions.Add(new SearchCondition(field, @operator, value));
        return this;
    }

    /// <summary>Creates a copy of this request with a different offset. Used for paging.</summary>
    internal ListRequest WithOffset(int offset)
    {
        var copy = new ListRequest
        {
            DataType = DataType,
            FieldsToGet = FieldsToGet,
            Limit = Limit,
            Offset = offset,
            SearchAndOr = SearchAndOr,
            FromDate = FromDate,
            IncludeInactive = IncludeInactive,
        };

        foreach (var condition in Conditions)
        {
            copy.Conditions.Add(condition);
        }

        return copy;
    }

    /// <summary>Validates the request and returns the query parameters it maps to (without token and checksum).</summary>
    internal Dictionary<string, string> ToQueryParameters()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(DataType, nameof(DataType));
        ArgumentOutOfRangeException.ThrowIfNegative(Limit, nameof(Limit));
        ArgumentOutOfRangeException.ThrowIfNegative(Offset, nameof(Offset));

        List<SearchCondition> conditions = [.. Conditions];
        if (FromDate is { } fromDate)
        {
            conditions.Add(new SearchCondition(
                DefaultSearchFields.Created,
                SearchOperator.GreaterEqual,
                fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        Dictionary<string, string> parameters = new(StringComparer.Ordinal)
        {
            ["datatype"] = DataType,
            ["limit"] = Limit.ToString(CultureInfo.InvariantCulture),
            ["offset"] = Offset.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(FieldsToGet))
        {
            parameters["fields"] = FieldsToGet;
        }

        if (conditions.Count > 0)
        {
            foreach (var condition in conditions)
            {
                ArgumentNullException.ThrowIfNull(condition, nameof(Conditions));
                ValidatePart(condition.Field.Value, "search field", allowEmpty: false);
                ValidatePart(condition.Operator.Value, "search operator", allowEmpty: false);
                ValidatePart(condition.Value, "search value", allowEmpty: true);
            }

            ValidatePart(SearchAndOr.Value, "search connector", allowEmpty: false);

            parameters["search"] = string.Join(';', conditions.Select(c => c.Value));
            parameters["searchfield"] = string.Join(';', conditions.Select(c => c.Field.Value));
            parameters["searchcomp"] = string.Join(';', conditions.Select(c => c.Operator.Value));
            parameters["searchandor"] = SearchAndOr.Value!;
        }

        if (IncludeInactive)
        {
            parameters["includeinactive"] = "1";
        }

        return parameters;
    }

    private static void ValidatePart(string? value, string what, bool allowEmpty)
    {
        if (value is null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
        {
            throw new ArgumentException($"The {what} of a search condition must be set.");
        }

        if (value.Contains(';', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The {what} '{value}' contains ';', which the Metazo API uses to separate conditions.");
        }
    }
}
