using IpwBridge.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using IpwBridge.Contracts;
using System.Security.Cryptography;
using IpwBridge.Models.Responses.List;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.Datatypes;
using IpwBridge.Models.Responses.Explanation;
using Microsoft.Extensions.Logging;
using IpwBridge.Interfaces.Services;
using IpwBridge.Interfaces.Models;
using IpwBridge.Exceptions;

namespace IpwBridge.Services;

/// <summary>
/// Provides a client for interacting with the IPW Metazo API.
/// </summary>
/// <remarks>
/// This client handles token authentication, checksum calculation, and makes HTTP calls to the API endpoints.
/// It supports operations such as retrieving data types, explanations, lists, items, sending models, and uploading binary files.
/// </remarks>
public class MetazoApiClient(
    IOptions<MetazoApiOptions> options,
    IHttpClientFactory httpClientFactory,
    ITokenProvider tokenProvider,
    IChecksumService checksumService,
    ILogger<MetazoApiClient> logger,
    IFileChecksumService fileChecksumService,
    IUrlBuilder urlBuilder,
    IApiRequestSender apiRequestSender) : IMetazoApiClient
{
    private readonly MetazoApiOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IChecksumService _checksumService = checksumService;
    private readonly ILogger<MetazoApiClient> _logger = logger;
    private readonly IFileChecksumService _fileChecksumService = fileChecksumService;
    private readonly IUrlBuilder _urlBuilder = urlBuilder;
    private readonly IApiRequestSender _apiRequestSender = apiRequestSender;

    /// <summary>
    /// Retrieves the available data types from the API.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="MetazoDatatypesResponse"/>
    /// describing the available data types.
    /// </returns>
    public async Task<MetazoDatatypesResponse?> GetDatatypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling GetDatatypesAsync.");
        var json = await ExecuteWithTokenRefreshAsync(async ct =>
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            Dictionary<string, string> parameters = new()
            {
                { "token", token }
            };

            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);
            parameters.Add("checksum", checksum);

            var url = _urlBuilder.BuildUrl("datatypes", parameters);

            return await _apiRequestSender.SendGetRequestAsync(url, ct);
        }, cancellationToken);

        return JsonSerializer.Deserialize<MetazoDatatypesResponse>(json);
    }

    /// <summary>
    /// Retrieves an explanation for the specified data type.
    /// </summary>
    /// <param name="datatype">The data type for which an explanation is requested.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="MetazoExplanationResponse"/>
    /// with details about the data type.
    /// </returns>
    public async Task<MetazoExplanationResponse?> GetExplanationAsync(string datatype, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling GetExplanationAsync for datatype: {Datatype}", datatype);
        var json = await ExecuteWithTokenRefreshAsync(async ct =>
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            Dictionary<string, string> parameters = new()
            {
                { "datatype", datatype },
                { "token", token }
            };

            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);
            parameters.Add("checksum", checksum);

            var url = _urlBuilder.BuildUrl("explain", parameters);

            return await _apiRequestSender.SendGetRequestAsync(url, ct);
        }, cancellationToken);

        return JsonSerializer.Deserialize<MetazoExplanationResponse>(json);
    }

    /// <summary>
    /// Retrieves a list of items from the API based on the specified request parameters.
    /// </summary>
    /// <param name="dataRequest">The parameters for retrieving the list of items.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the list response.
    /// </returns>
    public async Task<JsonElement> GetListAsync(ListRequest dataRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling GetListAsync with DataType: {DataType}", dataRequest.DataType);
        return await ExecuteWithTokenRefreshAsync(async ct =>
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            Dictionary<string, string> parameters = new()
            {
                { "datatype", dataRequest.DataType },
                { "fields", dataRequest.FieldsToGet },
                { "limit", dataRequest.Limit.ToString() },
                { "offset", dataRequest.Offset.ToString() },
                { "searchandor", dataRequest.SearchAndOr },
                { "search", dataRequest.SearchAfter ?? dataRequest.FromDate.ToString("yyyy-MM-dd") },
                { "searchcomp", dataRequest.SearchOperation },
                { "searchfield", dataRequest.SearchField },
                { "token", token }
            };

            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);
            parameters.Add("checksum", checksum);

            var url = _urlBuilder.BuildUrl("list", parameters);

            return await _apiRequestSender.SendGetRequestAsync(url, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of items from the API and deserializes them into a specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type into which the list items will be deserialized. This type must implement <see cref="IMetazoListItem"/>.
    /// </typeparam>
    /// <param name="dataRequest">The parameters for retrieving the list of items.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="MetazoListResponse{T}"/>
    /// with the deserialized list data.
    /// </returns>
    public async Task<MetazoListResponse<T>?> GetListAsync<T>(ListRequest dataRequest, CancellationToken cancellationToken = default)
        where T : IMetazoListItem
    {
        var response = await GetListAsync(dataRequest, cancellationToken);
        return JsonSerializer.Deserialize<MetazoListResponse<T>>(response);
    }
    
    /// <summary>
    /// Retrieves a specific item from the API based on its object ID.
    /// </summary>
    /// <param name="objectId">The unique identifier of the object to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the item.
    /// </returns>
    public async Task<JsonElement> GetItemAsync(int objectId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling GetItemAsync for ObjectId: {ObjectId}", objectId);
        return await ExecuteWithTokenRefreshAsync(async ct =>
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            Dictionary<string, string> parameters = new()
            {
                { "objectid", objectId.ToString() },
                { "token", token }
            };

            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);
            parameters.Add("checksum", checksum);

            var url = _urlBuilder.BuildUrl("read", parameters);

            return await _apiRequestSender.SendGetRequestAsync(url, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific item from the API and deserializes it into a specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type into which the item will be deserialized. This type must implement <see cref="IMetazoItemObject"/>.
    /// </typeparam>
    /// <param name="objectId">The unique identifier of the object to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="MetazoItemResponse{T}"/>
    /// with the deserialized item data.
    /// </returns>
    public async Task<MetazoItemResponse<T>?> GetItemAsync<T>(int objectId, CancellationToken cancellationToken = default)
        where T : IMetazoItemObject
    {
        var response = await GetItemAsync(objectId, cancellationToken);
        return JsonSerializer.Deserialize<MetazoItemResponse<T>>(response);
    }

    /// <summary>
    /// Sends a CRUD model request to the API.
    /// </summary>
    /// <param name="crudModel">
    /// The CRUD request model containing the operation, data type, and JSON payload.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the API response.
    /// </returns>
    public async Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling SendModelAsync for Datatype: {Datatype}, Model: {Model}", crudModel.Datatype, crudModel.Model);
        return await ExecuteWithTokenRefreshAsync(async ct =>
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            Dictionary<string, string> parameters = new()
            {
                { "datatype", crudModel.Datatype },
                { "model", crudModel.Model.ToString().ToLower() },
                { "token", token }
            };

            if (crudModel.ObjectId.HasValue)
            {
                parameters.Add("objectid", crudModel.ObjectId.Value.ToString());
            }

            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret, crudModel.JsonData);
            parameters.Add("checksum", checksum);

            var url = _urlBuilder.BuildUrl("model", parameters);

            return await _apiRequestSender.SendPostRequestAsync(url, crudModel.JsonData, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Uploads binary files to the API.
    /// </summary>
    /// <param name="model">
    /// The binary file upload request model containing the parent ID and file streams.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="JsonElement"/>
    /// representing the API response.
    /// </returns>
    public async Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest model, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling UploadBinfileAsync for ParentId: {ParentId}", model.ParentId);
        return await ExecuteWithTokenRefreshAsync(async ct =>
        {
            if (model.Files.Count == 0)
            {
                _logger.LogWarning("No files provided for upload.");
                throw new ArgumentNullException(nameof(model.Files));
            }

            var token = await _tokenProvider.GetTokenAsync(ct);

            // Calculate file checksums and prepare query parameters.
            Dictionary<string, string> fileChecksums = [];

            foreach (var file in model.Files)
            {
                var fileChecksum = await _fileChecksumService.CalculateChecksumAsync(file.Value, ct);
                fileChecksums.Add(file.Key, fileChecksum);
            }

            // Prepare query parameters.
            Dictionary<string, string> parameters = new()
            {
                { "parentid", model.ParentId.ToString() },
                { "token", token }
            };

            // Add file checksums to query parameters.
            foreach (var fileChecksum in fileChecksums)
            {
                parameters.Add(fileChecksum.Key, fileChecksum.Value);
            }

            // Calculate request checksum.
            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);
            parameters.Add("checksum", checksum);

            var url = _urlBuilder.BuildUrl("binfile/upload", parameters);

            return await _apiRequestSender.SendMultipartFormDataAsync(url, model.Files, ct);
        }, cancellationToken);
    }

    // Helper methods

    private async Task<JsonElement> ExecuteWithTokenRefreshAsync(Func<CancellationToken, Task<JsonElement>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            return await action(cancellationToken);
        }
        catch (TokenInvalidException tiex)
        {
            _logger.LogWarning(tiex, "Token invalid. Refreshing token and retrying.");
            await _tokenProvider.RefreshTokenAsync(cancellationToken);
            return await action(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing API call.");
            throw;
        }
    }
}
