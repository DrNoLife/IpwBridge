using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts.Models;

public readonly struct SearchField(string value)
{
    public string Value { get; } = value;

    public static implicit operator SearchField(DefaultSearchFields searchField)
        => new(searchField.ToString());

    public static implicit operator SearchField(string value)
        => new(value);

    public static implicit operator string(SearchField searchField)
        => searchField.Value;

    public override string ToString()
        => Value;
}
