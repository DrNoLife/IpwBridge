using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// A field to search on in a list request: a default field or a datatype field id such as <c>f276474</c>.
/// </summary>
/// <remarks>
/// Converts implicitly from <see cref="DefaultSearchFields"/> and from <see cref="string"/>. Enum values are converted to
/// the lowercase name the API expects; strings are sent as given. A <see langword="default"/> instance has no
/// value and is rejected when a request is sent.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// SearchField fromEnum = DefaultSearchFields.Created;
/// SearchField fromString = "created";
/// ]]></code>
/// </example>
public readonly struct SearchField : IEquatable<SearchField>
{
    /// <summary>Initializes a new instance with the given raw value.</summary>
    /// <param name="value">The value sent to the API.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public SearchField(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>Gets the value sent to the API, or <see langword="null"/> for a <see langword="default"/> instance.</summary>
    public string? Value { get; }

    /// <summary>Converts a <see cref="DefaultSearchFields"/> value to a <see cref="SearchField"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SearchField(DefaultSearchFields value)
        => new(value.ToString().ToLowerInvariant());

    /// <summary>Converts a <see cref="string"/> to a <see cref="SearchField"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SearchField(string value)
        => new(value);

    /// <summary>Converts a <see cref="SearchField"/> to its string value.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(SearchField value)
        => value.Value ?? string.Empty;

    /// <summary>Creates a <see cref="SearchField"/> from a <see cref="DefaultSearchFields"/> value.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static SearchField FromDefaultSearchFields(DefaultSearchFields value) => value;

    /// <summary>Creates a <see cref="SearchField"/> from a string.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static SearchField FromString(string value) => value;

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SearchField other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(SearchField other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <summary>Determines whether two instances are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if the values are equal.</returns>
    public static bool operator ==(SearchField left, SearchField right)
        => left.Equals(right);

    /// <summary>Determines whether two instances differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if the values differ.</returns>
    public static bool operator !=(SearchField left, SearchField right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
}
