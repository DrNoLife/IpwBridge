namespace IpwBridge.Contracts.Enums;

/// <summary>
/// Enumerates the operators available for search comparisons in the IPW Metazo API.
/// </summary>
/// <remarks>
/// These operators allow filtering based on various conditions such as equality, inequality,
/// pattern matching, and date comparisons.
/// </remarks>
public enum SearchOperator
{
    Like,
    LikeStart,
    LikeEnd,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    Equal,
    NotEqual,
    SoundsLike,
    NotSoundsLike,
    NotLike,
    In,
    IsEmpty,
    IsToday,
    BeforeToday,
    AfterToday,
    DaysAgo,
    InDays,
    LessDaysAgo,
    NotIn,
    RLike,
    FindInSet,
    IsNotEmpty,
    InMoreDays
}
