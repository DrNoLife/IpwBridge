using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents a search field used for filtering items in the IPW Metazo API.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="DefaultSearchFields"/> and <see cref="string"/>.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// // Example: Implicit conversion of a DefaultSearchFields enum to SearchField.
/// SearchField field = DefaultSearchFields.Created;
/// // Example: Explicit setting of string value.
/// SearchField field = "created";
/// ]]></code>
/// </example>
public readonly struct SearchField(string value) : IEquatable<SearchField>
{
    /// <summary>
    /// Gets the string value representing the search field.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Implicitly converts a <see cref="DefaultSearchFields"/> value to a <see cref="SearchField"/>.
    /// </summary>
    /// <param name="searchField">The <see cref="DefaultSearchFields"/> value to convert.</param>
    public static implicit operator SearchField(DefaultSearchFields searchField)
        => new(searchField.ToString().ToLower());

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="SearchField"/>.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    public static implicit operator SearchField(string value)
        => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="SearchField"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="searchField">The <see cref="SearchField"/> instance.</param>
    public static implicit operator string(SearchField searchField)
        => searchField.Value;

    /// <summary>
    /// Returns a string that represents the current search field.
    /// </summary>
    /// <returns>A string that represents the current search field.</returns>
    public override string ToString() => Value;

    public override bool Equals([NotNullWhen(true)] object? obj)
    => obj is SearchField other && Equals(other);

    public bool Equals(SearchField other)
        => String.Equals(Value, other.Value, StringComparison.Ordinal);

    public static bool operator ==(SearchField left, SearchField right)
        => left.Equals(right);

    public static bool operator !=(SearchField left, SearchField right)
        => !(left == right);

    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Value);
}