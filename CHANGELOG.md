# Changelog

All notable changes to IpwBridge are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [2.0.0] - 2026-10-05

A reliability and security release based on a full code review. It contains breaking changes; see
[Migrating from 1.x](https://github.com/DrNoLife/IpwBridge#migrating-from-1x).

### Security

- The password and token no longer appear in logs. With the previously pinned `Microsoft.Extensions.Http` 8.0.0, the
  HttpClient factory logged full request URLs, including `pass=` and `token=`, at `Information` level.
- Authentication is a POST with a multipart form body, as in the API documentation, instead of a GET with
  credentials in the query string. Set `UseLegacyGetAuthentication` if your server still requires GET.
- Request and response bodies are no longer logged. Exception messages contain at most 200 characters of the
  server's error text; the (4 KB truncated) body is available in `IpwBridgeCommunicationException.ResponseBody`.

### Fixed

- Uploading from a non-seekable stream sent an empty file. Non-seekable streams are now spooled to a temporary file.
- An upload that hit an expired token always failed with `ObjectDisposedException`. Uploads are now resent with a
  fresh token, and the caller's streams are never disposed.
- Typed list and read responses failed when the API sent numbers as strings (`"count": "20"`,
  `"objectid": "5045"`).
- A default `ListRequest` sent an empty `search=` parameter, and `FromDate` was never sent.
- A login answered with `"success": "false"` was treated as successful with an empty token.
- Responses with `"success": "false"` were returned as if they had succeeded; they now throw.
- The request checksum now follows the documented PHP reference (`ksort` + `key . value`): keys are used exactly as
  sent instead of being lowercased, they are sorted ordinally instead of by the current culture, and JSON booleans
  become `1`/empty instead of `True`/`False`. Lowercase keys, which Metazo uses, give the same checksum as before.
- The file checksum could hash fewer than 256 bytes when a stream returned short reads.
- Protocol values (field names, operators, numbers, dates) were formatted with the current culture, which broke
  requests on, for example, Turkish or Thai systems.
- A `CrudRequest` without a model failed with `NullReferenceException`; invalid requests now throw
  `ArgumentException` before anything is sent.
- Authentication ignored a base URL without a trailing slash and bypassed the configured HttpClient.
- Concurrent requests rejected for the same token caused one re-authentication each instead of one in total.

### Added

- `PingAsync`, `ValidateTokenAsync`, `RevokeTokenAsync`, `GetFilterAsync` and `GetFilterCountAsync`.
- Multiple search conditions (`ListRequest.Where`) and `IncludeInactive`.
- `GetAllAsync<T>`, which pages through all matching items.
- `DownloadBinfileAsync(objectId, Stream)`, which streams a file instead of buffering it.
- `CrudRequest.Update` and `CrudRequest.CreateCopy` builders, and typed `MetazoModelResponse` and
  `MetazoUploadResponse` results.
- File names and content types for uploads.
- Options: `Site`, `Language`, `DatasourceToken`, `RequestTimeout`, `BinfileTimeout`, `MaxRetryAttempts`,
  `RetryBaseDelay`, `UseLegacyGetAuthentication` and `JsonTypeInfoResolver`, validated at startup.
- `AddIpwBridge(IConfiguration)`.
- Automatic retries with exponential backoff for idempotent requests, and timeouts that cover the response body.
- `GetAllAsync` follows the page size the server applies, even if it caps the requested limit.
- `IpwBridgeException` as base type for all exceptions, plus `IpwBridgeAuthenticationException`.
- `AdditionalFields` on `MetazoListItem` and `MetazoItemObject` with every other returned field.
- XML documentation (IntelliSense), SourceLink, a symbol package, license metadata, a test suite and CI.
- Trimming and Native AOT compatibility (`IsAotCompatible`); configuration binding uses the source generator.

### Changed

- Targets `net8.0` and `net10.0` (previously only `net9.0`, which reaches end of support on 10 November 2026). Both
  targets depend on the `Microsoft.Extensions.*` 10.x packages, which are supported until November 2028 and run on
  .NET 8.
- The token lifetime is read from the JWT `exp` claim instead of being fixed at 25 minutes.
- `IpwCrudRequest` is renamed to `CrudRequest`, `TokenInvalidException` to `IpwBridgeTokenInvalidException`,
  `DownloadBinFileAsync` to `DownloadBinfileAsync`, and `MetazoItemResult<T>.Object` to `Fields`.
- `SendModelAsync` and `UploadBinfileAsync` return typed responses; the `Get*Async` methods no longer return
  nullable results.
- `BinfileUploadRequest.Files` is a list of `BinfileUploadFile`; use `AddFile`.
- `ListRequest.DataType` is `required`; response collections are `IReadOnlyList<T>`; `MetazoListItem` and
  `MetazoItemObject` have `init`-only properties, nullable `Site`/`Language` and `AdditionalFields`; `JsonContext`
  is internal.
- Responses no longer expose `SuccessAsString`/`Success`: a returned response is always successful, and failures
  throw. Detection reads the raw response, so it also works with your own source-generated JSON contexts.
- Collections in responses (`Items`, `Datatypes`, `Fields`, `Models`) are empty instead of failing when the API
  omits them. Other required fields (such as `datatype`) still fail with `IpwBridgeDeserializationException`.
- Binfile downloads check JSON answers for `"success": "false"`, so an error (or a rejected token) is never saved
  as file content.
- Responses that start with a UTF-8 byte order mark are accepted, as in 1.x.
- `AddIpwBridge` returns the `IHttpClientBuilder` and can safely be called more than once.
- Services are registered as singletons, and implementation types are internal.
- Logging uses source-generated messages and never includes URLs, tokens, credentials or payloads.

### Removed

- `IpwModel`, which could not be constructed.
- `IMetazoListItem` and `IMetazoItemObject`; any class can be used as item type.
- The public service interfaces other than `IMetazoApiClient`.
- `ListRequest.SearchField`, `SearchOperation` and `SearchAfter`; use `Where`.

## [1.4.5]

Last 1.x release.

[2.0.0]: https://github.com/DrNoLife/IpwBridge/releases/tag/v2.0.0
[1.4.5]: https://www.nuget.org/packages/IpwBridge/1.4.5
