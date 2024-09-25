using IpwBridge.Contracts;
using System.Text.Json;

namespace IpwBridge.Interfaces;

public interface IMetazoApiClient
{
    Task<JsonElement> GetDatatypesAsync();
    Task<JsonElement> GetExplanationAsync(string datatype);
    Task<JsonElement> GetListAsync(ListRequest dataRequest);
    Task<JsonElement> GetItemAsync(int objectId);
    Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel);
    Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest binfileUploadModel);
}
