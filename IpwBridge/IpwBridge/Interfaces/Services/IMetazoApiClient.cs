using System.Text.Json;
using IpwBridge.Contracts;
using IpwBridge.Exceptions;
using IpwBridge.Models.Responses;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;

namespace IpwBridge.Interfaces.Services;

/// <summary>
/// Client for the IPW Metazo API. Authentication, token caching and refresh, and request checksums are handled
/// internally.
/// </summary>
/// <remarks>
/// <para>Register it with <c>services.AddIpwBridge(...)</c> and inject <see cref="IMetazoApiClient"/>. The client is
/// thread-safe and registered as a singleton.</para>
/// <para>Every method can throw <see cref="IpwBridgeException"/> subtypes: <see cref="IpwBridgeCommunicationException"/>
/// when a request fails or the API reports <c>"success": "false"</c>, <see cref="IpwBridgeTokenInvalidException"/>
/// when even a fresh token is rejected, <see cref="IpwBridgeAuthenticationException"/> when no token can be
/// obtained, and <see cref="IpwBridgeDeserializationException"/> when a response does not match the expected type.
/// The base <see cref="IpwBridgeException"/> itself signals a local I/O failure: the destination stream of a
/// download rejected a write, or a non-seekable upload could not be copied to a temporary file.
/// Invalid arguments throw <see cref="ArgumentException"/>, and cancellation throws
/// <see cref="OperationCanceledException"/>.</para>
/// </remarks>
public interface IMetazoApiClient
{
    /// <summary>Checks that the API is reachable. Needs no authentication.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the API answered the ping; <see langword="false"/> if the request failed.</returns>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>Asks the API whether the current token is still valid. Authenticates first if no token is cached.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the token is valid; <see langword="false"/> if it is expired or unknown.</returns>
    Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the current token, for example when a batch job ends. Does nothing if no token is cached. The next
    /// call authenticates again.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the token is revoked.</returns>
    Task RevokeTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the datatypes (forms and subtables) available in Metazo.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The datatypes.</returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var response = await client.GetDatatypesAsync();
    /// foreach (var datatype in response.Datatypes)
    ///     Console.WriteLine($"{datatype.Id}: {datatype.Name}");
    /// ]]></code>
    /// </example>
    Task<MetazoDatatypesResponse> GetDatatypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Describes the fields and models of a datatype.</summary>
    /// <param name="datatype">The datatype, for example <c>form113622</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The fields and datatype-specific models.</returns>
    Task<MetazoExplanationResponse> GetExplanationAsync(string datatype, CancellationToken cancellationToken = default);

    /// <summary>Lists items of a datatype and returns the raw JSON response.</summary>
    /// <param name="request">The datatype, fields, paging and search conditions.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <c>/list</c> response as JSON.</returns>
    Task<JsonElement> GetListAsync(ListRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists items of a datatype and deserializes each item into <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The item type, for example <see cref="MetazoListItem"/> or your own class.</typeparam>
    /// <param name="request">The datatype, fields, paging and search conditions.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One page of items.</returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var page = await client.GetListAsync<MetazoListItem>(
    ///     new ListRequest { DataType = "form121889", FieldsToGet = "f276474" }
    ///         .Where(DefaultSearchFields.ObjectId, SearchOperator.Greater, "1000"));
    /// ]]></code>
    /// </example>
    Task<MetazoListResponse<T>> GetListAsync<T>(ListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams every matching item, requesting further pages of <see cref="ListRequest.Limit"/> items until the
    /// API returns a short page. A limit of 0 fetches everything in one request.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="request">The datatype, fields, page size, start offset and search conditions.</param>
    /// <param name="cancellationToken">A token to cancel the enumeration.</param>
    /// <returns>All matching items.</returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// await foreach (var item in client.GetAllAsync<MetazoListItem>(new ListRequest { DataType = "form121889", Limit = 500 }))
    ///     Console.WriteLine(item.ObjectId);
    /// ]]></code>
    /// </example>
    IAsyncEnumerable<T> GetAllAsync<T>(ListRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads one object and returns the raw JSON response.</summary>
    /// <param name="objectId">The object id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <c>/read</c> response as JSON.</returns>
    Task<JsonElement> GetItemAsync(int objectId, CancellationToken cancellationToken = default);

    /// <summary>Reads one object and deserializes its fields into <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The fields type, for example <see cref="MetazoItemObject"/> or your own class.</typeparam>
    /// <param name="objectId">The object id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The object.</returns>
    Task<MetazoItemResponse<T>> GetItemAsync<T>(int objectId, CancellationToken cancellationToken = default);

    /// <summary>Runs a model (create, update, delete, copy or a datatype-specific model) on an object.</summary>
    /// <param name="request">The request; use <see cref="CrudRequest.Create"/>, <see cref="CrudRequest.Update"/> and friends.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result, including the id of the object the model ran on.</returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// string json = new JsonObject { ["state"] = "2" }.ToJsonString();
    /// var result = await client.SendModelAsync(CrudRequest.Create("form121889", json));
    /// Console.WriteLine(result.ObjectId);
    /// ]]></code>
    /// </example>
    Task<MetazoModelResponse> SendModelAsync(CrudRequest request, CancellationToken cancellationToken = default);

    /// <summary>Attaches files to an object. The streams are read but not disposed.</summary>
    /// <param name="request">The parent object and the files.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new binfile ids, keyed by file key.</returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// await using var file = File.OpenRead("photo.png");
    /// var result = await client.UploadBinfileAsync(
    ///     new BinfileUploadRequest { ParentId = 2605115 }.AddFile("file_1", file, contentType: "image/png"));
    /// string binfileId = result.UploadedFiles["file_1"];
    /// ]]></code>
    /// </example>
    Task<MetazoUploadResponse> UploadBinfileAsync(BinfileUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Downloads a binfile into memory. Prefer the stream overload for large files.</summary>
    /// <param name="objectId">The binfile's object id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The file content.</returns>
    Task<byte[]> DownloadBinfileAsync(int objectId, CancellationToken cancellationToken = default);

    /// <summary>Downloads a binfile and writes it to <paramref name="destination"/> without buffering it in memory.</summary>
    /// <remarks>
    /// A response with a JSON content type of up to 64 KB is checked for a Metazo error before it is written, so a
    /// JSON file whose top-level object contains <c>"success": false</c> is treated as an error.
    /// </remarks>
    /// <param name="objectId">The binfile's object id.</param>
    /// <param name="destination">The stream to write to. It is not disposed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the file is written.</returns>
    Task DownloadBinfileAsync(int objectId, Stream destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the items of a quickfilter (Metazo 5.27 or later). Uses header authentication with
    /// <c>MetazoApiOptions.DatasourceToken</c>.
    /// </summary>
    /// <param name="filterId">The object id of the quickfilter.</param>
    /// <param name="advanced">Whether to request the advanced result format.</param>
    /// <param name="includeInactive">Whether to include inactive objects.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The filter result as JSON.</returns>
    /// <exception cref="InvalidOperationException"><c>DatasourceToken</c> is not configured.</exception>
    Task<JsonElement> GetFilterAsync(int filterId, bool advanced = false, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of items of a quickfilter (Metazo 5.27 or later).</summary>
    /// <param name="filterId">The object id of the quickfilter.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count response as JSON.</returns>
    /// <exception cref="InvalidOperationException"><c>DatasourceToken</c> is not configured.</exception>
    Task<JsonElement> GetFilterCountAsync(int filterId, CancellationToken cancellationToken = default);
}
