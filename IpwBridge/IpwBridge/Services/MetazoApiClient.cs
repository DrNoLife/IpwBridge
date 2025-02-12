using IpwBridge.Interfaces;
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

namespace IpwBridge.Services;

public class MetazoApiClient(
    IOptions<MetazoApiOptions> options,
    IHttpClientFactory httpClientFactory,
    ITokenProvider tokenProvider,
    IChecksumService checksumService) : IMetazoApiClient
{
    private readonly MetazoApiOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IChecksumService _checksumService = checksumService;

    public async Task<MetazoDatatypesResponse?> GetDatatypesAsync(CancellationToken cancellationToken = default)
    {
        var json = await ExecuteWithTokenRefreshAsync(async ct =>
        {
            var token = await _tokenProvider.GetTokenAsync(ct);
            Dictionary<string, string> parameters = new()
            {
                { "token", token }
            };

            var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);

            parameters.Add("checksum", checksum);

            var url = BuildUrl("datatypes", parameters);
            return await SendGetRequestAsync(url, ct);
        }, cancellationToken);

        return JsonSerializer.Deserialize<MetazoDatatypesResponse>(json);
    }

    public async Task<MetazoExplanationResponse?> GetExplanationAsync(string datatype, CancellationToken cancellationToken = default)
    {
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

            var url = BuildUrl("explain", parameters);
            return await SendGetRequestAsync(url, ct);
        }, cancellationToken);

        return JsonSerializer.Deserialize<MetazoExplanationResponse>(json);
    }

    public async Task<JsonElement> GetListAsync(ListRequest dataRequest, CancellationToken cancellationToken = default)
    {
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

            var url = BuildUrl("list", parameters);
            return await SendGetRequestAsync(url, ct);
        }, cancellationToken);
    }

    public async Task<MetazoListResponse<T>?> GetListAsync<T>(ListRequest dataRequest, CancellationToken cancellationToken = default)
        where T : IMetazoListItem
    {
        var response = await GetListAsync(dataRequest, cancellationToken);

        return JsonSerializer.Deserialize<MetazoListResponse<T>>(response);
    }

    public async Task<JsonElement> GetItemAsync(int objectId, CancellationToken cancellationToken = default)
    {
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

            var url = BuildUrl("read", parameters);
            return await SendGetRequestAsync(url, ct);
        }, cancellationToken);
    }

    public async Task<MetazoItemResponse<T>?> GetItemAsync<T>(int objectId, CancellationToken cancellationToken = default)
        where T : IMetazoItemObject
    {
        var response = await GetItemAsync(objectId, cancellationToken);

        return JsonSerializer.Deserialize<MetazoItemResponse<T>>(response);
    }

    public async Task<JsonElement> SendModelAsync(IpwCrudRequest crudModel, CancellationToken cancellationToken = default)
    {
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

            var url = BuildUrl("model", parameters);

            return await SendPostRequestAsync(url, crudModel.JsonData, ct);
        }, cancellationToken);
    }

    public async Task<JsonElement> UploadBinfileAsync(BinfileUploadRequest model, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithTokenRefreshAsync(async ct =>
        {
            if (model.Files.Count == 0)
            {
                throw new ArgumentNullException(nameof(model.Files));
            }

            var token = await _tokenProvider.GetTokenAsync(ct);

            // Calculate file checksums and prepare query parameters.
            Dictionary<string, string> fileChecksums = new();

            foreach (var file in model.Files)
            {
                var fileChecksum = await CalculateFileChecksumAsync(file.Value, ct);
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

            var url = BuildUrl("binfile/upload", parameters);
            return await SendMultipartFormDataAsync(url, model.Files, ct);
        }, cancellationToken);
    }

    // Helper methods
    private async Task<JsonElement> ExecuteWithTokenRefreshAsync(Func<CancellationToken, Task<JsonElement>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            return await action(cancellationToken);
        }
        catch (TokenInvalidException)
        {
            // Invalidate the token and re-authenticate
            await _tokenProvider.RefreshTokenAsync(cancellationToken);
            return await action(cancellationToken);
        }
    }

    private static async Task<string> CalculateFileChecksumAsync(Stream fileContent, CancellationToken cancellationToken = default)
    {
        fileContent.Position = 0;
        byte[] buffer = new byte[256];
        int bytesRead = await fileContent.ReadAsync(buffer.AsMemory(0, 256), cancellationToken);
        byte[] hash = SHA1.HashData(buffer.AsSpan(0, bytesRead));

        fileContent.Position = 0;

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private string BuildUrl(string endpoint, Dictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{_options.IpwUrl}{endpoint}?{query}";
    }

    private async Task<JsonElement> SendGetRequestAsync(string url, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(contentString);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (IsTokenInvalidError(errorContent))
            {
                throw new TokenInvalidException("Token is invalid or has been revoked.");
            }

            throw new Exception($"Error calling API: {response.StatusCode} - {errorContent}");
        }
    }

    private async Task<JsonElement> SendPostRequestAsync(string url, string jsonData, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(contentString);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (IsTokenInvalidError(errorContent))
            {
                throw new TokenInvalidException("Token is invalid or has been revoked.");
            }

            throw new Exception($"Error calling API: {response.StatusCode} - {errorContent}");
        }
    }

    private async Task<JsonElement> SendMultipartFormDataAsync(string url, Dictionary<string, Stream> files, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        using MultipartFormDataContent content = new();

        foreach (var file in files)
        {
            var filePath = ((FileStream)file.Value).Name;
            var fileNameAndExtension = Path.GetFileName(filePath);

            content.Add(new StreamContent(file.Value), file.Key, fileNameAndExtension);
        }

        var response = await client.PostAsync(url, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(contentString);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (IsTokenInvalidError(errorContent))
            {
                throw new TokenInvalidException("Token is invalid or has been revoked.");
            }

            throw new Exception($"Error uploading file: {response.StatusCode} - {errorContent}");
        }
    }

    private static bool IsTokenInvalidError(string errorContent)
    {
        return errorContent.Contains("Token doesn't exist in the database");
    }
}
