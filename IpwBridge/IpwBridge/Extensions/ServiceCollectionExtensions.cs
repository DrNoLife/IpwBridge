using IpwBridge.Interfaces;
using IpwBridge.Models;
using IpwBridge.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IpwBridge.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIpwBridge(this IServiceCollection services, Action<MetazoApiOptions> configureOptions)
    {
        services.AddHttpClient();

        services.Configure(configureOptions);

        // Register services.
        services.AddSingleton<IChecksumService, ChecksumService>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddTransient<IMetazoApiClient, MetazoApiClient>();

        return services;
    }
}
