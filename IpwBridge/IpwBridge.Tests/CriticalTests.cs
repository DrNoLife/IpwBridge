using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using IpwBridge.Contracts;
using IpwBridge.Models.Responses.Item;
using IpwBridge.Models.Responses.List;

namespace IpwBridge.Tests;

public class CriticalTests
{
    private static readonly byte[] FileContent = Encoding.UTF8.GetBytes(new string('x', 300) + new string('y', 4700));

    private static string Sha1Prefix(byte[] data) =>
        Convert.ToHexString(SHA1.HashData(data.AsSpan(0, Math.Min(256, data.Length)))).ToLowerInvariant();

    [Fact(DisplayName = "K1: password and token never appear in any log line")]
    public async Task K1_NoSecretsInLogs()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/datatypes", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"count\":0,\"datatypes\":[]}")
            : null;
        await host.Client.GetDatatypesAsync();
        await host.Client.GetItemAsync(5);

        string token = host.To("read").First().Query["token"];
        Assert.NotEmpty(host.Logs);
        Assert.DoesNotContain(host.Logs, l => l.Contains(TestHost.Password, StringComparison.Ordinal));
        Assert.DoesNotContain(host.Logs, l => l.Contains(Uri.EscapeDataString(TestHost.Password), StringComparison.Ordinal));
        Assert.DoesNotContain(host.Logs, l => l.Contains(token, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "K1: Microsoft.Extensions.Http resolves to a version that redacts query strings (>= 9)")]
    public void K1_HttpPackageVersion()
    {
        var version = typeof(Microsoft.Extensions.DependencyInjection.HttpClientFactoryServiceCollectionExtensions).Assembly.GetName().Version!;
        Assert.True(version.Major >= 9, $"Microsoft.Extensions.Http {version}");
    }

    [Theory(DisplayName = "K2/P1: every stream type is uploaded in full with the documented checksum")]
    [InlineData("memory")]
    [InlineData("file")]
    [InlineData("nonseekable")]
    public async Task K2_UploadsFullContent(string kind)
    {
        await using var host = new TestHost();
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, FileContent);
            await using Stream stream = kind switch
            {
                "memory" => new MemoryStream(FileContent),
                "file" => File.OpenRead(path),
                _ => new TrickleStream(FileContent),
            };

            await host.Client.UploadBinfileAsync(new BinfileUploadRequest { ParentId = 7 }.AddFile("file_1", stream));

            var upload = host.To("binfile/upload").Single();
            Assert.True(ContainsSequence(upload.Body, FileContent), "multipart body must contain the whole file");
            Assert.Equal(Sha1Prefix(FileContent), upload.Query["file_1"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact(DisplayName = "K2: a seekable stream that was just written (position at the end) is uploaded from the start")]
    public async Task K2_PositionedStreamIsRewound()
    {
        await using var host = new TestHost();
        var stream = new MemoryStream();
        stream.Write(FileContent);

        await host.Client.UploadBinfileAsync(new BinfileUploadRequest { ParentId = 7 }.AddFile("file_1", stream));

        var upload = host.To("binfile/upload").Single();
        Assert.True(ContainsSequence(upload.Body, FileContent));
        Assert.Equal(Sha1Prefix(FileContent), upload.Query["file_1"]);
    }

    [Theory(DisplayName = "K3: an upload rejected for its token is resent in full with a fresh token, and the stream stays open")]
    [InlineData("memory")]
    [InlineData("file")]
    [InlineData("nonseekable")]
    public async Task K3_UploadRetryAfterTokenRejection(string kind)
    {
        await using var host = new TestHost();
        int uploads = 0;
        host.Route = (r, _) =>
        {
            if (!r.Path.EndsWith("binfile/upload", StringComparison.Ordinal)) return null;
            return Interlocked.Increment(ref uploads) == 1
                ? TestHost.Json("{\"error\":\"Bad request\",\"message\":\"Token doesn't exist in the database\"}", HttpStatusCode.BadRequest)
                : TestHost.Json("{\"success\":\"true\",\"uploadedfiles\":[{\"file_1\":\"991\"}]}");
        };

        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, FileContent);
            Stream stream = kind switch
            {
                "memory" => new MemoryStream(FileContent),
                "file" => File.OpenRead(path),
                _ => new TrickleStream(FileContent),
            };

            await using (stream)
            {
                var response = await host.Client.UploadBinfileAsync(new BinfileUploadRequest { ParentId = 7 }.AddFile("file_1", stream));

                Assert.Equal("991", response.UploadedFiles["file_1"]);
                var attempts = host.To("binfile/upload").ToList();
                Assert.Equal(2, attempts.Count);
                Assert.Equal(attempts[0].Body.Length, attempts[1].Body.Length);
                Assert.True(ContainsSequence(attempts[1].Body, FileContent));
                Assert.NotEqual(attempts[0].Query["token"], attempts[1].Query["token"]);
                Assert.True(stream.CanRead, "the caller's stream must not be disposed");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory(DisplayName = "K4: typed list accepts count, limit and offset as strings or numbers")]
    [InlineData("\"20\"", "\"20\"", "\"0\"")]
    [InlineData("20", "20", "0")]
    public async Task K4_ListNumbersAsStrings(string count, string limit, string offset)
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/list", StringComparison.Ordinal)
            ? TestHost.Json($"{{\"success\":\"true\",\"datatype\":\"comment\",\"primaryfield\":\"name\",\"count\":{count},\"limit\":{limit},\"offset\":{offset},\"items\":[{{\"objectid\":\"5799\",\"language\":\"EN\",\"subject\":\"Api\"}}]}}")
            : null;

        var page = await host.Client.GetListAsync<MetazoListItem>(new ListRequest { DataType = "comment" });

        Assert.Equal(20, page.Count);
        Assert.Equal("5799", page.Items[0].ObjectId);
        Assert.Equal("Api", page.Items[0].AdditionalFields["subject"].GetString());
    }

    [Fact(DisplayName = "K5: typed read accepts objectid as a string")]
    public async Task K5_ItemObjectIdAsString()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/read", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"true\",\"result\":{\"objectid\":\"5045\",\"fields\":{\"objectid\":\"5045\",\"site\":\"1\",\"type\":\"binfile\"}}}")
            : null;

        var item = await host.Client.GetItemAsync<MetazoItemObject>(5045);

        Assert.Equal(5045, item.Result.ObjectId);
        Assert.Equal("1", item.Result.Fields.Site);
        Assert.Equal("binfile", item.Result.Fields.AdditionalFields["type"].GetString());
    }

    internal static bool ContainsSequence(byte[] haystack, byte[] needle)
        => haystack.AsSpan().IndexOf(needle) >= 0;
}
