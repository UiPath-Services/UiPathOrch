using Xunit;
using static UiPath.OrchAPI.OrchestratorAuthManager;

namespace UnitTests;

// Redaction contract for the auth-call log path (OrchestratorAuthManager.SendWithLogging).
// MaskAuthSecrets is the ONLY thing standing between a token/secret and the drive's on-disk
// log file, and its whole behaviour is the _authSecretKeys set -- so the set needs tests on
// BOTH sides:
//
//   * under-masking  -- a secret-bearing key that is missing from the set leaks verbatim.
//     That is exactly how the on-prem user/password bearer token used to reach the log: the
//     set was written from the OAuth2/OIDC vocabulary (`access_token`, `client_secret`, ...),
//     but ABP's /api/Account/Authenticate returns the token as `result`, which nothing in that
//     vocabulary covers.
//   * over-masking   -- adding keys is cheap, so the set can quietly grow until ordinary
//     diagnostic fields are redacted and the log stops being useful. The "survives" cases
//     below fail if that happens.
//
// Pure string in / string out, so no HTTP is involved (same rationale as the ParseTokens /
// AuthFlowSelection tests: this module deliberately does not mock HttpClient).
public class MaskAuthSecretsTests
{
    // The literal the masker substitutes. Hardcoded rather than referenced (_redactedValue is
    // private) so the on-disk wire format is itself pinned by these tests.
    private const string Redacted = "***REDACTED***";

    // ---------------- JSON bodies ----------------

    [Theory]
    [InlineData("result")]        // on-prem /api/Account/Authenticate  <-- the leak this locks
    [InlineData("access_token")]  // /connect/token
    [InlineData("refresh_token")]
    [InlineData("id_token")]
    [InlineData("password")]      // the Authenticate REQUEST body
    public void Json_secret_keys_are_redacted(string key)
    {
        var masked = MaskAuthSecrets($@"{{""{key}"":""s3cr3t-value""}}", "application/json");

        Assert.Contains(Redacted, masked);
        Assert.DoesNotContain("s3cr3t-value", masked);
    }

    [Theory]
    [InlineData("displayName")]
    [InlineData("targetUrl")]
    [InlineData("tenancyName")]
    [InlineData("usernameOrEmailAddress")]
    public void Json_ordinary_keys_survive(string key)
    {
        var masked = MaskAuthSecrets($@"{{""{key}"":""keep-me""}}", "application/json");

        Assert.Contains("keep-me", masked);
        Assert.DoesNotContain(Redacted, masked);
    }

    // The full ABP envelope as /api/Account/Authenticate actually returns it: the token must go,
    // and every neighbouring field must stay -- that combination is the reason `result` can be
    // added to the set at zero diagnostic cost (on this endpoint `result` is only ever the token).
    [Fact]
    public void AjaxResponse_envelope_loses_only_the_token()
    {
        const string body =
            @"{""result"":""eyJhbGciOiJIUzI1NiJ9.payload.sig"",""targetUrl"":null," +
            @"""success"":true,""error"":null,""unAuthorizedRequest"":false,""__abp"":true}";

        var masked = MaskAuthSecrets(body, "application/json");

        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", masked);
        Assert.Contains(Redacted, masked);
        // Diagnostic fields the troubleshooting flow relies on are untouched.
        Assert.Contains(@"""success"":true", masked);
        Assert.Contains(@"""unAuthorizedRequest"":false", masked);
        Assert.Contains(@"""targetUrl"":null", masked);
    }

    // The set is built with OrdinalIgnoreCase; a differently-cased key must not slip through.
    [Fact]
    public void Json_key_matching_is_case_insensitive()
    {
        var masked = MaskAuthSecrets(@"{""Result"":""token-value""}", "application/json");

        Assert.DoesNotContain("token-value", masked);
    }

    // Only string values are rewritten (the regex requires a quoted value). A null/numeric
    // `result` therefore passes through -- harmless, since a token is always a string, but
    // pinned here so the limitation is a documented property rather than a surprise.
    [Fact]
    public void Json_non_string_values_are_left_alone()
    {
        var masked = MaskAuthSecrets(@"{""result"":null}", "application/json");

        Assert.Equal(@"{""result"":null}", masked);
    }

    // ---------------- form-urlencoded bodies ----------------

    [Fact]
    public void Form_redacts_only_the_secret_parameters()
    {
        const string body =
            "grant_type=client_credentials&client_id=my-app-id&client_secret=sup3r-s3cret&scope=OR.Folders";

        var masked = MaskAuthSecrets(body, "application/x-www-form-urlencoded");

        Assert.DoesNotContain("sup3r-s3cret", masked);
        Assert.Contains($"client_secret={Redacted}", masked);
        // grant_type / client_id / scope carry no secret and are what makes a failing token
        // request diagnosable at all.
        Assert.Contains("grant_type=client_credentials", masked);
        Assert.Contains("client_id=my-app-id", masked);
        Assert.Contains("scope=OR.Folders", masked);
    }

    [Fact]
    public void Form_redacts_the_pkce_code_exchange()
    {
        const string body =
            "grant_type=authorization_code&code=abcd1234&code_verifier=verifier9876&client_id=my-app-id";

        var masked = MaskAuthSecrets(body, "application/x-www-form-urlencoded");

        Assert.DoesNotContain("abcd1234", masked);
        Assert.DoesNotContain("verifier9876", masked);
        Assert.Contains("grant_type=authorization_code", masked);
    }

    // A null content type falls through to the form/query branch (the shape
    // MaskAuthSecretsInUri relies on).
    [Fact]
    public void Null_content_type_uses_the_form_branch()
    {
        var masked = MaskAuthSecrets("code=abcd1234&state=xyz", contentType: null);

        Assert.DoesNotContain("abcd1234", masked);
        Assert.Contains("state=xyz", masked);
    }

    [Fact]
    public void Empty_content_is_returned_unchanged()
    {
        Assert.Equal("", MaskAuthSecrets("", "application/json"));
    }

    // ---------------- request URIs ----------------

    // The PKCE redirect lands on the loopback listener with the authorization code in the
    // query string; SendWithLogging masks the URI in place before building the log block.
    [Fact]
    public void Uri_query_secrets_are_redacted_and_the_path_is_preserved()
    {
        var masked = MaskAuthSecretsInUri("http://localhost:8085/?code=abcd1234&state=xyz");

        Assert.StartsWith("http://localhost:8085/?", masked);
        Assert.DoesNotContain("abcd1234", masked);
        Assert.Contains("state=xyz", masked);
    }

    [Fact]
    public void Uri_without_a_query_is_returned_unchanged()
    {
        const string uri = "https://cloud.uipath.com/identity_/connect/token";

        Assert.Equal(uri, MaskAuthSecretsInUri(uri));
    }
}
