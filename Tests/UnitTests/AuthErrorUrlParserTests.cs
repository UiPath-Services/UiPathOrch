using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Locks the pure auth-error URL decode + diagnosis (AuthErrorUrlParser),
// driving Resolve-OrchAuthError. The #219 case uses the *verbatim* URL the
// customer's browser was left on (slack-thread2.txt), so the parser is
// proven against real-world input, not a synthetic shape.
public class AuthErrorUrlParserTests
{
    // The exact URL captured from the customer's browser on the failed
    // Cloud sign-in (errorCode=219, returnUrl carrying the authorize
    // request, traceId for server-side correlation).
    private const string Real219Url =
        "https://cloud.uipath.com/identity_/web/login?errorCode=219&returnUrl="
        + "%2Fidentity_%2Fconnect%2Fauthorize%2Fcallback%3Fresponse_type%3Dcode"
        + "%26client_id%3D820f076b-c773-4d0f-bc97-289cb5234572%26scope%3D"
        + "OR.Administration%2520OR.Analytics%2520OR.Assets%2520OR.Audit%2520"
        + "OR.BackgroundTasks%2520OR.Execution%2520OR.Folders%2520OR.Hypervisor"
        + "%2520OR.Jobs%2520OR.License%2520OR.Machines%2520OR.ML%2520"
        + "OR.Monitoring%2520OR.Queues%2520OR.Robots%2520OR.Settings%2520"
        + "OR.Tasks%2520OR.TestDataQueues%2520OR.TestSetExecutions%2520"
        + "OR.TestSets%2520OR.TestSetSchedules%2520OR.Users%2520OR.Webhooks"
        + "%2520offline_access%26redirect_uri%3Dhttp%253A%252F%252Flocalhost"
        + "%253A8085%252FTemporary_Listen_Addresses%26code_challenge%3D"
        + "24B7NCwHzrUcw53zZXOmmWSsRgh0ncv0UIosGZW1IRk%26code_challenge_method"
        + "%3DS256&traceId=0HNLC91R48MO0%3A000015F1";

    [Fact]
    public void Real219_DecodesCodeTraceClientRedirectAndScopes()
    {
        var d = AuthErrorUrlParser.Parse(Real219Url);

        Assert.True(d.IsAuthError);
        Assert.Equal("219", d.ErrorCode);
        Assert.Equal("0HNLC91R48MO0:000015F1", d.TraceId);
        Assert.Equal("820f076b-c773-4d0f-bc97-289cb5234572", d.ClientId);
        Assert.Equal("http://localhost:8085/Temporary_Listen_Addresses",
            d.RedirectUri);
        Assert.NotNull(d.Scopes);
        Assert.Contains("OR.Administration", d.Scopes!);
        Assert.Contains("offline_access", d.Scopes!);
    }

    [Fact]
    public void Real219_DiagnosisCitesD57c287AndRecommendsAdminAndConfigCheck()
    {
        var d = AuthErrorUrlParser.Parse(Real219Url);

        Assert.Contains("219", d.Diagnosis);
        // d57c287 stays in Diagnosis as a historical breadcrumb; the fix
        // is already applied for any caller that can run this cmdlet, so
        // "upgrade to 1.4.2" is no longer the recommendation (would be
        // self-referential nonsense — this cmdlet first shipped in 1.4.3).
        Assert.Contains("d57c287", d.Diagnosis);
        // RecommendedAction now leads with the two causes that actually
        // remain at runtime: federated invitation and manual IdentityUrl pin.
        Assert.Contains("administrator", d.RecommendedAction);
        Assert.Contains("IdentityUrl", d.RecommendedAction);
        // traceId must be surfaced for the federated / IdP-side path.
        Assert.Contains("0HNLC91R48MO0:000015F1", d.RecommendedAction);
    }

    private static string B64(string json)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    [Fact]
    public void InvalidRedirectUri_FromBase64ErrorId_DiagnosesAppMismatch()
    {
        // Cosmin's shape: errorCode=invalid_request with the detail inside a
        // base64 errorId envelope (visible part only says invalid_request).
        var json =
            "{\"error\":\"invalid_request\","
            + "\"error_description\":\"Invalid redirect_uri\","
            + "\"redirect_uri\":null,"
            + "\"client_id\":\"1b75a734-a1f2-48ee-b10e-b20998ec1692\"}";
        var url =
            "https://cloud.uipath.com/identity_/web/?errorCode=invalid_request"
            + "&errorId=" + B64(json);

        var d = AuthErrorUrlParser.Parse(url);

        Assert.True(d.IsAuthError);
        Assert.Equal("invalid_request", d.Error);
        Assert.Equal("Invalid redirect_uri", d.ErrorDescription);
        Assert.Equal("1b75a734-a1f2-48ee-b10e-b20998ec1692", d.ClientId);
        Assert.Contains("not registered", d.Diagnosis);
        Assert.Contains("RedirectUrl", d.RecommendedAction);
        Assert.Equal(json, d.RawErrorId);
    }

    // The verbatim base64 errorId from Cosmin's real failed sign-in. The
    // UiPath Identity envelope nests fields under "Data" (PascalCase) with a
    // top-level "Created" — the shape the flat synthetic fixture above did
    // NOT capture, which regressed the live diagnosis to generic.
    private const string RealCosminErrorId =
        "eyJDcmVhdGVkIjo2MzkxNDc3NTMxMzg0Mjg1NjQsIkRhdGEiOnsiRGlzcGxheU1vZGUiOm51bGwsIlVpTG9jYWxlcyI6bnVsbCwiRXJyb3IiOiJpbnZhbGlkX3JlcXVlc3QiLCJFcnJvckRlc2NyaXB0aW9uIjoiSW52YWxpZCByZWRpcmVjdF91cmkiLCJSZXF1ZXN0SWQiOiIwSE5MSUkzSzA1TktLOjAwMDAwRDU3IiwiQWN0aXZpdHlJZCI6IjAwLTg0ZGE3ODA4YzE2YmM5ODI1ZTRjMjk1ZWViZmJiODZiLTY2OTVlNzdlNzQ5NTg0YjEtMDAiLCJSZWRpcmVjdFVyaSI6bnVsbCwiUmVzcG9uc2VNb2RlIjpudWxsLCJDbGllbnRJZCI6IjFiNzVhNzM0LWExZjItNDhlZS1iMTBlLWIyMDk5OGVjMTY5MiJ9fQ";

    [Fact]
    public void RealCosminErrorId_NestedUnderData_IsFullyDiagnosed()
    {
        var url =
            "https://cloud.uipath.com/identity_/web/?errorCode=invalid_request"
            + "&errorId=" + RealCosminErrorId;

        var d = AuthErrorUrlParser.Parse(url);

        Assert.True(d.IsAuthError);
        Assert.Equal("invalid_request", d.ErrorCode);
        Assert.Equal("invalid_request", d.Error);
        Assert.Equal("Invalid redirect_uri", d.ErrorDescription);
        Assert.Equal("1b75a734-a1f2-48ee-b10e-b20998ec1692", d.ClientId);
        // RequestId surfaces as the correlation id when no query traceId.
        Assert.Equal("0HNLII3K05NKK:00000D57", d.TraceId);
        Assert.Contains("not registered", d.Diagnosis);
        Assert.Contains("RedirectUrl", d.RecommendedAction);
    }

    // Verbatim invalid_scope errorId observed in the wild — Identity's
    // RedirectUri field came back as binary garbage (U+FFFD replacement
    // chars + control bytes; server-side data corruption). The sanitizer
    // must replace it so the cmdlet output stays readable; RawErrorId
    // preserves the raw payload for inspection.
    private const string RealInvalidScopeErrorId =
        "eyJDcmVhdGVkIjo2MzkxNDg1MTgwNTgxODk2MjksIkRhdGEiOnsiRGlzcGxheU1vZGUiOm51bGwsIlVpTG9jYWxlcyI6bnVsbCwiRXJyb3IiOiJpbnZhbGlkX3Njb3BlIiwiRXJyb3JEZXNjcmlwdGlvbiI6IkludmFsaWQgc2NvcGUiLCJSZXF1ZXN0SWQiOiIwSE5MSUc5QkdKMkVLOjAwMDAwNTY2IiwiQWN0aXZpdHlJZCI6IjAwLTgzNTc0Mzc4ZjliZGNiYWUwYjg1OTJhYmUxN2IzODQ0LWM4MGUwOGIwYjg0ODVlMTctMDAiLCJSZWRpcmVjdFVyaSI6Ilx0XHVGRkZEXHVGRkZEXHVEODEyXHVERDAwXHUwMDExXHUwNzg3P01cdUZGRkRaXHVGRkZES1x1RkZGRFx1RkZGRFx1MDA2MCRcdUZGRkRcdUZGRkRYXHVGRkZEXHUwMDE4XHVGRkZEflx1RkZGRFx1MDAxRWMoUHdcdUZGRkRcdTAwM0NsXHUwMDBGXHUwMUI4XHVGRkZEXHVGRkZEXHUwMDBFXHVGRkZEMllmRVRtXHUwMDdGXHUwMDFBXHUwMDI3XHUwMDYwZFx1RkZGRFx1MDAxOFx1OEY3MnpcdTAwMTJcdUZGRkRcdUZGRkRcdTAwMkJLXHVGRkZEXHVGRkZEXHVGRkZETWJcdUZGRkRcdTAwMUVcdTAwMDZ0XHVGRkZEXHUwMDAxXHUwM0Y5JEZcdUZGRkRfXHVGRkZEaVx1MDAwMlx1RkZGRFx1RkZGRFx1MDAwMVx1RkZGRGJPXHVGRkZEXHVGRkZEXHVGRkZEaVRcdUZGRkRcdTA3NERcdUZGRkR2XHUwMDEzXHVGRkZEPVxuXHVGRkZEY3hcdUZGRkRcdUZGRkQzfm9cdTAwMURcdUZGRkQzXHUwMDdGKS16eVx1RkZGRFx1RkZGRFx1RkZGRFx1RkZGRFx1RkZGRFx1RkZGRFx1MDAyNlx1RkZGRCogXHVGRkZEa2NcdUZGRkRfXHUwMDE2XHVGRkZEXHVGRkZEUFx1MDAwMlx1RkZGRH5cdTA0MTdcblx1MDQzMmM2XG5fXHUwMDBGXHVGRkZEXHUwMDExXHVGRkZEXHVGRkZEKFx1RkZGRFx1RkZGRFx1RkZGRFx1RkZGRGVcdUZGRkRcdUZGRkRcdTAxQ0RFXHVGRkZEXHUwMDE3XHVGRkZEXHVGRkZEXHUwMDFGYiNAeHVcdUZGRkRoP1x1RkZGRDdpXHVGRkZEXHVGRkZEIywgXHUwNzIyXHVGRkZEIiwiUmVzcG9uc2VNb2RlIjoicXVlcnkiLCJDbGllbnRJZCI6ImFlOTc1ZWVhLTI2ZjEtNDI4OS1hOTQyLTBmM2MxMWNhZDc2MCJ9fQ";

    [Fact]
    public void CorruptRedirectUri_InRealInvalidScopePayload_IsSanitized()
    {
        var url =
            "https://cloud.uipath.com/identity_/web/?errorCode=invalid_scope"
            + "&errorId=" + RealInvalidScopeErrorId;

        var d = AuthErrorUrlParser.Parse(url);

        Assert.True(d.IsAuthError);
        Assert.Equal("invalid_scope", d.ErrorCode);
        Assert.Equal("invalid_scope", d.Error);
        Assert.Equal("Invalid scope", d.ErrorDescription);
        Assert.Equal("ae975eea-26f1-4289-a942-0f3c11cad760", d.ClientId);
        Assert.Equal("0HNLIG9BGJ2EK:00000566", d.TraceId);
        // The corrupt RedirectUri must be replaced with a stable placeholder
        // and must not leak U+FFFD into the cmdlet output.
        Assert.NotNull(d.RedirectUri);
        Assert.Contains("corrupt", d.RedirectUri);
        Assert.Contains("RawErrorId", d.RedirectUri);
        Assert.DoesNotContain("�", d.RedirectUri);
        // RawErrorId preserves the raw payload (including the garbage) for
        // anyone inspecting what the server actually returned.
        Assert.NotNull(d.RawErrorId);
        Assert.Contains("RedirectUri", d.RawErrorId);
        // The invalid_scope diagnosis branch must still fire correctly.
        Assert.Contains("scope", d.Diagnosis, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Base64UrlVariant_NoPadding_DashUnderscore_StillDecodes()
    {
        var json = "{\"error\":\"invalid_request\","
            + "\"error_description\":\"Invalid redirect_uri\"}";
        var b64Url = B64(json)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var url =
            "https://cloud.uipath.com/identity_/web/?errorCode=invalid_request"
            + "&errorId=" + b64Url;

        var d = AuthErrorUrlParser.Parse(url);

        Assert.Equal("invalid_request", d.Error);
        Assert.Equal("Invalid redirect_uri", d.ErrorDescription);
        Assert.Contains("not registered", d.Diagnosis);
    }

    [Fact]
    public void InvalidScope_FromReturnUrl_ListsScopesAndRecommendsReconcile()
    {
        var url =
            "https://cloud.uipath.com/identity_/web/login?errorCode=invalid_scope"
            + "&returnUrl=%2Fidentity_%2Fconnect%2Fauthorize%3Fscope%3D"
            + "OR.Administration%2520DataFabric.Data.Read%26client_id%3Dabc-123";

        var d = AuthErrorUrlParser.Parse(url);

        Assert.True(d.IsAuthError);
        Assert.Equal("invalid_scope", d.ErrorCode);
        Assert.Equal("abc-123", d.ClientId);
        Assert.NotNull(d.Scopes);
        Assert.Equal(
            new[] { "OR.Administration", "DataFabric.Data.Read" }, d.Scopes);
        Assert.Contains("scope", d.Diagnosis, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Scope", d.RecommendedAction);
    }

    [Fact]
    public void NonErrorUrl_IsNotAuthError_AndExplains()
    {
        var d = AuthErrorUrlParser.Parse("https://cloud.uipath.com/myorg/mytenant");

        Assert.False(d.IsAuthError);
        Assert.Null(d.ErrorCode);
        Assert.Contains("does not look", d.Diagnosis);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData("   ")]
    public void Unparseable_IsHandledGracefully(string input)
    {
        var d = AuthErrorUrlParser.Parse(input);

        Assert.False(d.IsAuthError);
        Assert.Contains("Not an absolute URL", d.Diagnosis);
    }

    [Fact]
    public void MalformedErrorId_DoesNotThrow_AndIsNoted()
    {
        var url =
            "https://cloud.uipath.com/identity_/web/?errorCode=invalid_request"
            + "&errorId=%%%not-base64%%%";

        var d = AuthErrorUrlParser.Parse(url);

        Assert.Equal("invalid_request", d.ErrorCode);
        Assert.Contains("not base64/JSON decodable", d.RawErrorId);
        // errorCode alone still drives a server-side diagnosis.
        Assert.True(d.IsAuthError);
    }

    [Fact]
    public void RawErrorId_IsValidJson_WhenDecoded()
    {
        var json = "{\"error\":\"invalid_scope\",\"trace\":\"x\"}";
        var url = "https://h/identity_/web/?errorCode=invalid_scope&errorId="
            + B64(json);

        var d = AuthErrorUrlParser.Parse(url);

        // Round-trips: the verbatim decoded payload re-parses as JSON.
        using var doc = JsonDocument.Parse(d.RawErrorId!);
        Assert.Equal("invalid_scope",
            doc.RootElement.GetProperty("error").GetString());
    }
}
