namespace IpwBridge.Contracts.Enums;

/// <summary>
/// The comparison operators available for search conditions in a <see cref="ListRequest"/>.
/// </summary>
/// <remarks>
/// Converted to the uppercase operator name the API expects (for example <c>GreaterEqual</c> becomes <c>GREATEREQUAL</c>).
/// </remarks>
public enum SearchOperator
{
    /// <summary>The field contains the value.</summary>
    Like,
    /// <summary>The field starts with the value.</summary>
    LikeStart,
    /// <summary>The field ends with the value.</summary>
    LikeEnd,
    /// <summary>The field is greater than the value.</summary>
    Greater,
    /// <summary>The field is greater than or equal to the value.</summary>
    GreaterEqual,
    /// <summary>The field is less than the value.</summary>
    Less,
    /// <summary>The field is less than or equal to the value.</summary>
    LessEqual,
    /// <summary>The field equals the value.</summary>
    Equal,
    /// <summary>The field does not equal the value.</summary>
    NotEqual,
    /// <summary>The field sounds like the value.</summary>
    SoundsLike,
    /// <summary>The field does not sound like the value.</summary>
    NotSoundsLike,
    /// <summary>The field does not contain the value.</summary>
    NotLike,
    /// <summary>The field is one of a comma-separated list of values.</summary>
    In,
    /// <summary>The field is empty.</summary>
    IsEmpty,
    /// <summary>The date field is today.</summary>
    IsToday,
    /// <summary>The date field is before today.</summary>
    BeforeToday,
    /// <summary>The date field is after today.</summary>
    AfterToday,
    /// <summary>The date field is the given number of days ago.</summary>
    DaysAgo,
    /// <summary>The date field is the given number of days ahead.</summary>
    InDays,
    /// <summary>The date field is fewer than the given number of days ago.</summary>
    LessDaysAgo,
    /// <summary>The field is not one of a comma-separated list of values.</summary>
    NotIn,
    /// <summary>The field matches a regular expression.</summary>
    RLike,
    /// <summary>The value is in the field's comma-separated set.</summary>
    FindInSet,
    /// <summary>The field is not empty.</summary>
    IsNotEmpty,
    /// <summary>The date field is more than the given number of days ahead.</summary>
    InMoreDays
}
