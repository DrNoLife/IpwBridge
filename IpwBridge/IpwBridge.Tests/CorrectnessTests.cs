using System.Globalization;
using System.Net;
using IpwBridge.Contracts;
using IpwBridge.Contracts.Enums;
using IpwBridge.Contracts.Models;
using IpwBridge.Exceptions;
using IpwBridge.Models;
using IpwBridge.Services;
using Microsoft.Extensions.Options;

namespace IpwBridge.Tests;

public class CorrectnessTests
{
    [Fact(DisplayName = "C1: a default list request sends no search parameters")]
    public async Task C1_DefaultListRequestHasNoSearch()
    {
        await using var host = new TestHost();
        await host.Client.GetListAsync(new ListRequest { DataType = "comment" });

        var query = host.To("list").Single().Query;
        Assert.DoesNotContain("search", query.Keys);
        Assert.DoesNotContain("searchfield", query.Keys);
        Assert.DoesNotContain("searchcomp", query.Keys);
        Assert.DoesNotContain("fields", query.Keys);
        Assert.Equal("20", query["limit"]);
    }

    [Fact(DisplayName = "C1: FromDate becomes a created >= date condition")]
    public void C1_FromDateIsSent()
    {
        var query = new ListRequest { DataType = "comment", FromDate = new DateTime(2026, 9, 5) }.ToQueryParameters();

        Assert.Equal("2026-09-05", query["search"]);
        Assert.Equal("created", query["searchfield"]);
        Assert.Equal("GREATEREQUAL", query["searchcomp"]);
    }

    [Fact(DisplayName = "C2: checksum matches the documented PHP reference vector")]
    public void C2_ChecksumMatchesDocumentation()
    {
        var service = new ChecksumService(Options.Create(new MetazoApiOptions
        {
            ChecksumSecret = "GJrSGgo0scmKvYSnX87kjSnAoSASUnh9XKXPT3X0kFwg3abK9n4Wp2tOAAkGQV1H4aFP4yYauq9MmvO2BCMwOQ==",
        }));

        string checksum = service.Calculate(new Dictionary<string, string> { ["site"] = "1", ["user"] = "demouser", ["pass"] = "demopass" });

        Assert.Equal("4cca594a61204d3751aaf42360e2df1c61360e1f", checksum);
    }

    [Fact(DisplayName = "C2: JSON booleans and null are canonicalized like PHP string conversion")]
    public void C2_JsonValuesLikePhp()
    {
        var service = new ChecksumService(Options.Create(new MetazoApiOptions { ChecksumSecret = "k" }));
        var empty = new Dictionary<string, string>();

        string fromJson = service.Calculate(empty, "{\"b\":true,\"a\":false,\"c\":null,\"d\":1.50,\"e\":\"x\"}");
        string expected = service.Calculate(new Dictionary<string, string> { ["a"] = "", ["b"] = "1", ["c"] = "", ["d"] = "1.50", ["e"] = "x" });

        Assert.Equal(expected, fromJson);
    }

    [Fact(DisplayName = "C2: keys are used exactly as sent, like the PHP reference (no lowercasing)")]
    public void C2_KeysKeepTheirCase()
    {
        var service = new ChecksumService(Options.Create(new MetazoApiOptions { ChecksumSecret = "k" }));

        string checksum = service.Calculate(new Dictionary<string, string> { ["File_1"] = "abc", ["age"] = "1" });

        // ksort is byte-wise: "File_1" (F = 0x46) sorts before "age" (a = 0x61).
        string expected = Convert.ToHexString(System.Security.Cryptography.HMACSHA1.HashData(
            "k"u8.ToArray(), System.Text.Encoding.UTF8.GetBytes("File_1abcage1"))).ToLowerInvariant();
        Assert.Equal(expected, checksum);
    }

    [Fact(DisplayName = "C2: keys are sorted ordinally regardless of culture")]
    public void C2_OrdinalSort()
    {
        var service = new ChecksumService(Options.Create(new MetazoApiOptions { ChecksumSecret = "k" }));
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK"); // "aa" sorts after "z" in Danish.
            string danish = service.Calculate(new Dictionary<string, string> { ["aa"] = "1", ["z"] = "2" });
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            string invariant = service.Calculate(new Dictionary<string, string> { ["z"] = "2", ["aa"] = "1" });
            Assert.Equal(invariant, danish);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact(DisplayName = "C3: a 200 response with success=false on authenticate throws IpwBridgeAuthenticationException")]
    public async Task C3_AuthenticationFailureThrows()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/authenticate", StringComparison.Ordinal)
            ? TestHost.Json("{\"success\":\"false\",\"token\":\"\",\"message\":\"Wrong password\"}")
            : null;

        var ex = await Assert.ThrowsAsync<IpwBridgeAuthenticationException>(() => host.Client.GetDatatypesAsync());
        Assert.Contains("Wrong password", ex.Message, StringComparison.Ordinal);
        Assert.Single(host.To("authenticate"));
        Assert.Empty(host.To("datatypes"));
    }

    [Fact(DisplayName = "C3: a login that returns an empty token throws instead of caching it")]
    public async Task C3_EmptyTokenThrows()
    {
        await using var host = new TestHost();
        host.Route = (r, _) => r.Path.EndsWith("/authenticate", StringComparison.Ordinal) ? TestHost.Json("{\"success\":\"true\",\"token\":\"\"}") : null;

        await Assert.ThrowsAsync<IpwBridgeAuthenticationException>(() => host.Client.GetDatatypesAsync());
    }

    [Fact(DisplayName = "C4: a request without a model is rejected with ArgumentException before any HTTP call")]
    public async Task C4_DefaultModelRejected()
    {
        await using var host = new TestHost();
        var request = new CrudRequest { Datatype = "comment", Model = default, JsonData = "{}" };

        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.SendModelAsync(request));
        Assert.Empty(host.Requests);
        Assert.Throws<ArgumentNullException>(() => { CrudModel model = (string)null!; });
        Assert.Equal(string.Empty, default(SearchField).ToString());
    }

    [Fact(DisplayName = "C5: protocol values are culture-invariant (Turkish casing, Thai calendar)")]
    public void C5_CultureInvariant()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("objectid", ((SearchField)DefaultSearchFields.ObjectId).Value);
            Assert.Equal("LIKE", ((SearchOperation)SearchOperator.Like).Value);
            Assert.Equal("createcopy", ((CrudModel)ModelOptions.CreateCopy).Value);

            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            var query = new ListRequest { DataType = "x", Limit = 1000, FromDate = new DateTime(2026, 1, 2) }.ToQueryParameters();
            Assert.Equal("2026-01-02", query["search"]);
            Assert.Equal("1000", query["limit"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact(DisplayName = "C6: a stream that returns one byte per read still hashes the full 256-byte prefix")]
    public async Task C6_ShortReads()
    {
        byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
        await using var host = new TestHost();

        await host.Client.UploadBinfileAsync(new BinfileUploadRequest { ParentId = 1 }.AddFile("f", new TrickleStream(data, chunk: 1, canSeek: true)));

        string expected = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(data.AsSpan(0, 256))).ToLowerInvariant();
        Assert.Equal(expected, host.To("binfile/upload").Single().Query["f"]);
    }

    [Theory(DisplayName = "C7: reserved or duplicate file keys are rejected")]
    [InlineData("token")]
    [InlineData("ParentId")]
    [InlineData("checksum")]
    public async Task C7_ReservedFileKeys(string key)
    {
        await using var host = new TestHost();
        var request = new BinfileUploadRequest { ParentId = 1 }.AddFile(key, new MemoryStream([1]));

        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.UploadBinfileAsync(request));
        await Assert.ThrowsAsync<ArgumentException>(() => host.Client.UploadBinfileAsync(
            new BinfileUploadRequest { ParentId = 1 }.AddFile("a", new MemoryStream([1])).AddFile("A", new MemoryStream([2]))));
        Assert.Empty(host.Requests);
    }

    [Fact(DisplayName = "C7: JSON fields that collide with query parameters are rejected")]
    public async Task C7_JsonKeyCollision()
    {
        await using var host = new TestHost();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            host.Client.SendModelAsync(CrudRequest.Create("comment", "{\"datatype\":\"x\"}")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            host.Client.SendModelAsync(CrudRequest.Update("comment", 1, "{\"a\":\"1\",\"a\":\"2\"}")));
        Assert.Empty(host.Requests);
    }

    [Fact(DisplayName = "C8: the unconstructable IpwModel type no longer exists")]
    public void C8_IpwModelRemoved()
    {
        Assert.Null(typeof(MetazoApiOptions).Assembly.GetType("IpwBridge.Models.IpwModel"));
    }
}
