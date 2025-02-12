using IpwBridge.Contracts;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;
using System.Text.Json;

namespace IpwBridge.Interfaces;

public interface IMetazoApiClient
{
    Task<MetazoDatatypesResponse?> GetDatatypesAsync(CancellationToken cancellationToken = default);
    Task<MetazoExplanationResponse?> GetExplanationAsync(string datatype, CancellationToken cancellationToken = default);
    Task<JsonElement> GetListAsync(ListRequest dataRequest, CancellationToken cancellationToken = default);
    Task<MetazoListResponse<T>?> GetListAsync<T>(ListRequest dataRequest, CancellationToken cancellationToken = default)
        where T : IMetazoListItem;

    Task<JsonElement> GetItemAsync(int objectId, CancellationToken cancellationToken = default);
    Task<MetazoItemResponse<T>?> GetItemAsync<T>(int objectId, CancellationToken cancellationToken = default)
        where T : IMetazoItemObject;

    Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel, CancellationToken cancellationToken = default);
    Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest binfileUploadModel, CancellationToken cancellationToken = default);
}
