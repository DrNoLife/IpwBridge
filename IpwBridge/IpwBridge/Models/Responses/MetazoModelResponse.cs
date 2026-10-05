using System.Globalization;
using System.Text.Json;

namespace IpwBridge.Models.Responses;

/// <summary>The response from <c>/model</c>.</summary>
public sealed class MetazoModelResponse
{
    internal MetazoModelResponse(JsonElement raw)
    {
        Raw = raw;
        if (raw.ValueKind == JsonValueKind.Object && raw.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object)
        {
            if (result.TryGetProperty("objectid", out var objectId))
            {
                ObjectId = objectId.ValueKind switch
                {
                    JsonValueKind.Number when objectId.TryGetInt32(out int number) => number,
                    JsonValueKind.String when int.TryParse(objectId.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => parsed,
                    _ => null,
                };
            }

            if (result.TryGetProperty("model", out var model) && model.ValueKind == JsonValueKind.String)
            {
                Model = model.GetString();
            }

            if (result.TryGetProperty("modelresult", out var modelResult))
            {
                ModelResult = modelResult;
            }
        }
    }

    /// <summary>Gets the id of the object the model ran on (for create: the new object).</summary>
    public int? ObjectId { get; }

    /// <summary>Gets the name of the model that ran.</summary>
    public string? Model { get; }

    /// <summary>Gets the model-specific result value, if any.</summary>
    public JsonElement? ModelResult { get; }

    /// <summary>Gets the complete response as returned by the API.</summary>
    public JsonElement Raw { get; }
}
