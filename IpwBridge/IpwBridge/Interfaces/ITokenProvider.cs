namespace IpwBridge.Interfaces;

public interface ITokenProvider
{
    Task<string> GetTokenAsync();
    Task RefreshTokenAsync();
}
