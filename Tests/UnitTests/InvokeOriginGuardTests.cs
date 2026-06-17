using System;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins Invoke-OrchApi's cross-host token-leak guard (v1.9.3). An absolute -ApiPath
// URL is accepted only when it shares scheme + host + port with one of the drive's
// own base URLs (Orchestrator / Identity / Portal); otherwise the drive's live
// bearer token would be sent to an arbitrary, possibly http-downgraded, host.
// IsKnownOrigin is the comparison at the heart of that decision.
public class InvokeOriginGuardTests
{
    [Theory]
    // --- accepted: same origin ---
    [InlineData("https://cloud.uipath.com/odata/Releases", "https://cloud.uipath.com", true)]
    [InlineData("https://cloud.uipath.com/api", "https://cloud.uipath.com/identity_", true)] // path on base ignored — origin only
    [InlineData("https://CLOUD.UiPath.com/x", "https://cloud.uipath.com", true)]             // host case-insensitive
    [InlineData("HTTPS://cloud.uipath.com/x", "https://cloud.uipath.com", true)]             // scheme case-insensitive
    [InlineData("https://cloud.uipath.com:443/x", "https://cloud.uipath.com", true)]         // explicit default port == implicit
    [InlineData("https://cloud.uipath.com/x", "https://cloud.uipath.com:443", true)]         // implicit == explicit default port
    [InlineData("http://orch.local:80/x", "http://orch.local", true)]                        // http default port
    [InlineData("https://orch.local:8443/x", "https://orch.local:8443", true)]               // matching non-default port
    [InlineData("https://user:pass@cloud.uipath.com/x", "https://cloud.uipath.com", true)]   // userinfo stripped from Host
    // --- rejected: different origin ---
    [InlineData("http://cloud.uipath.com/x", "https://cloud.uipath.com", false)]             // scheme downgrade https->http
    [InlineData("https://cloud.uipath.com:8443/x", "https://cloud.uipath.com", false)]       // non-default port mismatch
    [InlineData("https://cloud.uipath.com/x", "https://cloud.uipath.com:8443", false)]       // port mismatch (other direction)
    [InlineData("https://evil.com/x", "https://cloud.uipath.com", false)]                    // different host
    [InlineData("https://evil.cloud.uipath.com/x", "https://cloud.uipath.com", false)]       // subdomain is a different host
    [InlineData("https://cloud.uipath.com.evil.com/x", "https://cloud.uipath.com", false)]   // suffix-attack host
    [InlineData("https://cloud.uipath.com./x", "https://cloud.uipath.com", false)]           // trailing-dot host fails closed (safe)
    public void Matches_only_same_scheme_host_port(string candidate, string baseUrl, bool expected)
        => Assert.Equal(expected, InvokeOrchApiCmdlet.IsKnownOrigin(new Uri(candidate, UriKind.Absolute), baseUrl));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("/relative/only")]
    public void Rejects_when_base_url_is_missing_or_unparseable(string? baseUrl)
        => Assert.False(InvokeOrchApiCmdlet.IsKnownOrigin(
            new Uri("https://cloud.uipath.com/x", UriKind.Absolute), baseUrl));
}
