using Microsoft.Extensions.Options;
using System.Text.Json;
using IpwBridge.Models;
using IpwBridge.Interfaces;

namespace IpwBridge.Services;

public class TokenProvider(
    IOptions<MetazoApiOptions> options,
    IHttpClientFactory httpClientFactory,
    IChecksumService checksumService) : ITokenProvider
{
    private readonly MetazoApiOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IChecksumService _checksumService = checksumService;
    private string _token = String.Empty;
    private DateTime _tokenExpiry;

    public async Task<string> GetTokenAsync()
    {
        if (!String.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
        {
            return _token;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (!String.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            {
                return _token;
            }

            _token = await AuthenticateAsync();
            _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
            return _token;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RefreshTokenAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _token = String.Empty;
            _tokenExpiry = DateTime.MinValue;

            // Reauthenticate.
            _token = await AuthenticateAsync();
            _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> AuthenticateAsync()
    {
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
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<IpwAuthenticationSuccessMessage>(content);
            return authResponse?.Token ?? throw new Exception($"Failed to get authentication token, despite API giving good response. {response.StatusCode}");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Authentication failed: {response.StatusCode} - {errorContent}");
        }
    }
}
