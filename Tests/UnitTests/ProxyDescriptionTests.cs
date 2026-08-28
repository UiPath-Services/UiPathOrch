using System.Net;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// A connection failure names only the address it could not reach. When that address is a proxy,
// and especially one inherited from the machine rather than configured on the drive, nothing in
// the error or the configuration says a proxy is involved -- reported from the field as
// "Could not connect to 127.0.0.1:10000", read as a port the module wanted to listen on.
// These pin the wording that closes that gap, and the cases where there is nothing to say.
public class ProxyDescriptionTests
{
    private sealed class FakeProxy(Uri? result) : IWebProxy
    {
        public ICredentials? Credentials { get; set; }
        public Uri? GetProxy(Uri destination) => result;
        public bool IsBypassed(Uri host) => result is null;
    }

    private static readonly Uri Destination = new("https://cloud.uipath.com/acme/tenant");

    [Fact]
    public void No_proxy_says_nothing()
    {
        var note = OrchHttp.DescribeEffectiveProxy(Destination, configured: null, systemProxy: new FakeProxy(null));

        Assert.Equal("", note);
    }

    [Fact]
    public void A_proxy_answering_with_the_destination_says_nothing()
    {
        // A hand-built WebProxy reports "no proxy for this one" by echoing the destination,
        // where the system proxy returns null. Both mean the same and must read the same.
        var note = OrchHttp.DescribeEffectiveProxy(Destination, configured: null, systemProxy: new FakeProxy(Destination));

        Assert.Equal("", note);
    }

    [Fact]
    public void An_inherited_proxy_is_named_and_attributed_to_the_machine()
    {
        var note = OrchHttp.DescribeEffectiveProxy(
            Destination, configured: null, systemProxy: new FakeProxy(new Uri("http://127.0.0.1:10000")));

        Assert.Contains("http://127.0.0.1:10000", note);
        Assert.Contains("inherited from this machine", note);
        // The point the field report got wrong: an absent Proxy block is not the same as no proxy.
        Assert.Contains("does not by itself stop a proxy being used", note);
    }

    [Fact]
    public void A_disabled_proxy_block_still_reports_the_inherited_one()
    {
        // Enabled:false is what the shipped config template contains, so this is the common case.
        var configured = new ProxySettings { Enabled = false, Url = "http://unused.example:8080" };

        var note = OrchHttp.DescribeEffectiveProxy(
            Destination, configured, systemProxy: new FakeProxy(new Uri("http://127.0.0.1:10000")));

        Assert.Contains("http://127.0.0.1:10000", note);
        Assert.DoesNotContain("unused.example", note);
    }

    [Fact]
    public void A_configured_proxy_is_named_from_the_drive_configuration()
    {
        var configured = new ProxySettings { Enabled = true, Url = "http://proxy.example:8080" };

        var note = OrchHttp.DescribeEffectiveProxy(Destination, configured, systemProxy: new FakeProxy(null));

        Assert.Contains("proxy.example:8080", note);
        Assert.Contains("Proxy block", note);
        Assert.DoesNotContain("inherited", note);
    }

    // ---- UseProxy ----

    [Fact]
    public void UseProxy_false_turns_the_handler_off()
    {
        using var handler = OrchHttp.CreateHandler(new ProxySettings { UseProxy = false }, ignoreSslErrors: false);

        Assert.False(handler.UseProxy);
    }

    [Fact]
    public void UseProxy_false_beats_an_enabled_block()
    {
        // "Use no proxy" is the more specific instruction, so it wins.
        var configured = new ProxySettings { UseProxy = false, Enabled = true, Url = "http://proxy.example:8080" };

        using var handler = OrchHttp.CreateHandler(configured, ignoreSslErrors: false);

        Assert.False(handler.UseProxy);
        Assert.Null(handler.Proxy);
    }

    [Fact]
    public void UseProxy_absent_leaves_the_handler_at_the_dotnet_default()
    {
        // Which is to use the machine's proxy -- the behaviour every existing config has.
        using var handler = OrchHttp.CreateHandler(new ProxySettings { Enabled = false }, ignoreSslErrors: false);

        Assert.True(handler.UseProxy);
    }

    [Fact]
    public void UseProxy_false_reports_no_proxy_in_the_error()
    {
        // Otherwise the annotation would name the machine's proxy on a connection that
        // deliberately did not use it.
        var configured = new ProxySettings { UseProxy = false };

        var note = OrchHttp.DescribeEffectiveProxy(
            Destination, configured, systemProxy: new FakeProxy(new Uri("http://127.0.0.1:10000")));

        Assert.Equal("", note);
    }

    // ---- AnnotateProxyFailure ----

    [Fact]
    public void An_already_annotated_exception_is_not_annotated_twice()
    {
        // The two send sites nest: SendOnce reads the HttpClient property inside its own try, and
        // that getter runs the auth send. A proxy failure during token acquisition is therefore
        // caught, annotated, and caught again one frame out -- which printed the paragraph twice
        // before this guard existed.
        var configured = new ProxySettings { Enabled = true, Url = "http://proxy.example:8080" };
        var original = new HttpRequestException("Could not connect.");

        var once = OrchHttp.AnnotateProxyFailure(original, Destination, configured);
        Assert.NotNull(once);

        var twice = OrchHttp.AnnotateProxyFailure((HttpRequestException)once!, Destination, configured);
        Assert.Null(twice);
    }

    [Fact]
    public void Annotation_keeps_the_original_as_inner_and_prepends_its_message()
    {
        var configured = new ProxySettings { Enabled = true, Url = "http://proxy.example:8080" };
        var original = new HttpRequestException("Could not connect.");

        var annotated = OrchHttp.AnnotateProxyFailure(original, Destination, configured)!;

        Assert.StartsWith("Could not connect.", annotated.Message);
        Assert.Same(original, annotated.InnerException);
    }

    [Fact]
    public void Nothing_to_add_yields_null_rather_than_a_copy()
    {
        var original = new HttpRequestException("Could not connect.");

        Assert.Null(OrchHttp.AnnotateProxyFailure(original, requestUri: null, configured: null));
    }

    [Fact]
    public void A_configured_proxy_using_internet_options_says_so()
    {
        var configured = new ProxySettings { Enabled = true, UseDefaultWebProxy = true, Url = "http://ignored.example" };

        var note = OrchHttp.DescribeEffectiveProxy(Destination, configured, systemProxy: new FakeProxy(null));

        Assert.Contains("Internet Options", note);
        Assert.DoesNotContain("ignored.example", note);
    }
}
