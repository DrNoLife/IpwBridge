using IpwBridge.Contracts;
using IpwBridge.Interfaces.Models;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;
using System.Text.Json;

namespace IpwBridge.Interfaces.Services;

/// <summary>
/// Defines methods for interacting with the IPW Metazo API.
/// </summary>
/// <remarks>
/// Implementations of this interface provide methods for retrieving data types, explanations, lists, and items,
/// as well as sending models and uploading binary files to the API. Token authentication and checksum security
/// are handled internally.
/// </remarks>
public interface IMetazoApiClient
{
    /// <summary>
    /// Retrieves the available data types from the API.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="MetazoDatatypesResponse"/>
    /// that describes the available data types.
    /// </returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Example: Retrieve available data types.
    /// IMetazoApiClient apiClient = ...; // Obtain via dependency injection.
    /// var datatypes = await apiClient.GetDatatypesAsync();
    /// Console.WriteLine(datatypes);
    /// ]]></code>
    /// </example>
    Task<MetazoDatatypesResponse?> GetDatatypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an explanation for the specified data type.
    /// </summary>
    /// <param name="datatype">The data type for which an explanation is requested.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="MetazoExplanationResponse"/>
    /// with details about the data type.
    /// </returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Example: Get an explanation for a given data type.
    /// IMetazoApiClient apiClient = ...;
    /// string datatype = "form113622";
    /// var explanation = await apiClient.GetExplanationAsync(datatype);
    /// Console.WriteLine(explanation);
    /// ]]></code>
    /// </example>
    Task<MetazoExplanationResponse?> GetExplanationAsync(string datatype, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of items from the API based on the specified request.
    /// </summary>
    /// <param name="dataRequest">The request parameters for retrieving the list.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the list response.
    /// </returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Example: Retrieve a list of items.
    /// IMetazoApiClient apiClient = ...;
    /// ListRequest request = new ListRequest
    /// {
    ///     DataType = "form121889",
    ///     FieldsToGet = "f276474,f1628152,f2605112",
    ///     Limit = 20,
    ///     Offset = 0,
    ///     SearchAndOr = SearchConnector.And, // Implicitly converts to SearchConnection.
    ///     SearchField = DefaultSearchFields.ObjectId, // Implicitly converts to SearchField.
    ///     SearchOperation = SearchOperator.GreaterEqual, // Implicitly converts to SearchOperation.
    ///     SearchAfter = "2604436",
    ///     FromDate = DateTime.UtcNow.AddDays(-30)
    /// };
    /// var listResponse = await apiClient.GetListAsync(request);
    /// Console.WriteLine(listResponse);
    /// ]]></code>
    /// </example>
    Task<JsonElement> GetListAsync(ListRequest dataRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of items from the API and deserializes them into a specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type into which the list items will be deserialized. This type must implement <see cref="IMetazoListItem"/>.
    /// </typeparam>
    /// <param name="dataRequest">The request parameters for retrieving the list.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="MetazoListResponse{T}"/>
    /// that holds the deserialized list data.
    /// </returns>
    Task<MetazoListResponse<T>?> GetListAsync<T>(ListRequest dataRequest, CancellationToken cancellationToken = default)
        where T : IMetazoListItem;

    /// <summary>
    /// Retrieves a specific item from the API based on its object ID.
    /// </summary>
    /// <param name="objectId">The unique identifier of the object to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the item.
    /// </returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Example: Retrieve an item by object ID.
    /// IMetazoApiClient apiClient = ...;
    /// int objectId = 2605115;
    /// var itemResponse = await apiClient.GetItemAsync(objectId);
    /// Console.WriteLine(itemResponse);
    /// ]]></code>
    /// </example>
    Task<JsonElement> GetItemAsync(int objectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific item from the API and deserializes it into a specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type into which the item will be deserialized. This type must implement <see cref="IMetazoItemObject"/>.
    /// </typeparam>
    /// <param name="objectId">The unique identifier of the object to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="MetazoItemResponse{T}"/>
    /// with the deserialized item data.
    /// </returns>
    Task<MetazoItemResponse<T>?> GetItemAsync<T>(int objectId, CancellationToken cancellationToken = default)
        where T : IMetazoItemObject;

    /// <summary>
    /// Sends a CRUD model request to the API.
    /// </summary>
    /// <param name="crudModel">
    /// The CRUD request model containing the operation, data type, and JSON payload.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the API response.
    /// </returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Example: Send a create model request.
    /// IMetazoApiClient apiClient = ...;
    /// Dictionary<string, string> jsonData = new()
    /// {
    ///     { "state", "2" },
    /// };
    /// string fullJson = JsonSerializer.Serialize(jsonData);
    /// IpwCrudRequest crudRequest = new()
    /// {
    ///     Datatype = "form121889",
    ///     Model = ModelOptions.Create,
    ///     JsonData = fullJson
    /// };
    /// var crudResponse = await apiClient.SendModelAsync(crudRequest);
    /// Console.WriteLine(crudResponse);
    /// ]]></code>
    /// </example>
    Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads binary files to the API.
    /// </summary>
    /// <param name="binfileUploadModel">
    /// The binary file upload request model containing the parent ID and a collection of file streams.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the API response.
    /// </returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Example: Upload a binary file.
    /// IMetazoApiClient apiClient = ...;
    /// using var fileStream = File.OpenRead("testimg.png");
    /// BinfileUploadRequest uploadRequest = new()
    /// {
    ///     ParentId = 2605115,
    ///     Files = new Dictionary<string, Stream>
    ///     {
    ///         { "file_1", fileStream }
    ///     }
    /// };
    /// var uploadResponse = await apiClient.UploadBinfileAsync(uploadRequest);
    /// Console.WriteLine(uploadResponse);
    /// ]]></code>
    /// </example>
    Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest binfileUploadModel, CancellationToken cancellationToken = default);
}