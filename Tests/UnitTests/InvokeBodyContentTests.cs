using System.Management.Automation;
using System.Net.Http;
using System.Text;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins Invoke-OrchApi's request-body construction, in particular the PSObject
// unwrap: PowerShell can hand a [string] body wrapped in a live PSObject, and
// without unwrapping it the string would fall into JsonSerializer.Serialize and
// throw a System.Text.Json object-cycle exception. The contract: a wrapped
// string is sent verbatim, exactly like a raw string.
public class InvokeBodyContentTests
{
    [Fact]
    public async Task PSObjectWrappedString_IsSentVerbatim_NotJsonSerialized()
    {
        const string json = """{ "name": "n", "value": "v" }""";
        var content = InvokeOrchApiCmdlet.BuildBodyContent(new PSObject(json), "application/json");

        Assert.IsType<StringContent>(content);
        Assert.Equal(json, await content.ReadAsStringAsync());
        Assert.Equal("application/json", content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task RawString_IsSentVerbatim()
    {
        const string json = """{ "a": 1 }""";
        var content = InvokeOrchApiCmdlet.BuildBodyContent(json, "application/json");

        Assert.IsType<StringContent>(content);
        Assert.Equal(json, await content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ByteArray_BypassesSerialization_AndDefaultsToOctetStream()
    {
        byte[] payload = Encoding.UTF8.GetBytes("binary-blob");
        var content = InvokeOrchApiCmdlet.BuildBodyContent(payload, "");

        Assert.IsType<ByteArrayContent>(content);
        Assert.Equal(payload, await content.ReadAsByteArrayAsync());
        Assert.Equal("application/octet-stream", content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public void EmptyContentType_DefaultsToApplicationJsonForStrings()
    {
        var content = InvokeOrchApiCmdlet.BuildBodyContent("{}", "");
        Assert.Equal("application/json", content.Headers.ContentType!.MediaType);
    }
}
