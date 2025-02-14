using System.Text.Json;

namespace IpwBridge.Interfaces.Services;

public interface IApiRequestSender
{
    Task<T> SendGetRequestAsync<T>(string url, CancellationToken cancellationToken = default);

    Task<T> SendPostRequestAsync<T>(string url, string jsonData, CancellationToken cancellationToken = default);

    Task<JsonElement> SendMultipartFormDataAsync(string url, Dictionary<string, Stream> files, CancellationToken cancellationToken = default);

}
