namespace IpwBridge.Interfaces.Services;

public interface IFileChecksumService
{
    Task<string> CalculateChecksumAsync(Stream fileContent, CancellationToken cancellationToken = default);
}
