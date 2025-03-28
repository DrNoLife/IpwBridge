using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents an operation used in search comparisons for filtering data in the IPW Metazo API.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="SearchOperator"/> and <see cref="string"/>.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// // Example: Implicit conversion of a SearchOperator enum to SearchOperation.
/// SearchOperation operation = SearchOperator.LessEqual;
/// // Example: Explicit setting of string value.
/// SearchOperation operation = "LESSEQUAL";
/// ]]></code>
/// </example>
public readonly struct SearchOperation(string value) : IEquatable<SearchOperation>
{
    /// <summary>
    /// Gets the string value representing the search operation.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Implicitly converts a <see cref="SearchOperator"/> value to a <see cref="SearchOperation"/>.
    /// </summary>
    /// <param name="searchOperation">The <see cref="SearchOperator"/> value to convert.</param>
    public static implicit operator SearchOperation(SearchOperator searchOperation)
        => new(searchOperation.ToString().ToUpper());

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="SearchOperation"/>.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    public static implicit operator SearchOperation(string value)
        => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="SearchOperation"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="searchOperation">The <see cref="SearchOperation"/> instance.</param>
    public static implicit operator string(SearchOperation searchOperation)
        => searchOperation.Value;

    /// <summary>
    /// Returns a string that represents the current search operation.
    /// </summary>
    /// <returns>A string that represents the current search operation.</returns>
    public override string ToString() => Value;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SearchOperation other && Equals(other);

    public bool Equals(SearchOperation other)
        => String.Equals(Value, other.Value, StringComparison.Ordinal);

    public static bool operator ==(SearchOperation left, SearchOperation right)
        => left.Equals(right);

    public static bool operator !=(SearchOperation left, SearchOperation right)
        => !(left == right);

    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Value);
}