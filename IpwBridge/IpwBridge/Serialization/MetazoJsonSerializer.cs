using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using IpwBridge.Models;
using IpwBridge.Models.Responses;
using Microsoft.Extensions.Options;

namespace IpwBridge.Serialization;

/// <summary>
/// Holds the serializer options for every response. Library types use source-generated metadata; a caller's own
/// item types use the resolver from <see cref="MetazoApiOptions.JsonTypeInfoResolver"/> when set, and reflection
/// otherwise.
/// </summary>
internal sealed class MetazoJsonSerializer
{
    public MetazoJsonSerializer(IOptions<MetazoApiOptions> options)
    {
        List<IJsonTypeInfoResolver> resolvers = [];
        if (options.Value.JsonTypeInfoResolver is { } custom)
        {
            resolvers.Add(custom);
        }

        resolvers.Add(JsonContext.Default);
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            resolvers.Add(CreateReflectionResolver());
        }

        Options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = JsonTypeInfoResolver.Combine([.. resolvers]),
        };
        Options.MakeReadOnly();
    }

    public JsonSerializerOptions Options { get; }

    public JsonTypeInfo<T> GetTypeInfo<T>() => (JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));

    // Only reached when reflection-based serialization is enabled, which trimmed and Native AOT applications
    // disable; those must supply their types through MetazoApiOptions.JsonTypeInfoResolver.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    private static DefaultJsonTypeInfoResolver CreateReflectionResolver() => new();
}
