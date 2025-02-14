using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using IpwBridge.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IpwBridge.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIpwBridge(this IServiceCollection services, Action<MetazoApiOptions> configureOptions)
    {
        services.AddHttpClient(Constants.HttpClientName);

        services.Configure(configureOptions);

        // Register services.
        services.AddSingleton<IChecksumService, ChecksumService>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddTransient<IMetazoApiClient, MetazoApiClient>();
        services.AddTransient<IFileChecksumService, FileChecksumService>();
        services.AddTransient<IUrlBuilder, UrlBuilder>();
        services.AddTransient<IApiRequestSender, ApiRequestSender>();

        return services;
    }
}
