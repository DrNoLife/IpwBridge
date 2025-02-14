namespace IpwBridge.Interfaces.Services;

public interface IUrlBuilder
{
    /// <summary>
    /// Builds a URL based on the provided endpoint and query parameters.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="parameters">Query parameters as key/value pairs.</param>
    /// <returns>A fully constructed URL.</returns>
    string BuildUrl(string endpoint, Dictionary<string, string> parameters);
}
