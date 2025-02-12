using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents a search field used for filtering items in the IPW Metazo API.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="DefaultSearchFields"/> and <see cref="string"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SearchField"/> struct.
/// </remarks>
/// <param name="value">The string value representing the search field.</param>
public readonly struct SearchField(string value)
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
        => new(searchField.ToString());

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
}