using System.Net;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pure helpers extracted from OrchestratorAuthManager.GetAuthorizationCode so the
// PKCE authorize-URL construction (incl. the v1.9.2 macOS scope-encoding fix), the
// success-page language fallback, and the mounted-drive list become unit-testable.
public class PkceAuthHelpersTests
{
    // ---- BuildAuthorizeUrl ----

    [Fact]
    public void OnPrem_pkce_url_has_endpoint_scope_and_challenge()
    {
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            identityUrl: null, isCloud: false, baseUrl: "https://orch.local",
            scope: "OR.Default", appId: "APPID", redirectUrl: "http://127.0.0.1:5000/",
            useInPrivate: false, codeVerifier: "verifier-123");

        Assert.StartsWith("https://orch.local/identity/connect/authorize?response_type=code&client_id=APPID&scope=", url);
        Assert.Contains("scope=" + WebUtility.UrlEncode("OR.Default offline_access"), url); // v1.9.2: space encoded, offline_access appended
        Assert.Contains("redirect_uri=" + WebUtility.UrlEncode("http://127.0.0.1:5000/"), url);
        Assert.Contains("code_challenge=", url);
        Assert.Contains("&code_challenge_method=S256", url);
    }

    [Fact]
    public void Authorize_url_never_contains_a_raw_space()
    {
        // The v1.9.2 macOS bug: a raw space in the URL truncates the launched URL
        // at the first space, dropping redirect_uri. A multi-scope value is the
        // worst case; the whole URL must be space-free.
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            null, false, "https://orch.local", "OR.Foo OR.Bar", "A", "http://127.0.0.1:1/", false, "v");
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void Explicit_identity_url_wins_over_base_url()
    {
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            "https://id.example", isCloud: true, baseUrl: "https://ignored",
            scope: "S", appId: "A", redirectUrl: "http://127.0.0.1:1/", useInPrivate: false, codeVerifier: "v");
        Assert.StartsWith("https://id.example/connect/authorize?", url);
    }

    [Fact]
    public void Cloud_uses_common_path_and_acr_values()
    {
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            null, isCloud: true, baseUrl: "https://cloud.uipath.com/myorg",
            scope: "OR.Default", appId: "A", redirectUrl: "http://127.0.0.1:1/", useInPrivate: false, codeVerifier: "v");
        Assert.StartsWith("https://cloud.uipath.com/identity_/connect/authorize?", url);
        Assert.Contains("&acr_values=tenantName:myorg", url);
    }

    [Fact]
    public void Cloud_inprivate_omits_acr_values()
    {
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            null, isCloud: true, baseUrl: "https://cloud.uipath.com/myorg",
            scope: "OR.Default", appId: "A", redirectUrl: "http://127.0.0.1:1/", useInPrivate: true, codeVerifier: "v");
        Assert.DoesNotContain("acr_values", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Without_code_verifier_no_pkce_challenge_is_added(string? codeVerifier)
    {
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            null, false, "https://orch.local", "OR.Default", "A", "http://127.0.0.1:1/", false, codeVerifier);
        Assert.DoesNotContain("code_challenge", url);
    }

    // ---- ResolveNotificationLang ----

    [Theory]
    [InlineData("ja", "ja")]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    [InlineData("tr", "tr")]
    [InlineData("zz", "en")]   // unsupported -> English
    [InlineData("", "en")]
    [InlineData("JA", "en")]   // case-sensitive: only lowercase ISO codes are listed
    public void ResolveNotificationLang_falls_back_to_english(string input, string expected)
        => Assert.Equal(expected, OrchestratorAuthManager.ResolveNotificationLang(input));

    // ---- FormatMountedDriveList ----

    [Theory]
    [InlineData("Orch1:", "OR.Default", "Orch1:")]
    [InlineData("Orch1:", "OR.Default Du.Tasks.Read", "Orch1:, Orch1Du:")]
    [InlineData("Orch1:", "TM.Projects.Read", "Orch1:, Orch1Tm:")]
    [InlineData("Orch1:", "Du. TM.", "Orch1:, Orch1Du:, Orch1Tm:")]
    public void FormatMountedDriveList_adds_du_and_tm_shadow_drives_by_scope(string drive, string scope, string expected)
        => Assert.Equal(expected, OrchestratorAuthManager.FormatMountedDriveList(drive, scope));

    // ---- BuildOAuthCallbackErrorMessage ----
    // Identity can redirect back to the loopback listener with ?error=... instead of
    // ?code=... The listener used to ignore those callbacks, so the caller waited out
    // the full 3-minute PKCE timeout and the actual reason never reached the user.

    [Fact]
    public void Callback_error_names_the_oauth_error_code()
    {
        var msg = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage("access_denied", null, null);

        Assert.Contains("'access_denied'", msg);
        Assert.Contains("instead of an authorization code", msg);
        Assert.Contains("Import-OrchConfig", msg);
    }

    [Fact]
    public void Invalid_scope_gets_the_scope_specific_guidance()
    {
        // The case this path exists for: a scope list built against one deployment
        // moved to another whose Identity does not define one of the scopes.
        var msg = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage("invalid_scope", null, null);

        Assert.Contains("not recognized by this deployment's Identity", msg);
        Assert.Contains("Edit-OrchConfig", msg);
        // The generic branch must not also fire.
        Assert.DoesNotContain("redirect URI", msg);
    }

    [Fact]
    public void Invalid_scope_matching_is_case_insensitive()
    {
        var msg = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage("INVALID_SCOPE", null, null);
        Assert.Contains("not recognized by this deployment's Identity", msg);
    }

    [Theory]
    [InlineData("scope not allowed", "scope not allowed.")]   // punctuation added
    [InlineData("scope not allowed.", "scope not allowed.")]  // already punctuated, not doubled
    [InlineData("  padded  ", "padded.")]                     // trimmed
    public void Server_description_is_included_verbatim(string description, string expected)
    {
        var msg = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage("invalid_scope", description, null);
        Assert.Contains(expected, msg);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_description_and_uri_are_omitted(string? blank)
    {
        var msg = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage("invalid_scope", blank, blank);

        Assert.DoesNotContain("More information:", msg);
        // No dangling separator from an empty description.
        Assert.DoesNotContain("code.  ", msg);
    }

    [Fact]
    public void Error_uri_is_appended_when_present()
    {
        var msg = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage(
            "invalid_scope", null, " https://id.example/help ");

        Assert.Contains("More information: https://id.example/help.", msg);
    }
}
