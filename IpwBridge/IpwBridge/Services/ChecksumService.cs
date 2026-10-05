using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IpwBridge.Models;
using Microsoft.Extensions.Options;

namespace IpwBridge.Services;

/// <summary>
/// Calculates the HMAC-SHA1 request checksum described in the Metazo documentation.
/// </summary>
/// <remarks>
/// The documented PHP reference sorts the parameters by key (<c>ksort</c>), concatenates <c>key . value</c> and
/// signs the result with the checksum secret. This implementation follows it: keys are used exactly as sent and
/// sorted ordinally. For <c>/model</c>, the top-level properties of the JSON body are included as parameters;
/// booleans and <c>null</c> are converted the way PHP converts them to strings (<c>true</c> becomes <c>"1"</c>,
/// <c>false</c> and <c>null</c> become <c>""</c>). Numbers, objects and arrays keep their JSON text, which may not
/// match the server for values such as <c>1.50</c>; send field values as strings to be safe.
/// </remarks>
internal sealed class ChecksumService(IOptions<MetazoApiOptions> options)
{
    private readonly byte[] _secret = Encoding.UTF8.GetBytes(options.Value.ChecksumSecret);

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "HMAC-SHA1 is mandated by the Metazo API protocol.")]
    public string Calculate(IReadOnlyDictionary<string, string> parameters, string? jsonPayload = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        SortedDictionary<string, string> sorted = new(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
        {
            Add(sorted, key, value);
        }

        if (!string.IsNullOrWhiteSpace(jsonPayload))
        {
            using var document = JsonDocument.Parse(jsonPayload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("The JSON data must be a JSON object.", nameof(jsonPayload));
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                Add(sorted, property.Name, ToPhpString(property.Value));
            }
        }

        StringBuilder message = new();
        foreach (var (key, value) in sorted)
        {
            message.Append(key).Append(value);
        }

        byte[] hash = HMACSHA1.HashData(_secret, Encoding.UTF8.GetBytes(message.ToString()));
        return Hex.ToLower(hash);
    }

    private static void Add(SortedDictionary<string, string> sorted, string key, string value)
    {
        if (!sorted.TryAdd(key, value))
        {
            throw new ArgumentException(
                $"The parameter '{key}' appears more than once; JSON fields may not reuse the name of a query "
                + "parameter such as datatype, model, objectid or token.");
        }
    }

    private static string ToPhpString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.True => "1",
        JsonValueKind.False or JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        _ => value.GetRawText(),
    };

}
