# IpwBridge
C# wrapper for the IPW Metazo API

## How to use

Use the extension method ```AddIpwBridge()```.

```csharp
builder.Services.AddIpwBridge(options =>
{
    options.IpwUrl = "https://your-domain.dk/metazo/api/v1/";
    options.IpwUser = "USERNAME";
    options.IpwPassword = "PASSWORD";
    options.ChecksumSecret = "CHECKSUM-SECRET";
});
```

Which will setup DI for the ```IMetazoApiClient``` service.

Example:
```csharp
public class Worker(IMetazoApiClient metazoApiClient) : BackgroundService
{
    private readonly IMetazoApiClient _metazoApiClient = metazoApiClient;
}
```

## Client methods

The ```IMetazoApiClient``` interface exposes a few methods to use:

```csharp
Task<JsonElement> GetDatatypesAsync();
Task<JsonElement> GetExplanationAsync(string datatype);
Task<JsonElement> GetListAsync(ListRequest dataRequest);
Task<JsonElement> GetItemAsync(int objectId);
Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel);
Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest binfileUploadModel);
```

*Note: Not every single endpoint from the API has been covered. The project is being updated with endpoints as I find a need for them.*

### GetDatatypesAsync()

```csharp
var datatypes = await _metazoApiClient.GetDatatypesAsync()
```

### GetExplanationAsync(string datatype)

```csharp
string datatype = "form113622";
var explanation = await _metazoApiClient.GetExplanationAsync(datatype);
```

### GetListAsync(ListRequest dataRequest)

```csharp
// Find all entries in the form, that has an objectId greater than '2604436'.
// The entries we find, we also want to get their fields as specified in 'FieldsToGet'.
ListRequest request = new()
{
    DataType = "form121889",
    FieldsToGet = "f276474,f1628152,f2605112",
    SearchAfter = "2604436",
    SearchField = "objectid",
    SearchOperation = "GREATER"
};
wait _metazoApiClient.GetListAsync(request)
```

### GetItemAsync(int objectId)

```csharp
var item = await _metazoApiClient.GetItemAsync(2605115);
```


## SendModelAsync(IpwCrudRequest crudModel)

```csharp

```

*Note: I think this might not be working right now. Am still working on it.*

## UploadBinfileAsync(BinfileUploadRequest binfileUploadModel)

```csharp
using var fileStream = File.OpenRead("testimg.png");
BinfileUploadRequest uploadRequest = new()
{
    ParentId = 2605115,
    Files = new()
    {
        { "file_1", fileStream }
    }
};
WritePretty(await _metazoApiClient.UploadBinfileAsync(uploadRequest));
```

*Note: While I haven't tested it, it should also be able to handle multiple files being uploaded at once.*