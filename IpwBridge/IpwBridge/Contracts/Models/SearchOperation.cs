using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts.Models;

public readonly struct SearchOperation(string value)
{
    public string Value { get; } = value;

    public static implicit operator SearchOperation(SearchOperator searchOperation)
        => new(searchOperation.ToString());

    public static implicit operator SearchOperation(string value)
        => new(value);

    public static implicit operator string(SearchOperation searchOperation)
        => searchOperation.Value;

    public override string ToString()
        => Value;
}
