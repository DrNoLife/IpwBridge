using System.Text;
using IpwBridge.Models;
using Microsoft.Extensions.Options;

namespace IpwBridge.Services;

/// <summary>Builds absolute endpoint URLs from the configured base URL.</summary>
internal sealed class UrlBuilder(IOptions<MetazoApiOptions> options)
{
    private readonly string _baseUrl = options.Value.IpwUrl.EndsWith('/') ? options.Value.IpwUrl : options.Value.IpwUrl + "/";

    public string Build(string endpoint, IEnumerable<KeyValuePair<string, string>>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        StringBuilder url = new(_baseUrl);
        url.Append(endpoint);

        char separator = '?';
        foreach (var (key, value) in parameters ?? [])
        {
            url.Append(separator).Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
            separator = '&';
        }

        return url.ToString();
    }
}
