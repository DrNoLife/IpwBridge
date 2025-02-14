using IpwBridge.Interfaces.Services;
using System.Security.Cryptography;

namespace IpwBridge.Services;

public class FileChecksumService : IFileChecksumService
{
    public async Task<string> CalculateChecksumAsync(Stream fileContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileContent);

        // Ensure the stream is seekable.
        if (!fileContent.CanSeek)
        {
            var tempStream = new MemoryStream();
            await fileContent.CopyToAsync(tempStream, cancellationToken);
            tempStream.Position = 0;
            fileContent = tempStream;
        }

        fileContent.Position = 0;
        byte[] buffer = new byte[256];
        int bytesRead = await fileContent.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        byte[] hash = SHA1.HashData(buffer.AsSpan(0, bytesRead));
        fileContent.Position = 0; 

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
