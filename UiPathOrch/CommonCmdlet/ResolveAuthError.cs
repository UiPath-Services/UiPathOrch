using System.Management.Automation;
using System.Text;
using System.Text.Json;

namespace UiPath.PowerShell.Commands;

// Diagnoses a UiPath Identity sign-in failure from the URL the browser was
// left on. UiPathOrch's PKCE listener only ever sees "no code arrived"; the
// real reason is on the page Identity redirected the browser to. Two shapes
// occur in the wild:
//   A (login error):  https://{host}/identity_/web/login?errorCode=<code>
//                        &returnUrl=<url-encoded authorize/callback>&traceId=<id>
//   B (web error):    https://{host}/identity_/web/?errorCode=<x>
//                        &errorId=<base64 JSON envelope>
// All parsing/diagnosis lives in the pure AuthErrorUrlParser so it is
// unit-testable without a live Identity server (mirrors IdentityUrlAutoGen).
[Cmdlet(VerbsDiagnostic.Resolve, "OrchAuthError")]
[OutputType(typeof(OrchAuthErrorDiagnosis))]
public class ResolveAuthErrorCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
               ValueFromPipelineByPropertyName = true,
               HelpMessage = "The full browser URL from the failed sign-in "
                   + "(the address bar of the tab Identity left open).")]
    [ValidateNotNullOrEmpty]
    public string[] Url { get; set; } = [];

    protected override void ProcessRecord()
    {
        foreach (var u in Url)
        {
            OrchAuthErrorDiagnosis result;
            try
            {
                result = AuthErrorUrlParser.Parse(u);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ResolveAuthErrorFailed",
                    ErrorCategory.InvalidData, u));
                continue;
            }

            WriteObject(result);
        }
    }
}

public sealed class OrchAuthErrorDiagnosis
{
    public string? Url { get; set; }
    public bool IsAuthError { get; set; }
    public string? ErrorCode { get; set; }
    public string? Error { get; set; }            // base64 errorId envelope
    public string? ErrorDescription { get; set; } // base64 errorId envelope
    public string? TraceId { get; set; }          // server-side correlation id
    public string? ClientId { get; set; }         // External App / AppId in use
    public string? RedirectUri { get; set; }
    public string[]? Scopes { get; set; }
    public string? Diagnosis { get; set; }        // human, actionable
    public string? RecommendedAction { get; set; }
    public string? RawErrorId { get; set; }       // decoded errorId, verbatim

    public override string ToString()
        => IsAuthError ? $"{ErrorCode ?? Error}: {Diagnosis}" : Diagnosis ?? "";
}

public static class AuthErrorUrlParser
{
    public static OrchAuthErrorDiagnosis Parse(string url)
    {
        var r = new OrchAuthErrorDiagnosis { Url = url };

        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            r.IsAuthError = false;
            r.Diagnosis = "Not an absolute URL. Paste the full address-bar "
                + "contents from the browser tab the failed sign-in left open.";
            return r;
        }

        var q = ParseQuery(uri.Query);

        r.ErrorCode = Get(q, "errorCode");
        r.TraceId = Get(q, "traceId");

        // Shape B: base64 errorId envelope. The real UiPath Identity payload
        // nests the fields under a "Data" object (PascalCase: Error,
        // ErrorDescription, ClientId, RedirectUri, RequestId, ActivityId)
        // with a top-level "Created" ticks value. Older / OAuth-flat shapes
        // put snake_case fields at the root. GetProp searches Data first,
        // then root, across every known name variant, so both work.
        var errorId = Get(q, "errorId");
        if (!string.IsNullOrEmpty(errorId))
        {
            if (TryDecodeBase64Json(errorId, out var json, out var doc))
            {
                r.RawErrorId = json;
                using (doc)
                {
                    var root = doc!.RootElement;
                    r.Error = SanitizeServerString(
                        GetProp(root, "error", "Error"));
                    r.ErrorDescription = SanitizeServerString(GetProp(root,
                        "error_description", "errorDescription", "ErrorDescription"));
                    r.RedirectUri = SanitizeServerString(GetProp(root,
                        "redirect_uri", "redirectUri", "RedirectUri"));
                    r.ClientId = SanitizeServerString(GetProp(root,
                        "client_id", "clientId", "ClientId"));
                    // RequestId/ActivityId are Identity's server-side
                    // correlation ids (same role as the login-URL traceId).
                    r.TraceId ??= SanitizeServerString(
                        GetProp(root, "RequestId", "requestId", "request_id")
                        ?? GetProp(root, "ActivityId", "activityId"));
                }
            }
            else
            {
                r.RawErrorId = "(errorId present but not base64/JSON decodable)";
            }
        }

        // Shape A: returnUrl carries the original authorize/callback request
        // (its inner separators are percent-encoded, so the outer split keeps
        // it intact; ParseQuery decodes values, exposing client_id / scope /
        // redirect_uri without a second manual unescape).
        var returnUrl = Get(q, "returnUrl");
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var inner = ParseQuery(returnUrl);
            r.ClientId ??= Get(inner, "client_id");
            r.RedirectUri ??= Get(inner, "redirect_uri");
            var scope = Get(inner, "scope");
            if (!string.IsNullOrEmpty(scope))
            {
                r.Scopes = scope.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries);
            }
        }

        r.IsAuthError = r.ErrorCode is not null || r.Error is not null;
        Diagnose(r);
        return r;
    }

    private static void Diagnose(OrchAuthErrorDiagnosis r)
    {
        var code = r.ErrorCode?.Trim();
        var desc = r.ErrorDescription ?? string.Empty;
        var appNote = r.ClientId is not null
            ? $" (External App / AppId in use: {r.ClientId}.)"
            : string.Empty;
        var traceNote = r.TraceId is not null
            ? $" Give traceId '{r.TraceId}' to UiPath Identity for "
                + "server-side correlation."
            : string.Empty;

        if (code == "219")
        {
            // Resolve-OrchAuthError only ships from 1.4.3, so the historical
            // org-scoped Identity-URL regression (d57c287, fixed in 1.4.2)
            // cannot be the cause here — the caller already has the fix.
            // Mention it as breadcrumb for git-history searches, but lead
            // with the two causes that actually remain at runtime.
            r.Diagnosis = "Sign-in returned #219 \"user has not accepted "
                + "the invitation\". The historical Cloud Identity-URL "
                + "org-scoped regression (commit d57c287, fixed in 1.4.2) "
                + "is not the cause here — you already have that fix. "
                + "Most likely remaining causes: (a) the account is "
                + "Entra-ID-federated and the org invitation/membership "
                + "genuinely needs administrator action; or (b) the "
                + "drive's IdentityUrl is manually pinned to an "
                + "org-scoped path that re-introduces the same routing."
                + appNote;
            r.RecommendedAction = "Have an administrator confirm the "
                + "user's invitation/membership in the target org. If a "
                + "custom IdentityUrl is set in your UiPathOrch config, "
                + "verify it is host-level (e.g. "
                + "https://cloud.uipath.com/identity_) not org-scoped."
                + traceNote;
            return;
        }

        if (string.Equals(code, "invalid_request",
                StringComparison.OrdinalIgnoreCase)
            && desc.Contains("redirect", StringComparison.OrdinalIgnoreCase))
        {
            r.Diagnosis = "The redirect_uri UiPathOrch sent is not registered "
                + "on the external application"
                + (r.ClientId is not null ? $" {r.ClientId}" : string.Empty)
                + ". This is common when the config points at a shared / "
                + "pre-created External Application whose "
                + "registered Redirect URL differs from the RedirectUrl in "
                + "your UiPathOrch config.";
            r.RecommendedAction = "Either add your config's RedirectUrl to "
                + "the external app's Redirect URL list (needs admin on the "
                + "owning org), or — if you cannot edit the app — set the "
                + "UiPathOrch config 'RedirectUrl' to exactly the value the "
                + "app already accepts. UiPathOrch auto-derives its local "
                + "listener from RedirectUrl, so the latter needs no app "
                + "change and no admin rights.";
            return;
        }

        if (string.Equals(code, "invalid_scope",
                StringComparison.OrdinalIgnoreCase))
        {
            r.Diagnosis = "One or more requested OAuth scopes are not granted "
                + "to, or not valid for, this external application / Identity "
                + "version" + appNote
                + (r.Scopes is not null
                    ? $" Scopes requested: {string.Join(" ", r.Scopes)}."
                    : string.Empty);
            r.RecommendedAction = "Reconcile the 'Scope' in your UiPathOrch "
                + "config with the scopes actually granted to the external "
                + "app; remove any the app does not have.";
            return;
        }

        if (r.IsAuthError)
        {
            r.Diagnosis = $"Identity returned '{code ?? r.Error}'"
                + (string.IsNullOrEmpty(desc) ? string.Empty : $": {desc}")
                + ". UiPathOrch cannot resolve this client-side — it is "
                + "server-side at UiPath Identity or the federated IdP."
                + appNote;
            r.RecommendedAction = "Provide the full error URL"
                + (r.TraceId is not null
                    ? $" including traceId '{r.TraceId}'" : string.Empty)
                + " to UiPath Identity engineering for server-side "
                + "correlation.";
            return;
        }

        r.Diagnosis = "No errorCode or errorId found — this does not look "
            + "like a UiPath Identity error URL. Paste the full URL from the "
            + "browser tab the failed sign-in left open.";
        r.RecommendedAction = null;
    }

    // Splits a query string into decoded key/value pairs. Tolerates a leading
    // '?', a full URL or a bare "a=b&c=d", and a path prefix before '?'
    // (returnUrl decodes to "/identity_/.../callback?response_type=code&...").
    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
        {
            return map;
        }

        var qIndex = query.IndexOf('?');
        if (qIndex >= 0)
        {
            query = query[(qIndex + 1)..];
        }

        query = query.TrimStart('?');
        foreach (var pair in query.Split('&',
            StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            string key, value;
            if (eq < 0)
            {
                key = pair;
                value = string.Empty;
            }
            else
            {
                key = pair[..eq];
                value = pair[(eq + 1)..];
            }

            key = Uri.UnescapeDataString(key);
            if (!map.ContainsKey(key))
            {
                map[key] = Uri.UnescapeDataString(value);
            }
        }

        return map;
    }

    private static string? Get(Dictionary<string, string> map, string key)
        => map.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)
            ? v : null;

    // The real envelope nests fields under "Data"; OAuth-flat shapes keep
    // them at the root. Prefer Data, fall back to root.
    private static IEnumerable<JsonElement> ErrorScopes(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("Data", out var d)
                && d.ValueKind == JsonValueKind.Object)
            {
                yield return d;
            }
            else if (root.TryGetProperty("data", out var d2)
                && d2.ValueKind == JsonValueKind.Object)
            {
                yield return d2;
            }
        }

        yield return root;
    }

    // Identity's error envelope occasionally contains binary garbage in
    // string fields (observed on invalid_scope responses: RedirectUri came
    // back with U+FFFD replacement characters and arbitrary control bytes).
    // U+FFFD is the canonical UTF-8 decode-failure marker — its presence
    // means the server-side data was corrupted before being JSON-encoded.
    // Replace with a stable placeholder so the cmdlet output stays
    // readable. RawErrorId retains the verbatim payload for anyone needing
    // to inspect the raw bytes the server sent.
    private static string? SanitizeServerString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Contains('�'))
        {
            return "(corrupt — see RawErrorId for the raw payload)";
        }
        return value;
    }

    private static string? GetProp(JsonElement root, params string[] names)
    {
        foreach (var scope in ErrorScopes(root))
        {
            foreach (var name in names)
            {
                if (scope.ValueKind == JsonValueKind.Object
                    && scope.TryGetProperty(name, out var el)
                    && el.ValueKind is JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        return s;
                    }
                }
            }
        }

        return null;
    }

    // Base64URL-tolerant decode + JSON parse. Mirrors AuthManager's JWT
    // payload decode (pad to %4, '-'/'_' -> '+'/'/').
    private static bool TryDecodeBase64Json(
        string value, out string json, out JsonDocument? doc)
    {
        json = string.Empty;
        doc = null;
        try
        {
            var b = value.Replace('-', '+').Replace('_', '/');
            b = b.PadRight(b.Length + (4 - b.Length % 4) % 4, '=');
            json = Encoding.UTF8.GetString(Convert.FromBase64String(b));
            doc = JsonDocument.Parse(json);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
