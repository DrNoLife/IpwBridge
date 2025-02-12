using IpwBridge.Contracts.Enums;

namespace IpwBridge.Contracts.Models;

public readonly struct SearchConnection(string value)
{
    public string Value { get; } = value;

    public static implicit operator SearchConnection(SearchConnector searchConnector)
        => new(searchConnector.ToString());

    public static implicit operator SearchConnection(string value)
        => new(value);

    public static implicit operator string(SearchConnection searchConnection)
        => searchConnection.Value;

    public override string ToString() => Value;
}
