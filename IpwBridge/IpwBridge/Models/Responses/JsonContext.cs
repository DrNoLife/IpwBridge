using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;
using System.Text.Json.Serialization;

namespace IpwBridge.Models.Responses;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(MetazoListResponse<MetazoListItem>))]
[JsonSerializable(typeof(MetazoItemObject))]
[JsonSerializable(typeof(MetazoItemResponse<MetazoItemObject>))]
[JsonSerializable(typeof(MetazoExplanationResponse))]
[JsonSerializable(typeof(MetazoDatatypesResponse))]
public partial class JsonContext : JsonSerializerContext
{
}
