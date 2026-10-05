using System.Text.Json;

namespace IpwBridge.Models.Responses;

/// <summary>The response from <c>/binfile/upload</c>.</summary>
public sealed class MetazoUploadResponse
{
    internal MetazoUploadResponse(JsonElement raw)
    {
        Raw = raw;
        Dictionary<string, string> files = new(StringComparer.Ordinal);

        if (raw.ValueKind == JsonValueKind.Object && raw.TryGetProperty("uploadedfiles", out var uploaded))
        {
            IEnumerable<JsonElement> groups = uploaded.ValueKind switch
            {
                JsonValueKind.Array => uploaded.EnumerateArray(),
                JsonValueKind.Object => [uploaded],
                _ => [],
            };

            foreach (var group in groups.Where(g => g.ValueKind == JsonValueKind.Object))
            {
                foreach (var file in group.EnumerateObject())
                {
                    files[file.Name] = file.Value.ValueKind == JsonValueKind.String ? file.Value.GetString()! : file.Value.GetRawText();
                }
            }
        }

        UploadedFiles = files;
    }

    /// <summary>Gets the new binfile ids, keyed by the file keys of the request.</summary>
    public IReadOnlyDictionary<string, string> UploadedFiles { get; }

    /// <summary>Gets the complete response as returned by the API.</summary>
    public JsonElement Raw { get; }
}
