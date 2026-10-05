using System.Text.Json;
using System.Text.Json.Serialization;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;

namespace IpwBridge.Models.Responses;

/// <summary>Source-generated JSON metadata for every type IpwBridge itself deserializes.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(MetazoAuthenticationResponse))]
[JsonSerializable(typeof(MetazoDatatypesResponse))]
[JsonSerializable(typeof(MetazoExplanationResponse))]
[JsonSerializable(typeof(MetazoListResponse<MetazoListItem>))]
[JsonSerializable(typeof(MetazoItemResponse<MetazoItemObject>))]
internal sealed partial class JsonContext : JsonSerializerContext
{
}
