using IpwBridge.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IpwBridge.Services;

public class ChecksumService : IChecksumService
{
    public string CalculateChecksum(Dictionary<string, string> parameters, string secret, string? jsonPayload = null)
    {
        List<KeyValuePair<string, string>> keyValuePairs = new(parameters);

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
