using System.Text.Json;

namespace IpwBridge.Serialization;

/// <summary>
/// The <c>success</c>, <c>error</c> and <c>message</c> fields every Metazo JSON response may carry. They are read
/// directly from the response bytes, so failure detection works whatever metadata the caller's types use.
/// </summary>
internal readonly record struct MetazoEnvelope(bool? Success, string? Error, string? Message)
{
    /// <summary>Gets the most specific error text: <c>"error: message"</c>, or whichever of the two is present.</summary>
    public string? ServerMessage => Error is not null && Message is not null && Message != Error
        ? $"{Error}: {Message}"
        : Message ?? Error;

    /// <summary>Gets whether the body was a JSON object (as opposed to text or another JSON value).</summary>
    public bool IsJsonObject { get; init; }

    /// <summary>Reads the envelope fields of a JSON body. Throws <see cref="JsonException"/> for invalid JSON.</summary>
    public static MetazoEnvelope Read(ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return default;
        }

        bool? success = null;
        string? error = null;
        string? message = null;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string name = reader.GetString()!;
            reader.Read();
            switch (name)
            {
                case "success":
                    success = ReadBoolean(ref reader);
                    break;
                case "error":
                    error = ReadText(ref reader);
                    break;
                case "message":
                    message = ReadText(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new MetazoEnvelope(success, error, message) { IsJsonObject = true };
    }

    /// <summary>Tries to read the envelope of a body that may not be (complete) JSON.</summary>
    public static MetazoEnvelope? TryRead(ReadOnlySpan<byte> body)
    {
        try
        {
            MetazoEnvelope envelope = Read(body);
            return envelope.IsJsonObject ? envelope : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Reads booleans that Metazo sends as <c>true</c>, <c>"true"</c>, <c>"1"</c> or <c>1</c>.</summary>
    public static bool? ReadBoolean(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => element.TryGetInt64(out long number) ? number != 0 : null,
        JsonValueKind.String => ParseBoolean(element.GetString()),
        _ => null,
    };

    private static bool? ReadBoolean(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out long number) ? number != 0 : null;
            case JsonTokenType.String:
                return ParseBoolean(reader.GetString());
            default:
                reader.Skip();
                return null;
        }
    }

    private static string? ReadText(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Null:
                return null;
            default:
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.GetRawText();
                }
        }
    }

    private static bool? ParseBoolean(string? text)
    {
        if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1")
        {
            return true;
        }

        if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) || text == "0")
        {
            return false;
        }

        return null;
    }
}
