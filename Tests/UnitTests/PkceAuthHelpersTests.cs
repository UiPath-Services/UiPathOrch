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

    // ---- state (RFC 8252 §8.9) ----

    [Fact]
    public void Authorize_url_carries_the_state()
    {
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            null, false, "https://orch.local", "OR.Default", "A", "http://127.0.0.1:1/", false, "v",
            state: "st ate/+value");

        Assert.Contains("&state=" + WebUtility.UrlEncode("st ate/+value"), url);
    }

    [Fact]
    public void Authorize_url_omits_state_when_none_is_supplied()
    {
        // The parameter is optional so the pure function stays usable without one; the sign-in
        // path always passes one.
        var url = OrchestratorAuthManager.BuildAuthorizeUrl(
            null, false, "https://orch.local", "OR.Default", "A", "http://127.0.0.1:1/", false, "v");

        Assert.DoesNotContain("state=", url);
    }

    [Fact]
    public void Matching_state_is_accepted()
    {
        Assert.True(OrchestratorAuthManager.IsExpectedState("abc123", "abc123"));
    }

    [Theory]
    [InlineData("abc123", "different")]
    [InlineData("abc123", "ABC123")]  // ordinal: case is part of the value
    [InlineData("abc123", null)]      // an injected callback that simply omits it
    [InlineData("abc123", "")]
    [InlineData(null, "abc123")]
    [InlineData("", "")]              // "no state" must never be the value that passes
    public void Non_matching_state_is_rejected(string? expected, string? received)
    {
        Assert.False(OrchestratorAuthManager.IsExpectedState(expected, received));
    }

    [Fact]
    public void State_mismatch_message_names_the_likely_cause_and_the_way_out()
    {
        var msg = OrchestratorAuthManager.BuildStateMismatchMessage();

        Assert.Contains("Import-OrchConfig", msg);
        Assert.Contains("RedirectUrl", msg);
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

    // ---- BuildTokenExchangeFailedMessage ----

    [Fact]
    public void Token_exchange_failure_separates_the_sign_in_from_the_connection()
    {
        // The page used to show the success card here, announcing a connection that does not
        // exist. The reader has just watched the sign-in succeed, so the message has to say
        // plainly which half failed.
        var msg = OrchestratorAuthManager.BuildTokenExchangeFailedMessage(
            new InvalidOperationException("Could not connect to 127.0.0.1:10000."));

        Assert.Contains("signed in successfully", msg);
        Assert.Contains("not connected", msg);
        Assert.Contains("from PowerShell", msg);
    }

    [Fact]
    public void Token_exchange_failure_carries_the_underlying_error()
    {
        // Which is where the proxy annotation lives, and the reader is standing in front of it.
        var msg = OrchestratorAuthManager.BuildTokenExchangeFailedMessage(
            new InvalidOperationException("went through the proxy http://127.0.0.1:10000/"));

        Assert.EndsWith("went through the proxy http://127.0.0.1:10000/", msg);
    }

    // ---- BuildNoticeHtml ----
    //
    // The sign-in page consumes PendingWarning instead of the console, so an empty
    // render has to be distinguishable from a populated one: the caller keys both the
    // block's visibility and whether it clears the buffer off a zero-length result.

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n\n   ")]
    public void No_pending_warning_renders_nothing(string? pending)
    {
        Assert.Equal("", OrchestratorAuthManager.BuildNoticeHtml(pending));
    }

    [Fact]
    public void Each_notice_becomes_its_own_list_item()
    {
        // Producers concatenate with "\n\n" (AppendPendingWarning); the console drain
        // splits on the same separator, and the page has to match it or two advisories
        // run together into one sentence.
        var html = OrchestratorAuthManager.BuildNoticeHtml("First notice.\n\nSecond notice.");

        Assert.Equal("<ul><li>First notice.</li><li>Second notice.</li></ul>", html);
    }

    [Fact]
    public void Warning_text_is_html_encoded()
    {
        // The text is composed in C# but embeds server- and config-supplied values
        // (drive names, URLs); it must never reach the page as markup.
        var html = OrchestratorAuthManager.BuildNoticeHtml("<script>alert('x')</script> & co");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp; co", html);
    }

    [Fact]
    public void Blank_segments_do_not_produce_empty_list_items()
    {
        var html = OrchestratorAuthManager.BuildNoticeHtml("Only one.\n\n   \n\nAnd another.");

        Assert.Equal("<ul><li>Only one.</li><li>And another.</li></ul>", html);
    }

    [Fact]
    public void Bare_urls_become_links()
    {
        var html = OrchestratorAuthManager.BuildNoticeHtml("Go to https://cloud.uipath.com/acme now.");

        Assert.Contains("<a href=\"https://cloud.uipath.com/acme\"", html);
        Assert.Contains(">https://cloud.uipath.com/acme</a>", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
    }

    [Fact]
    public void Sentence_punctuation_stays_outside_the_link()
    {
        // A trailing period swallowed into the href produces a 404 on click.
        var html = OrchestratorAuthManager.BuildNoticeHtml("See https://docs.example/page. Done");

        Assert.Contains("href=\"https://docs.example/page\"", html);
        Assert.Contains("</a>. Done", html);
    }

    [Fact]
    public void A_trailing_labelled_url_becomes_a_link_that_hides_the_url()
    {
        // The web banner's closing "Learn more". The console needs the bare URL because it has
        // nothing to click; the page should show the label alone.
        var html = OrchestratorAuthManager.BuildNoticeHtml("Body text. Learn more: https://docs.example/accounts");

        Assert.Contains("<a href=\"https://docs.example/accounts\"", html);
        Assert.Contains(">Learn more</a>", html);
        Assert.DoesNotContain(">https://docs.example/accounts</a>", html);
        Assert.DoesNotContain("Learn more:", html);
    }

    [Fact]
    public void A_mid_sentence_url_still_shows_its_url()
    {
        // Only the trailing label collapses; the organization URL is meant to be read, exactly
        // as the web banner displays it.
        var html = OrchestratorAuthManager.BuildNoticeHtml(
            "URL: https://cloud.uipath.com/acme in your browser. Learn more: https://docs.example/x");

        Assert.Contains(">https://cloud.uipath.com/acme</a>", html);
        Assert.Contains(">Learn more</a>", html);
    }

    [Fact]
    public void A_url_bearing_notice_is_still_encoded_before_linking()
    {
        // Linking runs on the already-encoded text, so markup in the message cannot escape
        // through the anchor it produces.
        var html = OrchestratorAuthManager.BuildNoticeHtml("<b>x</b> https://ok.example/a");

        Assert.DoesNotContain("<b>", html);
        Assert.Contains("&lt;b&gt;", html);
        Assert.Contains("href=\"https://ok.example/a\"", html);
    }

    // ---- BuildEntraIdSignInWarning ----

    // The advisory follows CurrentUICulture, so these pin the culture rather than inheriting the
    // build agent's or developer's — otherwise the English assertions below fail on a Japanese
    // Windows for the right reason and the wrong one is reported.
    private static (string Full, string ConsoleSummary) NoticeIn(string culture, string drive, string orgUrl, bool prefixFullText = true)
    {
        var original = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(culture);
            return OrchestratorAuthManager.BuildEntraIdSignInNotice(drive, orgUrl, prefixFullText);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = original;
        }
    }

    private static string AdvisoryIn(string culture, string drive, string orgUrl)
        => NoticeIn(culture, drive, orgUrl).Full;

    [Fact]
    public void The_page_form_carries_no_drive_prefix()
    {
        // The page is about one drive and names it already; "[Orch1:]" there is redundant and an
        // odd way to open a sentence.
        var (full, _) = NoticeIn("en-US", "Orch1:", "https://cloud.uipath.com/acme", prefixFullText: false);

        Assert.StartsWith("You are signed in", full);
    }

    [Fact]
    public void The_console_summary_always_names_its_drive_and_stays_short()
    {
        // It exists to be read out of context -- in a transcript, or by an agent that cannot see
        // the browser -- so it names the drive and still says what to do.
        var (full, summary) = NoticeIn("en-US", "Orch1:", "https://cloud.uipath.com/acme", prefixFullText: false);

        Assert.StartsWith("[Orch1:] ", summary);
        Assert.Contains("https://cloud.uipath.com/acme", summary);
        Assert.Contains("Import-OrchConfig", summary);
        Assert.True(summary.Length < full.Length, "the summary must be shorter than what it summarises");
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("ro-RO")]
    [InlineData("tr-TR")]
    public void Every_language_supplies_a_distinct_console_summary(string culture)
    {
        // A file that lost its %%CONSOLE%% marker falls back to the full text, which would go
        // unnoticed -- the console would simply be verbose again.
        var (full, summary) = NoticeIn(culture, "Orch1:", "https://cloud.uipath.com/acme", prefixFullText: true);

        Assert.NotEqual(full, summary);
        Assert.True(summary.Length < full.Length);
    }

    [Fact]
    public void Entra_advisory_names_the_drive_org_url_and_learn_more_link()
    {
        var msg = AdvisoryIn("en-US", "Orch1:", "https://cloud.uipath.com/acme");

        Assert.StartsWith("[Orch1:] You are signed in with a local user account.", msg);
        Assert.Contains("organization-specific URL: https://cloud.uipath.com/acme in your browser", msg);
        Assert.EndsWith("Learn more: " + OrchestratorAuthManager.EntraLearnMoreUrl, msg);
    }

    [Fact]
    public void Entra_advisory_follows_the_ui_culture()
    {
        var msg = AdvisoryIn("ja-JP", "Orch1:", "https://cloud.uipath.com/acme");

        Assert.StartsWith("[Orch1:] ローカル ユーザー アカウントでサインインしています。", msg);
        Assert.Contains("https://cloud.uipath.com/acme", msg);
        Assert.EndsWith("詳細: " + OrchestratorAuthManager.EntraLearnMoreUrl, msg);
    }

    [Fact]
    public void An_unshipped_culture_falls_back_to_english()
    {
        var msg = AdvisoryIn("sv-SE", "Orch1:", "https://cloud.uipath.com/acme");

        Assert.StartsWith("[Orch1:] You are signed in with a local user account.", msg);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("ro")]
    [InlineData("tr")]
    public void Every_shipped_language_has_an_advisory_resource(string lang)
    {
        // ResolveNotificationLang promises these seven; a missing .txt would silently fall back
        // to English for that language instead of failing anywhere visible.
        var names = typeof(OrchestratorAuthManager).Assembly.GetManifestResourceNames();

        Assert.Contains($"UiPathOrch.Resources.{lang}.EntraAdvisory.txt", names);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("ro-RO")]
    [InlineData("tr-TR")]
    public void Every_language_ends_in_a_labelled_learn_more_link(string culture)
    {
        // The page collapses a trailing "<label>: <url>" into a link labelled <label>. Each
        // translation has to keep that shape or its "Learn more" renders as a bare URL.
        var html = OrchestratorAuthManager.BuildNoticeHtml(
            AdvisoryIn(culture, "Orch1:", "https://cloud.uipath.com/acme"));

        Assert.DoesNotContain($">{OrchestratorAuthManager.EntraLearnMoreUrl}</a>", html);
        Assert.Contains($"<a href=\"{OrchestratorAuthManager.EntraLearnMoreUrl}\"", html);
        // The organization URL is meant to stay visible, exactly as the web banner shows it.
        Assert.Contains(">https://cloud.uipath.com/acme</a>", html);
    }
}
