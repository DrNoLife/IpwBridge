using IpwBridge.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IpwBridge.Services;

/// <summary>
/// Provides functionality for calculating checksums to secure API requests.
/// </summary>
/// <remarks>
/// The checksum is generated based on the provided parameters and an optional JSON payload,
/// using an HMAC algorithm with a secret key. This ensures data integrity and authenticity.
/// </remarks>
public class ChecksumService : IChecksumService
{
    /// <summary>
    /// Calculates a checksum for the given parameters and secret, optionally including a JSON payload.
    /// </summary>
    /// <param name="parameters">A dictionary of parameters to be included in the checksum calculation.</param>
    /// <param name="secret">The secret key used to compute the HMAC.</param>
    /// <param name="jsonPayload">
    /// An optional JSON payload whose properties will be parsed and included in the checksum calculation.
    /// </param>
    /// <returns>A lowercase hexadecimal string representing the computed checksum.</returns>
    public string CalculateChecksum(Dictionary<string, string> parameters, string secret, string? jsonPayload = null)
    {
        List<KeyValuePair<string, string>> keyValuePairs = [.. parameters];

        if (!String.IsNullOrEmpty(jsonPayload))
        {
            var jsonDocument = JsonDocument.Parse(jsonPayload);
            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                keyValuePairs.Add(new KeyValuePair<string, string>(
                    property.Name, property.Value.ToString()));
            }
        }

        var sortedKeyValuePairs = keyValuePairs
            .OrderBy(kvp => kvp.Key)
            .ToList();

        var message = String.Concat(sortedKeyValuePairs.Select(
            kvp => kvp.Key.ToLower() + kvp.Value));

        using HMACSHA1 hasher = new(Encoding.UTF8.GetBytes(secret));
        var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(message));

        return BitConverter
            .ToString(hash)
            .Replace("-", "")
            .ToLowerInvariant();
    }
}
