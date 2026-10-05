namespace IpwBridge.Contracts.Enums;

/// <summary>
/// The logical connector used to combine the search conditions of a <see cref="ListRequest"/>.
/// </summary>
public enum SearchConnector
{
    /// <summary>All conditions must match.</summary>
    And,
    /// <summary>At least one condition must match.</summary>
    Or
}
