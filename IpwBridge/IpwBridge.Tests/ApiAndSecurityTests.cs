using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using IpwBridge.Contracts;
using IpwBridge.Contracts.Enums;
using IpwBridge.Exceptions;
using IpwBridge.Extensions;
using IpwBridge.Interfaces.Services;
using IpwBridge.Models;
using IpwBridge.Models.Responses.List;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IpwBridge.Tests;

public class ApiAndSecurityTests
{
    [Fact(DisplayName = "A2: several conditions and includeinactive are sent as documented")]
    public void A2_MultipleConditions()
    {
        var query = new ListRequest { DataType = "comment", IncludeInactive = true, SearchAndOr = SearchConnector.Or }
            .Where(DefaultSearchFields.ObjectId, SearchOperator.Greater, "1000")
            .Where("subject", SearchOperator.Like, "Api")
            .ToQueryParameters();

        Assert.Equal("1000;Api", query["search"]);
        Assert.Equal("objectid;subject", query["searchfield"]);
        Assert.Equal("GREATER;LIKE", query["searchcomp"]);
        Assert.Equal("OR", query["searchandor"]);
        Assert.Equal("1", query["includeinactive"]);
        Assert.Throws<ArgumentException>(() => new ListRequest { DataType = "x" }.Where("a", SearchOperator.Equal, "1;2").ToQueryParameters());
    }

    [Fact(DisplayName = "A3: ping, validate and revoke are wrapped")]
    public async Task A3_TokenEndpoints()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path switch
        {
            var p when p.EndsWith("/ping", StringComparison.Ordinal) => TestHost.Json("{\"success\":\"true\",\"ping\":\"pong\"}"),
            var p when p.EndsWith("/validate", StringComparison.Ordinal) => TestHost.Json("{\"success\":\"true\",\"validtoken\":true}"),
            var p when p.EndsWith("/revoke", StringComparison.Ordinal) => TestHost.Json("{\"success\":\"true\",\"result\":\"Token revoked\"}"),
            _ => null,
        };

        Assert.True(await host.Client.PingAsync());
        Assert.Empty(host.To("authenticate"));
        Assert.True(await host.Client.ValidateTokenAsync());
        await host.Client.RevokeTokenAsync();
        Assert.Single(host.To("revoke"));
        await host.Client.GetItemAsync(1);
        Assert.Equal(2, host.To("authenticate").Count()); // revoked token is not reused
    }

    [Fact(DisplayName = "A3: an expired token makes ValidateTokenAsync return false")]
    public async Task A3_ValidateExpired()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/validate", StringComparison.Ordinal)
            ? TestHost.Json("{\"error\":\"Bad request\",\"message\":\"Expired token\"}", HttpStatusCode.BadRequest)
            : null;

        Assert.False(await host.Client.ValidateTokenAsync());
    }

    [Fact(DisplayName = "A3: quickfilters use header authentication")]
    public async Task A3_Filter()
    {
        await using var host = new TestHost(o => { o.DatasourceToken = "ds-token"; o.Site = 3; });
        host.Route = (r, _) => r.Path.Contains("/filter/", StringComparison.Ordinal) ? TestHost.Json("{\"success\":\"true\",\"count\":4}") : null;

        await host.Client.GetFilterAsync(5784, includeInactive: true);
        await host.Client.GetFilterCountAsync(5784);

        var filter = host.To("5784").Single();
        Assert.Equal("ds-token", filter.Headers["usertoken"]);
        Assert.Equal("apiuser", filter.Headers["username"]);
        Assert.Equal("3", filter.Headers["site"]);
        Assert.Equal("1", filter.Query["includeinactive"]);
        Assert.Single(host.To("count"));
        await using var noToken = new TestHost();
        await Assert.ThrowsAsync<InvalidOperationException>(() => noToken.Client.GetFilterAsync(1));
    }

    [Fact(DisplayName = "A4/S1: login is a POST form with site and language; no credentials in the URL")]
    public async Task A4_S1_LoginForm()
    {
        await using var host = new TestHost(o => { o.Site = 4; o.Language = "DA"; });
        await host.Client.GetItemAsync(1);

        var auth = host.To("authenticate").Single();
        Assert.Equal(HttpMethod.Post, auth.Method);
        Assert.Empty(auth.Uri.Query);
        Assert.StartsWith("multipart/form-data", auth.ContentType, StringComparison.Ordinal);
        var form = System.Text.RegularExpressions.Regex.Matches(auth.BodyText, "name=(\\w+)\r\n\r\n(.*?)\r\n--")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        Assert.Equal(TestHost.Password, form["pass"]);
        Assert.Equal("4", form["site"]);
        Assert.Equal("DA", form["language"]);
        Assert.Equal("apiuser", form["user"]);
        Assert.Matches("^[0-9a-f]{40}$", form["checksum"]);
    }

    [Fact(DisplayName = "S1: the legacy GET login can be switched on")]
    public async Task S1_LegacyGet()
    {
        await using var host = new TestHost(o => o.UseLegacyGetAuthentication = true);
        await host.Client.GetItemAsync(1);

        var auth = host.To("authenticate").Single();
        Assert.Equal(HttpMethod.Get, auth.Method);
        Assert.Equal(TestHost.Password, auth.Query["pass"]);
        Assert.DoesNotContain(host.Logs, l => l.Contains(TestHost.Password, StringComparison.Ordinal));
        Assert.DoesNotContain(host.Logs, l => l.Contains(Uri.EscapeDataString(TestHost.Password), StringComparison.Ordinal));
    }

    [Fact(DisplayName = "R6: a failing destination stream surfaces as IpwBridgeException naming the write")]
    public async Task R6_DestinationWriteFailure()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/binfile/download", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) }
            : null;
        var destination = new MemoryStream(new byte[1], writable: true); // fixed capacity: writing 3 bytes fails

        var ex = await Assert.ThrowsAsync<IpwBridgeException>(() => host.Client.DownloadBinfileAsync(1, destination));
        Assert.Contains("destination stream", ex.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A5: builders set model and object id")]
    public void A5_Builders()
    {
        Assert.Equal("update", CrudRequest.Update("x", 5, "{}").Model.Value);
        Assert.Equal(5, CrudRequest.Update("x", 5, "{}").ObjectId);
        Assert.Equal("createcopy", CrudRequest.CreateCopy("x", 6).Model.Value);
        Assert.Equal("delete", CrudRequest.Delete("x", 7).Model.Value);
    }

    [Fact(DisplayName = "A5: a model response exposes the object id")]
    public async Task A5_ModelResponse()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/model", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"result\":{\"objectid\":\"6046\",\"model\":\"create\",\"modelresult\":false}}")
            : null;

        var response = await host.Client.SendModelAsync(CrudRequest.Create("comment", "{\"subject\":\"Api\",\"done\":true}"));

        Assert.Equal(6046, response.ObjectId);
        Assert.Equal("create", response.Model);
        var model = host.To("model").Single();
        Assert.Equal("{\"subject\":\"Api\",\"done\":true}", model.BodyText);
        Assert.StartsWith("application/json", model.ContentType, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A6: uploads carry the file name and content type")]
    public async Task A6_FileNameAndContentType()
    {
        await using var host = new TestHost();
        await host.Client.UploadBinfileAsync(new BinfileUploadRequest { ParentId = 1 }
            .AddFile("file_1", new MemoryStream([1, 2, 3]), "report.pdf", "application/pdf")
            .AddFile("file_2", new MemoryStream([4])));

        string body = host.To("binfile/upload").Single().BodyText;
        Assert.Contains("filename=report.pdf", body, StringComparison.Ordinal);
        Assert.Contains("Content-Type: application/pdf", body, StringComparison.Ordinal);
        Assert.Contains("filename=file_2", body, StringComparison.Ordinal);
        Assert.Contains("Content-Type: application/octet-stream", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A7: invalid options fail at startup with a clear message")]
    public void A7_OptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddIpwBridge(o => o.IpwUrl = "not a url");
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MetazoApiOptions>>().Value);
        Assert.Contains(ex.Failures, f => f.Contains("IpwUrl", StringComparison.Ordinal));
        Assert.Contains(ex.Failures, f => f.Contains("ChecksumSecret", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "A7: options bind from configuration")]
    public void A7_ConfigurationBinding()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["IpwBridge:IpwUrl"] = "https://metazo.test/api/",
            ["IpwBridge:IpwUser"] = "u",
            ["IpwBridge:IpwPassword"] = "p",
            ["IpwBridge:ChecksumSecret"] = "s",
            ["IpwBridge:Site"] = "2",
            ["IpwBridge:RequestTimeout"] = "00:00:30",
            ["IpwBridge:UseLegacyGetAuthentication"] = "true",
        }).Build();

        var services = new ServiceCollection();
        services.AddIpwBridge(configuration.GetSection("IpwBridge"));
        using var provider = services.BuildServiceProvider();

        var bound = provider.GetRequiredService<IOptions<MetazoApiOptions>>().Value;
        Assert.Equal(2, bound.Site);
        Assert.Equal(TimeSpan.FromSeconds(30), bound.RequestTimeout);
        Assert.True(bound.UseLegacyGetAuthentication);
        Assert.Equal(2, bound.MaxRetryAttempts); // unset values keep their defaults
    }

    [Fact(DisplayName = "A8/P5: AddIpwBridge returns the HttpClient builder, is idempotent and registers singletons")]
    public void A8_P5_Registration()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder builder = services.AddIpwBridge(o => { });
        services.AddIpwBridge(o => { });

        Assert.Equal("Metazo", builder.Name);
        Assert.Single(services, d => d.ServiceType == typeof(IMetazoApiClient));
        Assert.All(services.Where(d => d.ServiceType.Namespace?.StartsWith("IpwBridge", StringComparison.Ordinal) == true),
            d => Assert.Equal(ServiceLifetime.Singleton, d.Lifetime));
    }

    [Fact(DisplayName = "A10: only the intended types are public")]
    public void A10_PublicSurface()
    {
        var exported = typeof(IMetazoApiClient).Assembly.GetExportedTypes().Select(t => t.FullName!).ToHashSet();

        Assert.DoesNotContain(exported, n => n.StartsWith("IpwBridge.Services.", StringComparison.Ordinal));
        Assert.DoesNotContain("IpwBridge.Constants", exported);
        Assert.DoesNotContain(exported, n => n.Contains("Interfaces.Models", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "A14: any POCO works as item type, no interface required")]
    public async Task A14_PlainItemType()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"datatype\":\"c\",\"count\":1,\"limit\":20,\"offset\":0,\"items\":[{\"objectid\":\"9\",\"subject\":\"s\"}]}")
            : null;

        var page = await host.Client.GetListAsync<Comment>(new ListRequest { DataType = "c" });

        Assert.Equal("s", page.Items[0].Subject);
    }

    [Fact(DisplayName = "A15: GetAllAsync keeps paging when the server caps the page size")]
    public async Task A15_ServerCappedLimit()
    {
        await using var host = new TestHost();
        host.Route = (r, _) =>
        {
            if (!r.Path.EndsWith("/list", StringComparison.Ordinal)) return null;
            int offset = int.Parse(r.Query["offset"]);
            int count = offset < 200 ? 100 : 30; // the server applies limit=100 although 500 was asked for
            string items = string.Join(",", Enumerable.Range(offset, count).Select(i => $"{{\"objectid\":\"{i}\"}}"));
            return TestHost.Json($"{{\"success\":\"true\",\"datatype\":\"c\",\"limit\":\"100\",\"items\":[{items}]}}");
        };

        int total = 0;
        await foreach (var _ in host.Client.GetAllAsync<MetazoListItem>(new ListRequest { DataType = "c", Limit = 500 }))
        {
            total++;
        }

        Assert.Equal(230, total);
    }

    [Fact(DisplayName = "A15: a list response without items is an empty page")]
    public async Task A15_MissingItems()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal) ? TestHost.Json("{\"success\":\"true\",\"datatype\":\"c\",\"count\":0}") : null;

        var page = await host.Client.GetListAsync<MetazoListItem>(new ListRequest { DataType = "c" });

        Assert.Empty(page.Items);
    }

    [Fact(DisplayName = "A15: GetAllAsync pages until a short page")]
    public async Task A15_Paging()
    {
        await using var host = new TestHost();
        host.Route = (r, _) =>
        {
            if (!r.Path.EndsWith("/list", StringComparison.Ordinal)) return null;
            int offset = int.Parse(r.Query["offset"]);
            int count = offset < 4 ? 2 : 1;
            string items = string.Join(",", Enumerable.Range(offset, count).Select(i => $"{{\"objectid\":\"{i}\"}}"));
            return TestHost.Json($"{{\"success\":\"true\",\"datatype\":\"c\",\"count\":{count},\"items\":[{items}]}}");
        };

        List<string> ids = [];
        await foreach (var item in host.Client.GetAllAsync<MetazoListItem>(new ListRequest { DataType = "c", Limit = 2 }))
        {
            ids.Add(item.ObjectId);
        }

        Assert.Equal(["0", "1", "2", "3", "4"], ids);
        Assert.Equal(3, host.To("list").Count());
    }

    [Fact(DisplayName = "P2: a binfile can be downloaded straight into a stream")]
    public async Task P2_DownloadToStream()
    {
        byte[] file = Encoding.UTF8.GetBytes(new string('z', 100_000));
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/binfile/download", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(file) }
            : null;

        using var destination = new MemoryStream();
        await host.Client.DownloadBinfileAsync(42, destination);

        Assert.Equal(file, destination.ToArray());
        Assert.Equal(file, await host.Client.DownloadBinfileAsync(42));
    }

    [Theory(DisplayName = "P2/R8: a JSON token error answered with 200 on a download refreshes the token and never reaches the file")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task P2_DownloadJsonErrorIsDetected(bool toStream)
    {
        byte[] file = Encoding.UTF8.GetBytes("%PDF-1.7 binary content");
        await using var host = new TestHost();
        int downloads = 0;
        host.Route = (r, _) => r.Path.EndsWith("/binfile/download", StringComparison.Ordinal)
            ? Interlocked.Increment(ref downloads) == 1
                ? TestHost.Json("{\"success\":\"false\",\"message\":\"Token doesn't exist in the database\"}")
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(file) }
            : null;

        byte[] result;
        if (toStream)
        {
            using var destination = new MemoryStream();
            await host.Client.DownloadBinfileAsync(42, destination);
            result = destination.ToArray();
        }
        else
        {
            result = await host.Client.DownloadBinfileAsync(42);
        }

        Assert.Equal(file, result);
        Assert.Equal(2, host.To("authenticate").Count());
    }

    [Fact(DisplayName = "P2/R8: a non-token success=false answer on a download throws and writes nothing")]
    public async Task P2_DownloadApiError()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/binfile/download", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"false\",\"message\":\"No access to file\"}")
            : null;

        using var destination = new MemoryStream();
        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.DownloadBinfileAsync(42, destination));

        Assert.Equal("No access to file", ex.ServerMessage);
        Assert.Equal(0, destination.Length);
    }

    [Theory(DisplayName = "P2: a large JSON binfile is streamed through unchanged, with or without Content-Length")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task P2_LargeJsonBinfile(bool withLength)
    {
        byte[] file = Encoding.UTF8.GetBytes("{\"data\":\"" + new string('d', 200_000) + "\"}");
        await using var host = new TestHost();
        host.Route = (r, _) =>
        {
            if (!r.Path.EndsWith("/binfile/download", StringComparison.Ordinal)) return null;
            HttpContent content = withLength ? new ByteArrayContent(file) : new StreamContent(new TrickleStream(file, chunk: 8192));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        };

        using var destination = new MemoryStream();
        await host.Client.DownloadBinfileAsync(42, destination);

        Assert.Equal(file, destination.ToArray());
    }

    [Fact(DisplayName = "P2: a binfile that is itself JSON is downloaded unchanged")]
    public async Task P2_JsonBinfile()
    {
        byte[] file = Encoding.UTF8.GetBytes("{\"setting\":true}");
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/binfile/download", StringComparison.Ordinal) ? TestHost.Json("{\"setting\":true}") : null;

        using var destination = new MemoryStream();
        await host.Client.DownloadBinfileAsync(42, destination);

        Assert.Equal(file, destination.ToArray());
    }

    [Fact(DisplayName = "A7: every configurable option can be bound from configuration")]
    public void A7_SettingsCoverAllOptions()
    {
        var options = typeof(MetazoApiOptions).GetProperties().Where(p => p.Name != nameof(MetazoApiOptions.JsonTypeInfoResolver)).Select(p => p.Name);
        var settings = typeof(MetazoApiOptions).Assembly.GetType("IpwBridge.Models.MetazoApiSettings")!.GetProperties().Select(p => p.Name);

        Assert.Equal(options.Order(), settings.Order());
    }

    [Fact(DisplayName = "P3/S3: large error bodies are truncated and kept out of the exception message")]
    public async Task P3_S3_ErrorBodyTruncation()
    {
        string html = "<html>" + new string('e', 100_000) + "</html>";
        await using var host = new TestHost(o => o.MaxRetryAttempts = 0);
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(html) }
            : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetItemAsync(1));

        Assert.True(ex.ResponseBody!.Length <= 4096);
        Assert.True(ex.Message.Length < 400, $"message length {ex.Message.Length}");
        Assert.All(host.Logs, l => Assert.True(l.Length < 1000, "log line too long"));
    }

    [Fact(DisplayName = "P8/R8: a source-generated caller context deserializes caller types and success=false still throws")]
    public async Task P8_SourceGeneratedContext()
    {
        await using var host = new TestHost(o => o.JsonTypeInfoResolver = CommentJsonContext.Default);
        bool fail = false;
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal)
            ? fail
                ? TestHost.Json("{\"success\":\"false\",\"message\":\"Unknown datatype\",\"datatype\":\"x\",\"items\":[]}")
                : TestHost.Json("{\"success\":\"true\",\"datatype\":\"c\",\"count\":\"1\",\"items\":[{\"objectid\":\"1\",\"subject\":\"s\"}]}")
            : null;

        var page = await host.Client.GetListAsync<Comment>(new ListRequest { DataType = "c" });
        Assert.Equal("s", page.Items[0].Subject);
        Assert.Equal(1, page.Count);

        fail = true;
        var ex = await Assert.ThrowsAsync<IpwBridgeCommunicationException>(() => host.Client.GetListAsync<Comment>(new ListRequest { DataType = "x" }));
        Assert.Equal("Unknown datatype", ex.ServerMessage);
    }

    [Fact(DisplayName = "P8: a caller-supplied JSON resolver is used for caller types")]
    public async Task P8_CustomResolver()
    {
        var resolver = new CountingResolver();
        await using var host = new TestHost(o => o.JsonTypeInfoResolver = resolver);
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"datatype\":\"c\",\"items\":[{\"objectid\":\"1\",\"subject\":\"s\"}]}")
            : null;

        await host.Client.GetListAsync<Comment>(new ListRequest { DataType = "c" });

        Assert.Contains(typeof(MetazoListResponse<Comment>), resolver.Requested);
    }

    [Fact(DisplayName = "S2: request and response bodies are never logged")]
    public async Task S2_NoPayloadLogging()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/model", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"result\":{\"objectid\":\"1\",\"secretresponse\":\"r\"}}")
            : null;

        await host.Client.SendModelAsync(CrudRequest.Create("c", "{\"cpr\":\"010101-1234\"}"));

        Assert.DoesNotContain(host.Logs, l => l.Contains("010101-1234", StringComparison.Ordinal) || l.Contains("secretresponse", StringComparison.Ordinal));
    }

    public sealed class Comment
    {
        [JsonPropertyName("objectid")]
        public string ObjectId { get; set; } = "";

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }
    }

    private sealed class CountingResolver : IJsonTypeInfoResolver
    {
        private readonly DefaultJsonTypeInfoResolver _inner = new();

        public List<Type> Requested { get; } = [];

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            lock (Requested) Requested.Add(type);
            return type == typeof(MetazoListResponse<Comment>) || type == typeof(Comment) ? _inner.GetTypeInfo(type, options) : null;
        }
    }
}

[JsonSerializable(typeof(MetazoListResponse<ApiAndSecurityTests.Comment>))]
[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString)]
public sealed partial class CommentJsonContext : JsonSerializerContext;
