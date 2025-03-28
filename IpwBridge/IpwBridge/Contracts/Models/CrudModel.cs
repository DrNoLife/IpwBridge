using IpwBridge.Contracts.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IpwBridge.Contracts.Models;

/// <summary>
/// Represents a CRUD model used for performing create, read, update, and delete operations in the IPW system.
/// </summary>
/// <remarks>
/// This readonly struct supports implicit conversion from both <see cref="ModelOptions"/> and <see cref="string"/>.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// // Example: Implicit conversion of a ModelOptions enum to CrudModel.
/// CrudModel model = ModelOptions.Create;
/// // Example: Explicit setting of string value.
/// CrudModel model = "CREATE";
/// ]]></code>
/// </example>
public readonly struct CrudModel(string value) : IEquatable<CrudModel>
{
    /// <summary>
    /// Gets the string value representing the CRUD model.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Implicitly converts a <see cref="ModelOptions"/> value to a <see cref="CrudModel"/>.
    /// </summary>
    /// <param name="modelOption">The <see cref="ModelOptions"/> value to convert.</param>
    public static implicit operator CrudModel(ModelOptions modelOption)
        => new(modelOption.ToString().ToUpper());

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CrudModel"/>.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    public static implicit operator CrudModel(string value)
        => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="CrudModel"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="crudModel">The <see cref="CrudModel"/> instance.</param>
    public static implicit operator string(CrudModel crudModel)
        => crudModel.Value;

    /// <summary>
    /// Returns a string that represents the current CRUD model.
    /// </summary>
    /// <returns>A string that represents the current CRUD model.</returns>
    public override string ToString() => Value;

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="CrudModel"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="CrudModel"/>.</param>
    /// <returns>true if the specified object is equal to the current <see cref="CrudModel"/>; otherwise, false.</returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is CrudModel other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="CrudModel"/> is equal to the current <see cref="CrudModel"/>.
    /// </summary>
    /// <param name="other">The <see cref="CrudModel"/> to compare with the current <see cref="CrudModel"/>.</param>
    /// <returns>true if the specified <see cref="CrudModel"/> is equal to the current <see cref="CrudModel"/>; otherwise, false.</returns>
    public bool Equals(CrudModel other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether two specified instances of <see cref="CrudModel"/> are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CrudModel"/> to compare.</param>
    /// <param name="right">The second <see cref="CrudModel"/> to compare.</param>
    /// <returns>true if the two <see cref="CrudModel"/> instances are equal; otherwise, false.</returns>
    public static bool operator ==(CrudModel left, CrudModel right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="CrudModel"/> are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CrudModel"/> to compare.</param>
    /// <param name="right">The second <see cref="CrudModel"/> to compare.</param>
    /// <returns>true if the two <see cref="CrudModel"/> instances are not equal; otherwise, false.</returns>
    public static bool operator !=(CrudModel left, CrudModel right)
        => !(left == right);

    /// <summary>
    /// Returns a hash code for the current <see cref="CrudModel"/>.
    /// </summary>
    /// <returns>A hash code for the current <see cref="CrudModel"/>.</returns>
    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Value);
}
