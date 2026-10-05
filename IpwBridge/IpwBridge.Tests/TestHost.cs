using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using IpwBridge.Extensions;
using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace IpwBridge.Tests;

/// <summary>A recorded request, with the body read eagerly so it can be inspected after the call.</summary>
public sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, byte[] Body, string? ContentType)
{
    public string Path => Uri.AbsolutePath;

    public string BodyText => Encoding.UTF8.GetString(Body);

    public Dictionary<string, string> Query => Uri.Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : "");
}

/// <summary>An in-memory Metazo server plus a configured service provider.</summary>
public sealed class TestHost : IAsyncDisposable
{
    public const string Password = "S3cret!pass";
    public const string Secret = "checksum-secret";

    private readonly ServiceProvider _provider;
    private int _tokenCounter;

    public TestHost(Action<MetazoApiOptions>? configure = null, Action<IServiceCollection>? services = null)
    {
        Time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
        Handler = new FakeHandler(this);

        ServiceCollection collection = new();
        collection.AddSingleton<TimeProvider>(Time);
        collection.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(new ListLoggerProvider(Logs)));
        services?.Invoke(collection);
        collection.AddIpwBridge(o =>
            {
                o.IpwUrl = "https://metazo.test/metazo/api/v1/";
                o.IpwUser = "apiuser";
                o.IpwPassword = Password;
                o.ChecksumSecret = Secret;
                o.RetryBaseDelay = TimeSpan.Zero;
                configure?.Invoke(o);
            })
            .ConfigurePrimaryHttpMessageHandler(() => Handler);

        _provider = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public FakeTimeProvider Time { get; }

    public FakeHandler Handler { get; }

    public ConcurrentQueue<string> Logs { get; } = new();

    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

    public IMetazoApiClient Client => _provider.GetRequiredService<IMetazoApiClient>();

    public IServiceProvider Services => _provider;

    /// <summary>Handles a request; the default implementation serves authenticate and echoes success.</summary>
    public Func<RecordedRequest, int, HttpResponseMessage?>? Route { get; set; }

    public IEnumerable<RecordedRequest> To(string endpoint) => Requests.Where(r => r.Path.EndsWith("/" + endpoint, StringComparison.Ordinal));

    public string IssueToken(TimeSpan lifetime)
    {
        int n = Interlocked.Increment(ref _tokenCounter);
        long exp = (Time.GetUtcNow() + lifetime).ToUnixTimeSeconds();
        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64("{\"typ\":\"JWT\"}")}.{B64($"{{\"uid\":\"{n}\",\"exp\":{exp}}}")}.sig{n}";
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    internal HttpResponseMessage Dispatch(RecordedRequest request, int callIndex)
    {
        if (Route?.Invoke(request, callIndex) is { } custom)
        {
            return custom;
        }

        if (request.Path.EndsWith("/authenticate", StringComparison.Ordinal))
        {
            return Json(JsonSerializer.Serialize(new { success = "true", token = IssueToken(TimeSpan.FromHours(1)) }));
        }

        return Json("{\"success\":\"true\"}");
    }

    public ValueTask DisposeAsync() => _provider.DisposeAsync();

    public sealed class FakeHandler(TestHost host) : HttpMessageHandler
    {
        private int _calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            byte[] body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            Dictionary<string, string> headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
            RecordedRequest recorded = new(request.Method, request.RequestUri!, headers, body, request.Content?.Headers.ContentType?.ToString());
            host.Requests.Enqueue(recorded);
            return host.Dispatch(recorded, Interlocked.Increment(ref _calls));
        }
    }
}

public sealed class ListLoggerProvider(ConcurrentQueue<string> sink) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ListLogger(sink, categoryName);

    public void Dispose() { }

    private sealed class ListLogger(ConcurrentQueue<string> sink, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => sink.Enqueue($"{logLevel}|{category}|{formatter(state, exception)}");
    }
}

/// <summary>A readable stream that cannot seek and returns at most <paramref name="chunk"/> bytes per read.</summary>
public sealed class TrickleStream(byte[] data, int chunk = int.MaxValue, bool canSeek = false) : Stream
{
    private readonly MemoryStream _inner = new(data);

    public bool Disposed { get; private set; }

    public override bool CanRead => !Disposed;
    public override bool CanSeek => canSeek && !Disposed;
    public override bool CanWrite => false;
    public override long Length => canSeek ? _inner.Length : throw new NotSupportedException();

    public override long Position
    {
        get => canSeek ? _inner.Position : throw new NotSupportedException();
        set
        {
            if (!canSeek) throw new NotSupportedException();
            _inner.Position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, Math.Min(count, chunk));

    public override long Seek(long offset, SeekOrigin origin) => canSeek ? _inner.Seek(offset, origin) : throw new NotSupportedException();

    public override void Flush() { }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}
