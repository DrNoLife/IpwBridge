using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// The logical connector (<c>AND</c>/<c>OR</c>) that combines the search conditions of a list request.
/// </summary>
/// <remarks>
/// Converts implicitly from <see cref="SearchConnector"/> and from <see cref="string"/>. Enum values are converted to
/// the uppercase name the API expects; strings are sent as given. A <see langword="default"/> instance has no
/// value and is rejected when a request is sent.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// SearchConnection fromEnum = SearchConnector.Or;
/// SearchConnection fromString = "OR";
/// ]]></code>
/// </example>
public readonly struct SearchConnection : IEquatable<SearchConnection>
{
    /// <summary>Initializes a new instance with the given raw value.</summary>
    /// <param name="value">The value sent to the API.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public SearchConnection(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>Gets the value sent to the API, or <see langword="null"/> for a <see langword="default"/> instance.</summary>
    public string? Value { get; }

    /// <summary>Converts a <see cref="SearchConnector"/> value to a <see cref="SearchConnection"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SearchConnection(SearchConnector value)
        => new(value.ToString().ToUpperInvariant());

    /// <summary>Converts a <see cref="string"/> to a <see cref="SearchConnection"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SearchConnection(string value)
        => new(value);

    /// <summary>Converts a <see cref="SearchConnection"/> to its string value.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(SearchConnection value)
        => value.Value ?? string.Empty;

    /// <summary>Creates a <see cref="SearchConnection"/> from a <see cref="SearchConnector"/> value.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static SearchConnection FromSearchConnector(SearchConnector value) => value;

    /// <summary>Creates a <see cref="SearchConnection"/> from a string.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static SearchConnection FromString(string value) => value;

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SearchConnection other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(SearchConnection other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <summary>Determines whether two instances are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if the values are equal.</returns>
    public static bool operator ==(SearchConnection left, SearchConnection right)
        => left.Equals(right);

    /// <summary>Determines whether two instances differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if the values differ.</returns>
    public static bool operator !=(SearchConnection left, SearchConnection right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
}
