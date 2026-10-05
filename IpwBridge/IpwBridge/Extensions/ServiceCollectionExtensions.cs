using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using IpwBridge.Serialization;
using IpwBridge.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IpwBridge.Extensions;

/// <summary>Registers IpwBridge in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IMetazoApiClient"/>, configured in code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Sets the options; they are validated when the application starts.</param>
    /// <returns>The builder of the underlying <see cref="HttpClient"/>, for adding handlers or resilience policies.</returns>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// builder.Services.AddIpwBridge(options =>
    /// {
    ///     options.IpwUrl = "https://your.metazo.domain/metazo/api/v1/";
    ///     options.IpwUser = "apiuser";
    ///     options.IpwPassword = builder.Configuration["Metazo:Password"]!;
    ///     options.ChecksumSecret = builder.Configuration["Metazo:ChecksumSecret"]!;
    /// });
    /// ]]></code>
    /// </example>
    public static IHttpClientBuilder AddIpwBridge(this IServiceCollection services, Action<MetazoApiOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<MetazoApiOptions>().Configure(configureOptions).ValidateOnStart();
        return AddCore(services);
    }

    /// <summary>Registers <see cref="IMetazoApiClient"/>, bound to a configuration section.</summary>
    /// <remarks>
    /// Binding is source-generated and safe for trimming and Native AOT. Keys that are missing or empty keep their
    /// defaults. To set <see cref="MetazoApiOptions.JsonTypeInfoResolver"/>, add
    /// <c>services.Configure&lt;MetazoApiOptions&gt;(o =&gt; o.JsonTypeInfoResolver = ...)</c> after this call.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The section holding the <see cref="MetazoApiOptions"/> properties, for example <c>Configuration.GetSection("IpwBridge")</c>.</param>
    /// <returns>The builder of the underlying <see cref="HttpClient"/>, for adding handlers or resilience policies.</returns>
    public static IHttpClientBuilder AddIpwBridge(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MetazoApiOptions>()
            .Configure(options => configuration.Get<MetazoApiSettings>()?.ApplyTo(options))
            .ValidateOnStart();
        services.AddSingleton<IOptionsChangeTokenSource<MetazoApiOptions>>(
            new ConfigurationChangeTokenSource<MetazoApiOptions>(Options.DefaultName, configuration));
        return AddCore(services);
    }

    private static IHttpClientBuilder AddCore(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MetazoApiOptions>, MetazoApiOptionsValidator>());
        services.TryAddSingleton(sp => new IpwBridgeClock(sp.GetService<TimeProvider>() ?? TimeProvider.System));
        services.TryAddSingleton<MetazoJsonSerializer>();
        services.TryAddSingleton<UrlBuilder>();
        services.TryAddSingleton<ChecksumService>();
        services.TryAddSingleton<ApiRequestSender>();
        services.TryAddSingleton<TokenProvider>();
        services.TryAddSingleton<IMetazoApiClient, MetazoApiClient>();

        // Timeouts are enforced per request (see MetazoApiOptions.RequestTimeout and BinfileTimeout), so the
        // client-wide timeout is disabled to keep it from cutting long binfile transfers short.
        return services.AddHttpClient(Constants.HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan);
    }
}
