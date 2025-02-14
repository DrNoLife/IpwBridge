using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace IpwBridge.Services;

public partial class UrlBuilder(IOptions<MetazoApiOptions> options, ILogger<UrlBuilder> logger) : IUrlBuilder
{
    private readonly MetazoApiOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<UrlBuilder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [GeneratedRegex("(?<=token=)[^&]*", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();

    public string BuildUrl(string endpoint, Dictionary<string, string> parameters)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrEmpty(_options.IpwUrl, nameof(_options.IpwUrl));

        // Ensure the base URL ends with a slash.
        string baseUrl = _options.IpwUrl.EndsWith('/')
            ? _options.IpwUrl
            : _options.IpwUrl + "/";

        string url = $"{baseUrl}{endpoint}";

        if (parameters is not null && parameters.Count > 0)
        {
            IEnumerable<string> parametersStringified = parameters
                .Select(kvp
                    => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");

            var query = String.Join("&", parametersStringified);
            url = $"{url}?{query}";
        }

        _logger.LogDebug("Built URL: {Url}", GetSafeUrl(url));

        return url;
    }

    public string GetSafeUrl(string url)
    {
        if (String.IsNullOrEmpty(url))
        {
            return url;
        }

        // Replace the value for the "token" parameter with "<token-hidden>".
        return TokenRegex().Replace(url, "<token-hidden>");
    }
}
