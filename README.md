# IpwBridge

[![NuGet](https://img.shields.io/nuget/v/IpwBridge.svg)](https://www.nuget.org/packages/IpwBridge)
[![NuGet downloads](https://img.shields.io/nuget/dt/IpwBridge.svg)](https://www.nuget.org/packages/IpwBridge)
[![CI](https://github.com/DrNoLife/IpwBridge/actions/workflows/ci.yml/badge.svg)](https://github.com/DrNoLife/IpwBridge/actions/workflows/ci.yml)
[![License: GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](https://github.com/DrNoLife/IpwBridge/blob/main/LICENSE)

A typed .NET client for the [IPW Metazo API](https://metazoapi.support.ipw.dk/). IpwBridge handles authentication,
token caching and refresh, request checksums, retries and timeouts, so your code only deals with Metazo data.

```csharp
await foreach (var item in client.GetAllAsync<MetazoListItem>(new ListRequest { DataType = "form121889", Limit = 500 }))
{
    Console.WriteLine(item.ObjectId);
}
```

## Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [Usage](#usage)
  - [Datatypes and fields](#datatypes-and-fields)
  - [Listing items](#listing-items)
  - [Reading an item](#reading-an-item)
  - [Creating, updating and deleting](#creating-updating-and-deleting)
  - [Uploading files](#uploading-files)
  - [Downloading files](#downloading-files)
  - [Tokens and health checks](#tokens-and-health-checks)
  - [Quickfilters](#quickfilters)
  - [Your own item types](#your-own-item-types)
- [Error handling](#error-handling)
- [Retries, timeouts and logging](#retries-timeouts-and-logging)
- [Customizing the HTTP pipeline](#customizing-the-http-pipeline)
- [Migrating from 1.x](#migrating-from-1x)
- [Building and testing](#building-and-testing)
- [Releasing](#releasing)
- [License](#license)

## Features

- **Every documented endpoint**: datatypes, explain, list, read, model, binfile upload and download, ping,
  validate, revoke and quickfilters.
- **Automatic authentication**: tokens are cached until shortly before their expiry and renewed transparently when
  the server rejects them.
- **Checksums handled for you**: request and file checksums follow the documented Metazo algorithm.
- **Typed or raw**: get typed responses, your own classes, or the raw `JsonElement`.
- **Large files**: uploads and downloads stream instead of buffering whole files in memory.
- **Production defaults**: retries with backoff for idempotent requests, per-request timeouts, startup validation of
  options, and logs that never contain credentials, tokens or payloads.
- **Fully documented API** with IntelliSense, SourceLink and symbol packages.

## Requirements

- .NET 8 or .NET 10 (the package targets `net8.0` and `net10.0`).
- A Metazo user, its password and the checksum secret from the Metazo configuration menu.

## Installation

```shell
dotnet add package IpwBridge
```

## Quick start

1. Add the settings to `appsettings.json` and keep the password and secret in
   [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or a vault:

   ```json
   {
     "IpwBridge": {
       "IpwUrl": "https://your.metazo.domain/metazo/api/v1/",
       "IpwUser": "apiuser",
       "IpwPassword": "<secret>",
       "ChecksumSecret": "<secret>"
     }
   }
   ```

2. Register IpwBridge in `Program.cs`:

   ```csharp
   builder.Services.AddIpwBridge(builder.Configuration.GetSection("IpwBridge"));
   ```

3. Inject `IMetazoApiClient` wherever you need it:

   ```csharp
   public sealed class DatatypeLister(IMetazoApiClient client)
   {
       public async Task PrintAsync(CancellationToken cancellationToken)
       {
           var response = await client.GetDatatypesAsync(cancellationToken);
           foreach (var datatype in response.Datatypes)
           {
               Console.WriteLine($"{datatype.Id}: {datatype.Name}");
           }
       }
   }
   ```

The options can also be set in code:

```csharp
builder.Services.AddIpwBridge(options =>
{
    options.IpwUrl = "https://your.metazo.domain/metazo/api/v1/";
    options.IpwUser = "apiuser";
    options.IpwPassword = builder.Configuration["Metazo:Password"]!;
    options.ChecksumSecret = builder.Configuration["Metazo:ChecksumSecret"]!;
});
```

The options are validated when the application starts, so a missing URL or secret fails immediately with a clear
message.

## Configuration

| Option | Default | Description |
| --- | --- | --- |
| `IpwUrl` | *(required)* | Absolute base URL of the API, for example `https://your.metazo.domain/metazo/api/v1/`. |
| `IpwUser` | *(required)* | Metazo user name. |
| `IpwPassword` | *(required)* | Password of `IpwUser`. |
| `ChecksumSecret` | *(required)* | Checksum secret from the Metazo configuration menu. |
| `Site` | `1` | Metazo site id. |
| `Language` | *(none)* | Language code sent when authenticating, for example `EN` or `DA`. |
| `DatasourceToken` | *(none)* | Datasource token for the quickfilter endpoints. |
| `RequestTimeout` | 100 s | Timeout for one JSON request, including reading the body. |
| `BinfileTimeout` | 10 min | Timeout for one binfile upload or download. |
| `MaxRetryAttempts` | `2` | Retries of GET requests after transient failures. `0` disables retries. |
| `RetryBaseDelay` | 500 ms | Delay before the first retry; it doubles for each further retry. |
| `UseLegacyGetAuthentication` | `false` | Authenticate with GET and query-string credentials, as older Metazo versions required. |
| `JsonTypeInfoResolver` | *(none)* | Source-generated JSON metadata for your own item types (trimming and Native AOT). |

## Usage

All examples assume an injected `IMetazoApiClient client` and these namespaces:

```csharp
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using IpwBridge.Contracts;
using IpwBridge.Contracts.Enums;
using IpwBridge.Exceptions;
using IpwBridge.Extensions;
using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;
```

### Datatypes and fields

```csharp
var datatypes = await client.GetDatatypesAsync();

var explanation = await client.GetExplanationAsync("form113622");
foreach (var field in explanation.Fields)
{
    Console.WriteLine($"{field.Id} ({field.InputType}): {field.Label}");
}
```

### Listing items

A `ListRequest` names the datatype, the fields to return, the page and any number of search conditions. Fields,
operators and connectors accept the enums or plain strings (for example a field id like `f276474`).

```csharp
var request = new ListRequest
{
    DataType = "form121889",
    FieldsToGet = "f276474,f1628152",
    Limit = 50,
}
.Where(DefaultSearchFields.ObjectId, SearchOperator.GreaterEqual, "2604436")
.Where("f276474", SearchOperator.Like, "Api");

MetazoListResponse<MetazoListItem> page = await client.GetListAsync<MetazoListItem>(request);
foreach (var item in page.Items)
{
    Console.WriteLine($"{item.ObjectId}: {item.AdditionalFields["f276474"]}");
}
```

Other list options:

| Property | Description |
| --- | --- |
| `Limit` / `Offset` | Page size (default 20; `0` returns everything) and number of items to skip. |
| `SearchAndOr` | Combines the conditions with `SearchConnector.And` (default) or `SearchConnector.Or`. |
| `FromDate` | Adds a `created >= date` condition. |
| `IncludeInactive` | Includes inactive objects. |

To read every matching item, let IpwBridge request the pages:

```csharp
await foreach (var item in client.GetAllAsync<MetazoListItem>(new ListRequest { DataType = "form121889", Limit = 500 }))
{
    Console.WriteLine(item.ObjectId);
}
```

`GetListAsync(request)` without a type argument returns the raw `JsonElement`.

### Reading an item

```csharp
MetazoItemResponse<MetazoItemObject> response = await client.GetItemAsync<MetazoItemObject>(2842317);
Console.WriteLine(response.Result.Fields.AdditionalFields["created"]);
```

`GetItemAsync(objectId)` without a type argument returns the raw `JsonElement`.

### Creating, updating and deleting

`CrudRequest` has builders for the built-in models. The field values are a JSON object; every top-level field is
part of the request checksum, so send values as strings (as Metazo itself does) and do not reuse the names
`datatype`, `model`, `objectid`, `token` or `checksum`.

```csharp
string fields = new JsonObject { ["state"] = "2" }.ToJsonString();

var created = await client.SendModelAsync(CrudRequest.Create("form121889", fields));
int newId = created.ObjectId!.Value;

await client.SendModelAsync(CrudRequest.Update("form121889", newId, fields));
await client.SendModelAsync(CrudRequest.CreateCopy("form121889", newId));
await client.SendModelAsync(CrudRequest.Delete("form121889", newId));
```

Datatype-specific models listed by `GetExplanationAsync` can be run by name:

```csharp
await client.SendModelAsync(new CrudRequest { Datatype = "form121889", Model = "approve", ObjectId = 2605115 });
```

### Uploading files

```csharp
await using var file = File.OpenRead("report.pdf");

var upload = await client.UploadBinfileAsync(
    new BinfileUploadRequest { ParentId = 2605115 }
        .AddFile("file_1", file, contentType: "application/pdf"));

string binfileId = upload.UploadedFiles["file_1"];
```

The streams are read but never disposed. Seekable streams are uploaded from the beginning, whatever their current
position. Non-seekable streams (such as a request body or a network stream) are spooled to a temporary file, so they
can be hashed and resent after a token refresh.

### Downloading files

```csharp
await using var destination = File.Create("report.pdf");
await client.DownloadBinfileAsync(4711, destination);

byte[] small = await client.DownloadBinfileAsync(4712);
```

The stream overload writes the file as it arrives; prefer it for anything but small files. Small JSON-typed answers
(up to 64 KB) are first checked for a Metazo error, so an error is never saved as file content.

### Tokens and health checks

```csharp
bool reachable = await client.PingAsync();         // no authentication needed
bool valid = await client.ValidateTokenAsync();
await client.RevokeTokenAsync();                   // for example at the end of a batch job
```

### Quickfilters

Quickfilters (Metazo 5.27 or later) authenticate with the datasource token, so set `DatasourceToken` first.

```csharp
var items = await client.GetFilterAsync(5784, includeInactive: true);
var count = await client.GetFilterCountAsync(5784);
```

### Your own item types

Any class works as item type; map the Metazo field ids with `[JsonPropertyName]`. Numbers sent as strings
(`"5045"`) are converted automatically.

```csharp
public sealed class Invoice
{
    [JsonPropertyName("objectid")]
    public int ObjectId { get; set; }

    [JsonPropertyName("f276474")]
    public string? Customer { get; set; }
}
```

IpwBridge is trimming- and Native AOT-compatible. In such applications reflection-based JSON is disabled, so generate
the metadata for your item types and pass the context in the options:

```csharp
[JsonSerializable(typeof(MetazoListResponse<Invoice>))]
[JsonSerializable(typeof(MetazoItemResponse<Invoice>))]
public sealed partial class InvoiceJsonContext : JsonSerializerContext;
```

```csharp
builder.Services.AddIpwBridge(builder.Configuration.GetSection("IpwBridge"));
builder.Services.Configure<MetazoApiOptions>(options => options.JsonTypeInfoResolver = InvoiceJsonContext.Default);
```

## Error handling

Every exception thrown by IpwBridge derives from `IpwBridgeException`:

| Exception | When |
| --- | --- |
| `IpwBridgeCommunicationException` | An HTTP error, a response with `"success": "false"`, a network failure after retries, or a timeout. `StatusCode`, `ServerMessage` and a truncated `ResponseBody` describe the failure. |
| `IpwBridgeTokenInvalidException` | The server rejected the token, and also the fresh one obtained after re-authenticating. For quickfilters: the datasource token was rejected. |
| `IpwBridgeAuthenticationException` | No token could be obtained, for example because of a wrong password or checksum secret. |
| `IpwBridgeDeserializationException` | A response does not match the requested type. |
| `IpwBridgeException` (the base type itself) | A local I/O failure: the destination stream of a download rejected a write, or a non-seekable upload could not be copied to a temporary file. |

`IpwBridgeTokenInvalidException` derives from `IpwBridgeCommunicationException`, so catch it first if you handle
both. Invalid arguments throw `ArgumentException` before any request is sent, the quickfilter methods throw
`InvalidOperationException` when `DatasourceToken` is not configured, and cancellation throws
`OperationCanceledException`.

```csharp
try
{
    await client.GetItemAsync(2842317);
}
catch (IpwBridgeCommunicationException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine($"Not found: {ex.ServerMessage}");
}
```

## Retries, timeouts and logging

- **Retries**: GET requests (and authentication) are retried on network errors, timeouts and HTTP 408, 429, 502, 503
  and 504, and on HTTP 500 unless the body is a Metazo error message (those are deterministic). Retries use
  exponential backoff (capped at 30 s) and honor `Retry-After`. Model runs and uploads are never retried this way,
  so they cannot run twice.
- **Token refresh**: when the server rejects a token, IpwBridge re-authenticates once and repeats the call, uploads
  included.
- **Timeouts**: `RequestTimeout` and `BinfileTimeout` cover the whole exchange, including the response body.
- **Logging**: IpwBridge logs through `Microsoft.Extensions.Logging` under the `IpwBridge.*` categories. Requests are
  logged at `Debug`, retries at `Warning`, and each failed call exactly once at `Error`. Log messages contain the
  endpoint name only; never URLs, tokens, credentials or payloads.

## Customizing the HTTP pipeline

`AddIpwBridge` returns the `IHttpClientBuilder` of the client IpwBridge uses, so you can add handlers, a proxy or
your own resilience policies:

```csharp
builder.Services.AddIpwBridge(builder.Configuration.GetSection("IpwBridge"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseProxy = false });
```

If you add your own retry policy, set `MaxRetryAttempts` to `0` to avoid retrying twice.

## Migrating from 1.x

Version 2.0 contains breaking changes. See the [changelog](https://github.com/DrNoLife/IpwBridge/blob/main/CHANGELOG.md) for the full list.

| 1.x | 2.0 |
| --- | --- |
| `using IpwBridge.Interfaces;` | `using IpwBridge.Interfaces.Services;` |
| `IpwCrudRequest` | `CrudRequest`, with `Create`, `Update`, `Delete` and `CreateCopy` builders |
| `SendModelAsync` returns `JsonElement` | returns `MetazoModelResponse` (`ObjectId`, `Model`, `Raw`) |
| `BinfileUploadRequest.Files = new Dictionary<string, Stream> { ... }` | `new BinfileUploadRequest { ParentId = id }.AddFile("file_1", stream)`; the stream is no longer disposed |
| `UploadBinfileAsync` returns `JsonElement` | returns `MetazoUploadResponse` (`UploadedFiles`, `Raw`) |
| `DownloadBinFileAsync` | `DownloadBinfileAsync`, plus a stream overload |
| `ListRequest.SearchField`, `SearchOperation`, `SearchAfter` | `ListRequest.Where(field, operator, value)` |
| `ListRequest.FromDate` defaulted to 30 days ago (but was never sent) | `FromDate` is optional and is sent when set |
| `ListRequest.DataType` was optional | it is `required` |
| `List<T>` collections on responses | `IReadOnlyList<T>` |
| `MetazoListItem` / `MetazoItemObject` had settable, required `Site`/`Language` | `init`-only and nullable; other fields in `AdditionalFields` |
| `JsonContext` was public | internal; use `MetazoApiOptions.JsonTypeInfoResolver` for your own types |
| `MetazoItemResult<T>.Object` | `MetazoItemResult<T>.Fields` |
| `IMetazoListItem` / `IMetazoItemObject` constraints | removed; any class works |
| `TokenInvalidException` | `IpwBridgeTokenInvalidException` (all exceptions derive from `IpwBridgeException`) |
| `SuccessAsString` / `Success` on responses | removed: a returned response is always successful |
| Responses with `"success": "false"` were returned | they throw `IpwBridgeCommunicationException` |
| Checksum keys were lowercased | keys are used exactly as sent, as in the documented PHP reference |
| `AddIpwBridge` returned `IServiceCollection` | returns `IHttpClientBuilder` |
| Services such as `MetazoApiClient` and `TokenProvider` were public | they are internal; use `IMetazoApiClient` |

## Building and testing

```shell
dotnet build IpwBridge/IpwBridge.sln
dotnet test IpwBridge/IpwBridge.sln
```

The test suite runs against an in-memory fake of the Metazo API and needs no server.

## Releasing

[GitHub Actions](https://github.com/DrNoLife/IpwBridge/blob/main/.github/workflows/ci.yml) builds and tests every
push to `main` and every pull request. Pushing a tag `v<version>` that matches `<Version>` in the project file also
packs the library and publishes it to nuget.org with
[trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing), so no API key is stored in
GitHub.

One-time setup on nuget.org (**Trusted Publishing** under your profile): add a policy with repository owner
`DrNoLife`, repository `IpwBridge`, workflow file `ci.yml` and environment `nuget`.

```shell
git tag v2.0.0
git push origin v2.0.0
```

## License

IpwBridge is licensed under the [GNU General Public License v3.0](https://github.com/DrNoLife/IpwBridge/blob/main/LICENSE).
