# IpwBridge

**C# wrapper for the IPW Metazo API**

IpwBridge is a lightweight, modern library for interacting with the IPW Metazo API. It abstracts away the complexity of token authentication, checksum calculation, and HTTP communication, so you can focus on your business logic.

## Features

- **Simple Integration:** Easily register and configure IpwBridge using dependency injection.
- **Token Management:** Automatic token retrieval and refresh.
- **Robust API Client:** Retrieve data types, explanations, lists, and items; send models; upload binary files.
- **Modern .NET Practices:** Uses cancellation tokens, Microsoft.Extensions.Logging for tracing, and XML documentation with examples.

## Getting Started

### Installation

Add IpwBridge to your project either via NuGet (if available) or by cloning the repository and referencing the project.

### Setup with Dependency Injection

Register the library with your DI container by calling the extension method `AddIpwBridge()` in your `Program.cs` (or `Startup.cs`):

```csharp
builder.Services.AddIpwBridge(options =>
{
    options.IpwUrl = "https://your-domain.dk/metazo/api/v1/";
    options.IpwUser = "USERNAME";
    options.IpwPassword = "PASSWORD";
    options.ChecksumSecret = "CHECKSUM-SECRET";
});
```

This will configure the IMetazoApiClient service and its dependencies.

## Usage

Once configured, inject the IMetazoApiClient into your classes. For example, in a background service:

```csharp
using IpwBridge.Interfaces;

public class Worker(IMetazoApiClient metazoApiClient) : BackgroundService
{
    private readonly IMetazoApiClient _metazoApiClient = metazoApiClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var datatypes = await _metazoApiClient.GetDatatypesAsync(stoppingToken);
        Console.WriteLine(datatypes);
    }
}
```

## API Methods and Examples

### 1. GetDatatypesAsync

Retrieve available data types from the API.

```csharp
// Example: Retrieve available data types.
var datatypes = await _metazoApiClient.GetDatatypesAsync();
Console.WriteLine(datatypes);
```

### 2. GetExplanationAsync

Retrieve an explanation for a specific data type.

```csharp
// Example: Get an explanation for a given data type.
string datatype = "form113622";
var explanation = await _metazoApiClient.GetExplanationAsync(datatype);
Console.WriteLine(explanation);
```

### 3. Creating and Using a ListRequest

IpwBridge supports flexible filtering using a ```ListRequest``` model. The search properties support implicit conversions from enums or strings.

**Using Enums**

```csharp
using IpwBridge.Contracts;
using IpwBridge.Contracts.Enums;

ListRequest request = new ListRequest
{
    DataType = "form121889",
    FieldsToGet = "f276474,f1628152,f2605112",
    Limit = 20,                                     // Optional, default value is 20.
    Offset = 0,                                     // Optional, default value is 0.
    SearchAndOr = SearchConnector.And,              // Implicitly converts to SearchConnection.
    SearchField = DefaultSearchFields.ObjectId,     // Implicitly converts to SearchField.
    SearchOperation = SearchOperator.GreaterEqual,  // Implicitly converts to SearchOperation.
    SearchAfter = "2604436",
    FromDate = DateTime.UtcNow.AddDays(-30)         // This is optional, as this value is the default value.
};
```

**Using Strings**
```csharp
ListRequest request = new ListRequest
{
    DataType = "form121889",
    FieldsToGet = "f276474,f1628152,f2605112",
    Limit = 20,
    Offset = 0,
    SearchAndOr = "Or",             // Implicitly converts to SearchConnection.
    SearchField = "created",        // Implicitly converts to SearchField.
    SearchOperation = "LessEqual",  // Implicitly converts to SearchOperation.
    SearchAfter = "2020-01-01"
};

```

### 4. GetListAsync

There are two ways to call ```GetListAsync```:

#### 4a. Non-Generic Version (Returns ```JsonElement```)

```csharp
// Example: Retrieve a list of items as a JsonElement.
JsonElement listResponseJsonElement = await _metazoApiClient.GetListAsync(request, stoppingToken);
Console.WriteLine(listResponseJsonElement);
```

#### 4b. Generic Version (Returns a Strongly Typed Response)

```csharp
// Example: Retrieve a list of items as a strongly typed object.
MetazoListItem listResponseObject = await _metazoApiClient.GetListAsync<MetazoListItem>(request, stoppingToken);
Console.WriteLine(listResponseObject);
```

#### Pros and Cons for GetListAsync

| Approach                          | Pros                                                                                          | Cons                                                                  |
|-----------------------------------|-----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| **Non-Generic (`JsonElement`)**   | - Flexible handling of raw JSON responses.<br>- Useful when the JSON structure is dynamic.     | - Requires manual deserialization.<br>- Lacks compile-time type safety. |
| **Generic (Strongly Typed)**      | - Provides compile-time type checking and IntelliSense support.<br>- Simplifies access to properties. | - Depends on correctly defined models with `[JsonPropertyName]` attributes.<br>- Less flexible if the API schema changes. |


### 5. GetItemAsync

Similarly, there are two ways to call ```GetItemAsync```:

#### 5a. Non-Generic Version (Returns ```JsonElement```)

```csharp
// Example: Retrieve an item by object ID as a JsonElement.
JsonElement itemResponseJsonElement = await _metazoApiClient.GetItemAsync(2842317, stoppingToken);
Console.WriteLine(itemResponseJsonElement);
```

#### 5b. Generic Version (Returns a Strongly Typed Response)

```csharp
// Example: Retrieve an item by object ID as a strongly typed object.
MetazoItemObject itemResponseObject = await _metazoApiClient.GetItemAsync<MetazoItemObject>(2842317, stoppingToken);
Console.WriteLine(itemResponseObject);
```

#### Pros and Cons for GetItemAsync

| Approach                          | Pros                                                                                          | Cons                                                                  |
|-----------------------------------|-----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| **Non-Generic (`JsonElement`)**   | - Flexible for dynamic JSON structures.<br>- Allows custom parsing when needed.                | - Requires extra manual deserialization.<br>- No compile-time validation. |
| **Generic (Strongly Typed)**      | - Enables compile-time type safety and IntelliSense support.<br>- Provides direct access to mapped properties. | - Requires accurate model definitions using `[JsonPropertyName]`.<br>- Less flexible if the response structure is variable. |

### 6. SendModelAsync

Send a CRUD model request to create, update, or delete an item.

```csharp
using System.Text.Json;
using IpwBridge.Contracts;
using IpwBridge.Contracts.Enums;

Dictionary<string, string> jsonData = new Dictionary<string, string>
{
    { "state", "2" },
};

string fullJson = JsonSerializer.Serialize(jsonData);

IpwCrudRequest crudRequest = new IpwCrudRequest
{
    Datatype = "form121889",
    Model = ModelOptions.Create, // For Update or Delete, set ObjectId as well.
    JsonData = fullJson
};

var crudResponse = await _metazoApiClient.SendModelAsync(crudRequest);
Console.WriteLine(crudResponse);
```

### 7. UploadBinfileAsync

Upload binary files to the API.

```csharp
using System.IO;
using IpwBridge.Contracts;

using var fileStream = File.OpenRead("testimg.png");
BinfileUploadRequest uploadRequest = new BinfileUploadRequest
{
    ParentId = 2605115,
    Files = new Dictionary<string, Stream>
    {
        { "file_1", fileStream }
    }
};

var uploadResponse = await _metazoApiClient.UploadBinfileAsync(uploadRequest);
Console.WriteLine(uploadResponse);
```

### Models and JSON Property Annotations

IpwBridge uses JSON property annotations to map JSON fields to your model properties. You can create your own models by implementing the provided interfaces.

**Default Models**

```csharp
public class MetazoListItem : IMetazoListItem
{
    [JsonPropertyName("objectid")]
    public required string ObjectId { get; set; }

    [JsonPropertyName("language")]
    public required string Language { get; set; }
}
```

```csharp
public class MetazoItemObject : IMetazoItemObject
{
    [JsonPropertyName("objectid")]
    public required string ObjectId { get; set; }

    [JsonPropertyName("site")]
    public required string Site { get; set; }
}
```

**Custom Models**

You can also define your own models by implementing ```IMetazoListItem``` or ```IMetazoItemObject``` and using ```[JsonPropertyName]``` attributes to map the JSON fields correctly.

## Logging and Cancellation

IpwBridge uses Microsoft.Extensions.Logging for logging important events (such as token refreshes and errors). All asynchronous methods accept a CancellationToken, which allows you to cancel long-running operations gracefully.

Ensure your application is configured with a logging provider (e.g., Console, Debug, or a logging framework) to capture and view these logs.