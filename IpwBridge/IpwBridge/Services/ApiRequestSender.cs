using IpwBridge.Exceptions;
using IpwBridge.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace IpwBridge.Services;

public class ApiRequestSender(IHttpClientFactory httpClientFactory, ILogger<ApiRequestSender> logger) : IApiRequestSender
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ApiRequestSender> _logger = logger;

    public async Task<T> SendGetRequestAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending GET request to URL: {Url}", url);

        var client = _httpClientFactory.CreateClient("Metazo");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        _logger.LogDebug("Received GET response with status code: {StatusCode}", response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            T? result = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);

            // If the caller expects a type other than JsonElement and we got a null value, then throw an exception.
            if (result is null && typeof(T) != typeof(JsonElement))
            {
                _logger.LogError("Deserialization failed for type {TypeName} from URL: {Url}", typeof(T).Name, url);
                throw new IpwBridgeDeserializationException(
                    $"Failed to deserialize the JSON response into an object of type '{typeof(T).Name}'. " +
                    "Ensure that the JSON is valid and matches the expected schema.");
            }

            _logger.LogDebug("Deserialization succeeded for type {TypeName} from URL: {Url}", typeof(T).Name, url);
            return result!;
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken); 
            _logger.LogError("GET request to URL {Url} failed with status code {StatusCode}. Error: {ErrorContent}",
                url, response.StatusCode, errorContent);
            HandleErrorResponse(response.StatusCode.ToString(), errorContent);
            throw new IpwBridgeCommunicationException("Unhandled error in GET request."); // Should not reach here.
        }
    }

    public async Task<T> SendPostRequestAsync<T>(string url, string jsonData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending POST request to URL: {Url} with payload: {Payload}", url, jsonData);

        var client = _httpClientFactory.CreateClient("Metazo");
        using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        _logger.LogDebug("Received POST response with status code: {StatusCode}", response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            T? result = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);

            // If the caller expects a type other than JsonElement and we got a null value, then throw an exception.
            if (result is null && typeof(T) != typeof(JsonElement))
            {
                _logger.LogError("Deserialization failed for type {TypeName} from URL: {Url}", typeof(T).Name, url);

                throw new IpwBridgeDeserializationException(
                    $"Failed to deserialize the JSON response into an object of type '{typeof(T).Name}'. " +
                    "Ensure that the JSON is valid and matches the expected schema.");
            }

            _logger.LogDebug("Deserialization succeeded for type {TypeName} from URL: {Url}", typeof(T).Name, url);
            return result!;
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("POST request to URL {Url} failed with status code {StatusCode}. Error: {ErrorContent}",
                url, response.StatusCode, errorContent);
            HandleErrorResponse(response.StatusCode.ToString(), errorContent);
            throw new IpwBridgeCommunicationException("Unhandled error in POST request.");
        }
    }

    public async Task<JsonElement> SendMultipartFormDataAsync(string url, Dictionary<string, Stream> files, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("Metazo");
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            // If the stream is a FileStream, we extract the file name; otherwise use a default.
            string fileName = file.Value is FileStream fs ? System.IO.Path.GetFileName(fs.Name) : "uploadfile";
            content.Add(new StreamContent(file.Value), file.Key, fileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
            return result;
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            HandleErrorResponse(response.StatusCode.ToString(), errorContent);
            throw new IpwBridgeCommunicationException("Unhandled error in multipart request."); // Should not reach here.
        }
    }

    private void HandleErrorResponse(string statusCode, string errorContent)
    {
        if (IsTokenInvalidError(errorContent))
        {
            throw new TokenInvalidException("Token is invalid or has been revoked.");
        }
            
        _logger.LogError("Error calling API: {StatusCode} - {ErrorContent}", statusCode, errorContent);
        throw new IpwBridgeCommunicationException($"Error calling API: {statusCode} - {errorContent}");
    }

    private static bool IsTokenInvalidError(string errorContent)
        => errorContent.Contains(Constants.TokenInvalidMessage);
}