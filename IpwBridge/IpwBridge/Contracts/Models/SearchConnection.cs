using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents a logical connector used to combine multiple search criteria.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="SearchConnector"/> and <see cref="string"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SearchConnection"/> struct.
/// </remarks>
/// <param name="value">The string value representing the search connection.</param>
public readonly struct SearchConnection(string value)
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
        => new(searchConnector.ToString());

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
}