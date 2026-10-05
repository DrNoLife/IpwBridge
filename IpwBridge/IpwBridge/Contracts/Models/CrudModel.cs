using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// The model to run on an object through <c>/model</c>, such as <c>create</c> or a datatype-specific model.
/// </summary>
/// <remarks>
/// Converts implicitly from <see cref="ModelOptions"/> and from <see cref="string"/>. Enum values are converted to
/// the lowercase name the API expects. Model names are always sent in lowercase, as in 1.x, because
/// the API uses lowercase model names. A <see langword="default"/> instance has no
/// value and is rejected when a request is sent.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// CrudModel fromEnum = ModelOptions.Create;
/// CrudModel fromString = "create";
/// ]]></code>
/// </example>
public readonly struct CrudModel : IEquatable<CrudModel>
{
    /// <summary>Initializes a new instance with the given raw value.</summary>
    /// <param name="value">The value sent to the API.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public CrudModel(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>Gets the value sent to the API, or <see langword="null"/> for a <see langword="default"/> instance.</summary>
    public string? Value { get; }

    /// <summary>Converts a <see cref="ModelOptions"/> value to a <see cref="CrudModel"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator CrudModel(ModelOptions value)
        => new(value.ToString().ToLowerInvariant());

    /// <summary>Converts a <see cref="string"/> to a <see cref="CrudModel"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator CrudModel(string value)
        => new(value);

    /// <summary>Converts a <see cref="CrudModel"/> to its string value.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(CrudModel value)
        => value.Value ?? string.Empty;

    /// <summary>Creates a <see cref="CrudModel"/> from a <see cref="ModelOptions"/> value.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static CrudModel FromModelOptions(ModelOptions value) => value;

    /// <summary>Creates a <see cref="CrudModel"/> from a string.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static CrudModel FromString(string value) => value;

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is CrudModel other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(CrudModel other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <summary>Determines whether two instances are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if the values are equal.</returns>
    public static bool operator ==(CrudModel left, CrudModel right)
        => left.Equals(right);

    /// <summary>Determines whether two instances differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if the values differ.</returns>
    public static bool operator !=(CrudModel left, CrudModel right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
}
