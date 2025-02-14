namespace IpwBridge.Interfaces.Services;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}