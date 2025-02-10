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


### SendModelAsync(IpwCrudRequest crudModel)

```csharp
Dictionary<string, string> jsonData = new()
{
    { "state", "2" },
};

var fullJson = JsonSerializer.Serialize(jsonData);

IpwCrudRequest crudRequest = new()
{
    Datatype = "form121889",
    Model = ModelOptions.Create,
    JsonData = fullJson
};

var crudResponse = await _metazoApiClient.SendModelAsync(crudRequest);
```

*Note: You can also use ```ModelOptions.Update``` or ```ModelOptions.Delete```, however these both require the ```ObjectId``` property to be set.*

### UploadBinfileAsync(BinfileUploadRequest binfileUploadModel)

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


## Responses and Custom Models

As of version 1.2.0 some default models has been included to help with the ease of use when deserializing the response.

As an example, to get a list of all datatypes in the system, one had to do the following:

- Call ```.GetDatatypesAsync()```.
- Create a custom model, which modeled the response.
- Deserialize the response based on the newly created model.

Now, this has been replaced, and one can just simply call the method.

The interface the client is using is the following:

```csharp
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
```

As one can see, one can provide a custom object for both ```GetListAsync()``` and ```GetItemAsync```.
In order to work with this, create a new item which inherits the ```IMetazoListItem``` (for list results) or ```IMetazoItemObject``` (for item results) interface.

Examples:

```csharp
public class CustomIpwModelHere : IMetazoListItem
{
    [JsonPropertyName("f2842251")]
    public required string Number { get; set; }

    [JsonPropertyName("f2842252")]
    public required string Name { get; set; }

    [JsonPropertyName("f2842253")]
    public required string Description { get; set; }

    [JsonPropertyName("objectid")]
    public required string ObjectId { get; set; }

    [JsonPropertyName("language")]
    public required string Language { get; set; }
}
```

Furthermore, you can also add multiple interfaces to the same model, if you desire, and thus use it in multiple places:

```csharp
public class CustomIpwModelHere : IMetazoListItem, IMetazoItemObject
{
    [JsonPropertyName("f2842251")]
    public required string Number { get; set; }

    [JsonPropertyName("f2842252")]
    public required string Name { get; set; }

    [JsonPropertyName("f2842253")]
    public required string Description { get; set; }

    [JsonPropertyName("objectid")]
    public required string ObjectId { get; set; }

    [JsonPropertyName("language")]
    public required string Language { get; set; }

    [JsonPropertyName("site")]
    public string Site { get; set; } = String.Empty;
}
```

```csharp
var listResponse = await _metazoApiClient.GetListAsync<CustomIpwModelHere>(request);
```
```csharp
var itemResponse = await _metazoApiClient.GetItemAsync<CustomIpwModelHere>(2842317);
```

*Note: Some default items for ```GetListAsync``` and ```GetItemAsync``` are included:*

```csharp
var listResponse = await _metazoApiClient.GetListAsync<MetazoListItem>(request);
```
```csharp
var itemResponse = await _metazoApiClient.GetItemAsync<MetazoItemObject>(2842317);
```