using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents an operation used in search comparisons for filtering data in the IPW Metazo API.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="SearchOperator"/> and <see cref="string"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SearchOperation"/> struct.
/// </remarks>
/// <param name="value">The string value representing the search operation.</param>
public readonly struct SearchOperation(string value)
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
        => new(searchOperation.ToString());

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
}