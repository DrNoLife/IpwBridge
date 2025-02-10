using IpwBridge.Contracts;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;
using System.Text.Json;

namespace IpwBridge.Interfaces;

public interface IMetazoApiClient
{
    Task<MetazoDatatypesResponse?> GetDatatypesAsync();
    Task<MetazoExplanationResponse?> GetExplanationAsync(string datatype);
    Task<JsonElement> GetListAsync(ListRequest dataRequest);
    Task<MetazoListResponse<T>?> GetListAsync<T>(ListRequest dataRequest) 
        where T : IMetazoListItem;

    Task<JsonElement> GetItemAsync(int objectId);
    Task<MetazoItemResponse<T>?> GetItemAsync<T>(int objectId) 
        where T : IMetazoItemObject;

    Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel);
    Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest binfileUploadModel);
}
