using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents a logical connector used to combine multiple search criteria.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="SearchConnector"/> and <see cref="string"/>.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// // Example: Implicit conversion of a SearchConnector enum to SearchConnection.
/// SearchConnection connection = SearchConnector.Or;
/// // Example: Explicit setting of string value.
/// SearchConnection connection = "OR";
/// ]]></code>
/// </example>
public readonly struct SearchConnection(string value) : IEquatable<SearchConnection>
{
    /// <summary>
    /// Gets the string value representing the search connection.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Implicitly converts a <see cref="SearchConnector"/> value to a <see cref="SearchConnection"/>.
    /// </summary>
    /// <param name="searchConnector">The <see cref="SearchConnector"/> value to convert.</param>
    public static implicit operator SearchConnection(SearchConnector searchConnector)
        => new(searchConnector.ToString().ToUpper());

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="SearchConnection"/>.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    public static implicit operator SearchConnection(string value)
        => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="SearchConnection"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="searchConnection">The <see cref="SearchConnection"/> instance.</param>
    public static implicit operator string(SearchConnection searchConnection)
        => searchConnection.Value;

    /// <summary>
    /// Returns a string that represents the current search connection.
    /// </summary>
    /// <returns>A string that represents the current search connection.</returns>
    public override string ToString() => Value;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SearchConnection other && Equals(other);

    public bool Equals(SearchConnection other)
        => String.Equals(Value, other.Value, StringComparison.Ordinal);

    public static bool operator ==(SearchConnection left, SearchConnection right)
        => left.Equals(right);

    public static bool operator !=(SearchConnection left, SearchConnection right)
        => !(left == right);

    public override int GetHashCode() 
        => StringComparer.Ordinal.GetHashCode(Value);

}