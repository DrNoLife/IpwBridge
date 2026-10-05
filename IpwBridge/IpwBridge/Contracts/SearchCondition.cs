using IpwBridge.Contracts.Models;

namespace IpwBridge.Contracts;

/// <summary>
/// One search condition of a <see cref="ListRequest"/>: a field, an operator and the value to compare with.
/// </summary>
/// <param name="Field">The field to search on, for example <c>DefaultSearchFields.ObjectId</c> or <c>"f276474"</c>.</param>
/// <param name="Operator">The comparison operator.</param>
/// <param name="Value">
/// The value to compare with. Use a comma-separated list for <c>In</c>/<c>NotIn</c>, and an empty string for
/// operators that take no value, such as <c>IsEmpty</c>. Semicolons are not allowed, because the API uses them
/// to separate conditions.
/// </param>
public sealed record SearchCondition(SearchField Field, SearchOperation Operator, string Value);
