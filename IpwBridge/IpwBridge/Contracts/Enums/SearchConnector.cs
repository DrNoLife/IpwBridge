namespace IpwBridge.Contracts.Enums;

/// <summary>
/// Specifies the logical connector to be used when combining multiple search conditions.
/// </summary>
/// <remarks>
/// This enum is used to determine whether search criteria should be combined using a logical AND or OR.
/// </remarks>
public enum SearchConnector
{
    And,
    Or
}
