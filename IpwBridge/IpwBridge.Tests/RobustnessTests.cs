using System.Net;
using System.Reflection;
using IpwBridge.Contracts;
using IpwBridge.Exceptions;

namespace IpwBridge.Tests;

public class RobustnessTests
{
    [Fact(DisplayName = "R1: transient GET failures are retried with backoff")]
    public async Task R1_GetIsRetried()
    {
        await using var host = new TestHost(o => o.MaxRetryAttempts = 2);
        int calls = 0;
        host.Route = (r, _) => r.Path.EndsWith("/datatypes", StringComparison.Ordinal) && Interlocked.Increment(ref calls) < 3
            ? TestHost.Json("{\"error\":\"Unexpected error\"}", HttpStatusCode.ServiceUnavailable)
            : r.Path.EndsWith("/datatypes", StringComparison.Ordinal) ? TestHost.Json("{\"success\":\"true\",\"count\":0,\"datatypes\":[]}") : null;

        var response = await host.Client.GetDatatypesAsync();

        Assert.Empty(response.Datatypes);
        Assert.Equal(3, host.To("datatypes").Count());
    }

    [Fact(DisplayName = "R1: a bare 500 is retried, but a structured Metazo 500 error is not")]
    public async Task R1_StructuredServerErrorsAreNotRetried()
    {
        await using var host = new TestHost(o => o.MaxRetryAttempts = 2);
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? TestHost.Json("{\"error\":\"Unexpected error\",\"message\":\"Object not found\"}", HttpStatusCode.InternalServerError)
            : r.Path.EndsWith("/explain", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("<html>proxy</html>") }
                : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetItemAsync(1));
        Assert.Equal("Unexpected error: Object not found", ex.ServerMessage);
        Assert.Single(host.To("read"));

        await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetExplanationAsync("x"));
        Assert.Equal(3, host.To("explain").Count());
    }

    [Fact(DisplayName = "R1: a structured 500 error larger than the kept 4 KB is still recognized and not retried")]
    public async Task R1_LargeStructuredErrorIsNotRetried()
    {
        await using var host = new TestHost(o => o.MaxRetryAttempts = 2);
        string json = $"{{\"error\":\"Unexpected error\",\"message\":\"failed\",\"trace\":\"{new string('t', 20_000)}\"}}";
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal) ? TestHost.Json(json, HttpStatusCode.InternalServerError) : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetItemAsync(1));

        Assert.Single(host.To("read"));
        Assert.Equal("Unexpected error: failed", ex.ServerMessage);
        Assert.True(ex.ResponseBody!.Length <= 4096);
    }

    [Fact(DisplayName = "R6: responses that start with a UTF-8 byte order mark are accepted")]
    public async Task R6_ByteOrderMark()
    {
        await using var host = new TestHost();
        host.Route = (r, _) =>
        {
            string? json = r.Path.EndsWith("/authenticate", StringComparison.Ordinal)
                ? $"{{\"success\":\"true\",\"token\":\"{host.IssueToken(TimeSpan.FromHours(1))}\"}}"
                : r.Path.EndsWith("/datatypes", StringComparison.Ordinal) ? "{\"success\":\"true\",\"datatypes\":[{\"id\":\"f1\",\"name\":\"n\"}]}" : null;
            if (json is null) return null;
            byte[] body = [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes(json)];
            var content = new ByteArrayContent(body);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        };

        var response = await host.Client.GetDatatypesAsync();

        Assert.Equal("f1", response.Datatypes[0].Id);
    }

    [Fact(DisplayName = "R6: a caller type the serializer cannot construct surfaces as IpwBridgeDeserializationException")]
    public async Task R6_UnsupportedCallerType()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"datatype\":\"c\",\"items\":[{\"objectid\":\"1\"}]}")
            : null;

        await Assert.ThrowsAsync<IpwBridgeDeserializationException>(
            () => host.Client.GetListAsync<Unconstructable>(new ListRequest { DataType = "c" }));
    }

    [Fact(DisplayName = "R6: a read error from the caller's upload stream propagates unchanged, seekable or not")]
    public async Task R6_CallerStreamErrorsPropagate()
    {
        await using var host = new TestHost();

        await Assert.ThrowsAsync<IOException>(() => host.Client.UploadBinfileAsync(
            new BinfileUploadRequest { ParentId = 1 }.AddFile("f", new FailingStream(canSeek: false))));
        await Assert.ThrowsAsync<IOException>(() => host.Client.UploadBinfileAsync(
            new BinfileUploadRequest { ParentId = 1 }.AddFile("f", new FailingStream(canSeek: true))));
    }

    private sealed class FailingStream(bool canSeek) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => canSeek;
        public override bool CanWrite => false;
        public override long Length => 10;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("disk gone");
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    public sealed class Unconstructable(int unrelated)
    {
        public int Value => unrelated;
    }

    [Theory(DisplayName = "R1: backoff doubles per attempt, adds at most 20 % jitter and is capped at 30 s")]
    [InlineData(1, 0.0, 500)]
    [InlineData(2, 0.0, 1000)]
    [InlineData(3, 1.0, 2400)]
    [InlineData(10, 1.0, 30000)]
    public void R1_Backoff(int attempt, double random, double expectedMilliseconds)
    {
        var delay = IpwBridge.Services.ApiRequestSender.GetBackoff(TimeSpan.FromMilliseconds(500), attempt, random);
        Assert.Equal(expectedMilliseconds, delay.TotalMilliseconds, 3);
    }

    [Fact(DisplayName = "R1: POST /model is never retried on transient failures")]
    public async Task R1_PostIsNotRetried()
    {
        await using var host = new TestHost(o => o.MaxRetryAttempts = 3);
        host.Route = (r, _) => r.Path.EndsWith("/model", StringComparison.Ordinal) ? TestHost.Json("{}", HttpStatusCode.BadGateway) : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.SendModelAsync(CrudRequest.Create("comment", "{}")));
        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Single(host.To("model"));
    }

    [Fact(DisplayName = "R2: authentication goes through the configured named client and a base URL without trailing slash works")]
    public async Task R2_AuthUsesNamedClientAndUrlBuilder()
    {
        // The fake handler is only attached to the IpwBridge client; a request on any other client would fail.
        await using var host = new TestHost(o => o.IpwUrl = "https://metazo.test/metazo/api/v1");
        host.Route = (r, _) => r.Path.EndsWith("/datatypes", StringComparison.Ordinal) ? TestHost.Json("{\"success\":\"true\",\"datatypes\":[]}") : null;
        await host.Client.GetDatatypesAsync();

        var auth = host.To("authenticate").Single();
        Assert.Equal("/metazo/api/v1/authenticate", auth.Path);
    }

    [Fact(DisplayName = "R3: a response body that stalls is cut off by the request timeout")]
    public async Task R3_BodyReadTimeout()
    {
        await using var host = new TestHost(o =>
        {
            o.RequestTimeout = TimeSpan.FromMilliseconds(300);
            o.MaxRetryAttempts = 0;
        });
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallingStream()) }
            : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetItemAsync(1));
        Assert.IsType<TimeoutException>(ex.InnerException);
    }

    [Fact(DisplayName = "R3: cancellation by the caller surfaces as OperationCanceledException")]
    public async Task R3_CallerCancellation()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallingStream()) }
            : null;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.Client.GetItemAsync(1, cts.Token));
    }

    [Fact(DisplayName = "R4: many concurrent token rejections cause exactly one re-authentication")]
    public async Task R4_SingleReauthentication()
    {
        await using var host = new TestHost();
        string? firstToken = null;
        host.Route = (r, _) =>
        {
            if (r.Path.EndsWith("/datatypes", StringComparison.Ordinal))
            {
                firstToken = r.Query["token"];
                return TestHost.Json("{\"success\":\"true\",\"datatypes\":[]}");
            }

            if (!r.Path.EndsWith("/read", StringComparison.Ordinal)) return null;
            return r.Query["token"] == firstToken
                ? TestHost.Json("{\"error\":\"Bad request\",\"message\":\"Expired token\"}", HttpStatusCode.BadRequest)
                : TestHost.Json("{\"success\":\"true\",\"result\":{}}");
        };

        await host.Client.GetDatatypesAsync(); // caches the first token
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => host.Client.GetItemAsync(1))));

        Assert.Equal(2, host.To("authenticate").Count());
    }

    [Fact(DisplayName = "R5: the token is renewed shortly before its JWT exp claim")]
    public async Task R5_TokenLifetimeFromJwt()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/authenticate", StringComparison.Ordinal)
            ? TestHost.Json($"{{\"success\":\"true\",\"token\":\"{host.IssueToken(TimeSpan.FromMinutes(10))}\"}}")
            : null;

        await host.Client.GetItemAsync(1);
        host.Time.Advance(TimeSpan.FromMinutes(8));
        await host.Client.GetItemAsync(1);
        Assert.Single(host.To("authenticate"));

        host.Time.Advance(TimeSpan.FromMinutes(1.5)); // inside the 60 s safety margin
        await host.Client.GetItemAsync(1);
        Assert.Equal(2, host.To("authenticate").Count());
    }

    [Fact(DisplayName = "R5: a token that expires within the safety margin is cached until its own exp")]
    public async Task R5_ShortLivedToken()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/authenticate", StringComparison.Ordinal)
            ? TestHost.Json($"{{\"success\":\"true\",\"token\":\"{host.IssueToken(TimeSpan.FromSeconds(30))}\"}}")
            : null;

        await host.Client.GetItemAsync(1);
        host.Time.Advance(TimeSpan.FromSeconds(20));
        await host.Client.GetItemAsync(1);
        Assert.Single(host.To("authenticate")); // still cached before exp

        host.Time.Advance(TimeSpan.FromSeconds(20));
        await host.Client.GetItemAsync(1);
        Assert.Equal(2, host.To("authenticate").Count());
    }

    [Fact(DisplayName = "R6: every exception type derives from IpwBridgeException")]
    public void R6_ExceptionHierarchy()
    {
        var types = typeof(IpwBridgeException).Assembly.GetExportedTypes().Where(t => typeof(Exception).IsAssignableFrom(t));
        Assert.All(types, t => Assert.True(typeof(IpwBridgeException).IsAssignableFrom(t), t.Name));
    }

    [Fact(DisplayName = "R7: one failed call produces exactly one Error log entry")]
    public async Task R7_SingleErrorLog()
    {
        await using var host = new TestHost(o => o.MaxRetryAttempts = 0);
        host.Route = (r, _) => r.Path.EndsWith("/datatypes", StringComparison.Ordinal) ? TestHost.Json("boom", HttpStatusCode.InternalServerError) : null;

        await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetDatatypesAsync());

        Assert.Single(host.Logs, l => l.StartsWith("Error|", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "R8: HTTP 200 with success=false is an error")]
    public async Task R8_SuccessFalseThrows()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"false\",\"message\":\"No access\"}")
            : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetItemAsync(1));
        Assert.Equal("No access", ex.ServerMessage);
    }

    [Fact(DisplayName = "R8: success=false is also detected on caller-defined response types")]
    public async Task R8_SuccessFalseOnCallerTypes()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"false\",\"message\":\"Unknown datatype\",\"datatype\":\"x\",\"items\":[]}")
            : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(
            () => host.Client.GetListAsync<ApiAndSecurityTests.Comment>(new ListRequest { DataType = "x" }));
        Assert.Equal("Unknown datatype", ex.ServerMessage);
    }

    [Fact(DisplayName = "R8: HTTP 200 with success=false and a token message triggers the token refresh")]
    public async Task R8_SuccessFalseTokenMessageRefreshes()
    {
        await using var host = new TestHost();
        int reads = 0;
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? Interlocked.Increment(ref reads) == 1
                ? TestHost.Json("{\"success\":false,\"message\":\"token doesn't exist in the database\"}")
                : TestHost.Json("{\"success\":true,\"result\":{}}")
            : null;

        await host.Client.GetItemAsync(1);

        Assert.Equal(2, host.To("authenticate").Count());
    }

    [Fact(DisplayName = "R3: timeouts beyond the CancelAfter limit are rejected at startup")]
    public void R3_TimeoutUpperBound()
    {
        var validator = new IpwBridge.Models.MetazoApiOptionsValidator();
        var options = new IpwBridge.Models.MetazoApiOptions
        {
            IpwUrl = "https://x.test/", IpwUser = "u", IpwPassword = "p", ChecksumSecret = "s",
            RequestTimeout = TimeSpan.FromDays(30),
        };

        Assert.True(validator.Validate(null, options).Failed);
        options.RequestTimeout = Timeout.InfiniteTimeSpan;
        Assert.True(validator.Validate(null, options).Succeeded);
    }

    [Fact(DisplayName = "R9: invalid arguments throw ArgumentException without logging or HTTP traffic")]
    public async Task R9_ArgumentValidation()
    {
        await using var host = new TestHost();

        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.UploadBinfileAsync(new BinfileUploadRequest { ParentId = 1 }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => host.Client.UploadBinfileAsync(null!));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => host.Client.GetItemAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.SendModelAsync(new CrudRequest { Datatype = "x", Model = "update" }));
        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.SendModelAsync(CrudRequest.Create("x", "[1,2]")));
        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.SendModelAsync(CrudRequest.Create("x", "{not json")));

        Assert.Empty(host.Requests);
        Assert.DoesNotContain(host.Logs, l => l.StartsWith("Error|", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "R10: the token cache holds token and expiry in one volatile reference")]
    public void R10_TokenStateIsAtomic()
    {
        var field = typeof(IpwBridge.Services.TokenProvider).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.Contains(typeof(System.Runtime.CompilerServices.IsVolatile), field.GetRequiredCustomModifiers());
    }

    private sealed class StallingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
