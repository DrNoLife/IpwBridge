using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using IpwBridge.Models;
using IpwBridge.Interfaces.Services;

namespace IpwBridge.Services;

/// <summary>
/// Provides token-based authentication for interacting with the IPW Metazo API.
/// </summary>
/// <remarks>
/// The <see cref="TokenProvider"/> manages retrieval and refreshing of authentication tokens,
/// ensuring that a valid token is used for API calls. It employs a semaphore to handle concurrent requests.
/// </remarks>
public class TokenProvider(
    IOptions<MetazoApiOptions> options,
    IHttpClientFactory httpClientFactory,
    IChecksumService checksumService,
    ILogger<TokenProvider> logger) : ITokenProvider
{
    private readonly MetazoApiOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IChecksumService _checksumService = checksumService;
    private readonly ILogger<TokenProvider> _logger = logger;
    private string _token = String.Empty;
    private DateTime _tokenExpiry;

    /// <summary>
    /// Retrieves a valid authentication token. If the current token is expired or not present, a new token is obtained.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the valid authentication token as a string.
    /// </returns>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
        {
            _logger.LogDebug("Returning cached token.");
            return _token;
        }

        _logger.LogInformation("Token expired or not found. Acquiring new token.");
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            {
                _logger.LogDebug("Token was refreshed by another thread; returning cached token.");
                return _token;
            }

            _token = await AuthenticateAsync(cancellationToken);
            _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
            _logger.LogInformation("New token acquired successfully.");
            return _token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting token.");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Refreshes the authentication token by clearing the current token and retrieving a new one.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing token.");
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _token = string.Empty;
            _tokenExpiry = DateTime.MinValue;

            // Reauthenticate.
            _token = await AuthenticateAsync(cancellationToken);
            _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
            _logger.LogInformation("Token refreshed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while refreshing token.");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting authentication process.");
        Dictionary<string, string> parameters = new()
        {
            { "pass", _options.IpwPassword },
            { "site", "1" },
            { "user", _options.IpwUser }
        };

        var checksum = _checksumService.CalculateChecksum(parameters, _options.ChecksumSecret);
        parameters.Add("checksum", checksum);

        var query = String.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var url = $"{_options.IpwUrl}authenticate?{query}";

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var authResponse = JsonSerializer.Deserialize<IpwAuthenticationSuccessMessage>(content);
            _logger.LogDebug("Authentication succeeded.");
            return authResponse?.Token ?? throw new Exception($"Failed to get authentication token, despite API giving good response. {response.StatusCode}");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Authentication failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new Exception($"Authentication failed: {response.StatusCode} - {errorContent}");
        }
    }
}
