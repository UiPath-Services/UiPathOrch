using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.OrchAPI;

internal class OrchestratorAuthManager
{
    // PKCE flow binds an HttpListener to a fixed redirect port (8085 by
    // default). Two concurrent flows in the same process would crash on
    // "address already in use". Serialize across all drives — already-
    // authenticated drives skip this path entirely, so multi-drive cmdlets
    // can still fetch in parallel for the cached-token case.
    private static readonly object _pkceLock = new();

    // One-shot per drive: write the PSDrive auth settings dump to the log
    // file the first time we hit any auth flow (or any HTTP call for the
    // PAT mode which doesn't have its own auth call). Diagnostic only;
    // requires the drive's Logging.Enabled.
    private bool _authSettingsLogged;

    private readonly HttpClient _httpClient;
    private readonly OrchDriveInfo _drive;
    internal string BaseUrl { get; }
    internal string? OnpremiseTenancy { get; }
    private readonly bool _isConfidentialApp;
    private readonly bool _isUserPassword;

    internal bool IsConfidentialApp
    {
        get { return _isConfidentialApp; }
    }

    /// Open PKCE authentication in an InPrivate browser
    internal bool UseInPrivate { get; set; }

    // volatile: these are read lock-free from threads other than the one that
    // wrote them -- the AccessToken/ParseJwtPayload/partition-id readers and the
    // parallel fan-out cmdlets -- while the writers hold the session's _authLock.
    // volatile gives the readers acquire semantics so they cannot observe a
    // torn/stale reference for a cached token shared across drives.
    private volatile string? _access_token;
    private volatile string? _refresh_token;

    // Lifetime (seconds) reported by the last token response's `expires_in`.
    // 0 when unknown — PAT and user/password flows never call GetAccessToken, so
    // the session falls back to its conservative 1h assumption for those modes.
    private int _expiresInSeconds;
    internal int ExpiresInSeconds => _expiresInSeconds;

    internal bool IsAuthenticated => !string.IsNullOrEmpty(_access_token);

    internal string? AccessToken => _access_token;

    public OrchestratorAuthManager(OrchDriveInfo drive, HttpClient httpClient)
    {
        _httpClient = httpClient;
        this._drive = drive;

        _isConfidentialApp = !string.IsNullOrEmpty(_drive._psDrive.AppSecret);
        _isUserPassword = !string.IsNullOrEmpty(_drive._psDrive.Password);

        // Cloud: Root = "https://cloud.uipath.com/{org}/{tenant}"
        //   → strip tenant, keep org: "https://cloud.uipath.com/{org}"
        //   Identity API now requires /{org}/identity_/ prefix
        var rootTrimmed = _drive._psDrive.Root!.TrimEnd('/');
        BaseUrl = drive._psDrive.IsCloud
            ? rootTrimmed[..rootTrimmed.LastIndexOf('/')]
            : rootTrimmed;

        if (!drive._psDrive.IsCloud) // On-premises: remove tenant path from BaseUrl
        {
            // 1. Empty check
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new InvalidOperationException("The provided URL is null or empty.");
            }

            // 2. Attempt to parse as a Uri
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("The provided URL is not a valid absolute URI.");
            }

            // 3. Remove trailing slash from the absolute path and split by '/'
            var path = uri.AbsolutePath.TrimEnd('/'); // "/" → ""
            var segments = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

            // 4. If the domain contains "uipath.com", the path must include a tenancy
            if (uri.Host.Contains("uipath.com", StringComparison.OrdinalIgnoreCase) && segments.Length == 0)
            {
                throw new InvalidOperationException(
                    "For domains containing 'uipath.com', the URL must be in the format 'https://domain/org/tenancy'."
                );
            }

            if (segments.Length == 0)
            {
                // Case 1: No tenancy specified (and domain is not uipath.com)
                OnpremiseTenancy = string.Empty;
                BaseUrl = uri.GetLeftPart(UriPartial.Authority); // e.g., "https://orchestrator.local"
            }
            else
            {
                // Case 2: Tenancy is specified
                OnpremiseTenancy = segments.Last();

                // Reconstruct the remaining path without the tenancy
                var remainingSegments = segments.Take(segments.Length - 1);
                string newPath = remainingSegments.Any()
                    ? "/" + string.Join("/", remainingSegments) // e.g., "/folder1"
                    : "/"; // Set "/" when there is only the domain root

                // Build scheme+authority+new path using UriBuilder
                var builder = new UriBuilder(uri)
                {
                    Path = newPath
                };

                // Remove trailing slash and set as BaseUrl
                BaseUrl = builder.Uri.ToString().TrimEnd('/');
            }
        }
    }

    // ---- Auth-flow selection (extracted for unit testing; see AuthFlowSelectionTests) ----
    // The credential shape on the PSDrive determines which identity flow runs. These two static
    // decisions are the single source of truth used by RequestToken / RenewAccessToken below, so
    // the routing can be exhaustively tested without driving live token endpoints (the module
    // deliberately avoids HTTP mocking -- cf. the ParseTokens / IsTokenApplied tests).
    internal enum AuthFlow
    {
        PatReapply,        // re-apply the stored Personal Access Token (no token-endpoint call)
        ClientCredentials, // confidential app: grant_type=client_credentials
        Pkce,              // interactive external app: authorization_code via the browser
        UserPassword,      // on-premises: POST /api/Account/Authenticate
        RefreshToken,      // grant_type=refresh_token (only the PKCE flow ever obtains one)
    }

    // Flow used by the INITIAL token request. Mirrors RequestToken's dispatch order: a stored PAT
    // wins, then a confidential app, then interactive PKCE, otherwise on-prem user/password.
    internal static AuthFlow SelectInitialFlow(bool hasAccessToken, bool isConfidentialApp, bool isUserPassword)
    {
        if (hasAccessToken) return AuthFlow.PatReapply;
        if (isConfidentialApp) return AuthFlow.ClientCredentials;
        if (!isUserPassword) return AuthFlow.Pkce;
        return AuthFlow.UserPassword;
    }

    // Flow used to RENEW an expiring token. The refresh_token grant is valid only for the
    // interactive (PKCE) flow -- the one mode that obtains a refresh token. Confidential app, PAT,
    // and on-prem user/password have none, so they renew by re-running the initial request.
    // (1.9.1 fix: previously every non-confidential mode sent a refresh_token grant, posting
    // refresh_token=null and breaking user/password + PAT drives at the expiry fallback.)
    internal static AuthFlow SelectRenewalFlow(bool hasAccessToken, bool isConfidentialApp, bool isUserPassword, bool hasRefreshToken)
        => (isConfidentialApp || !hasRefreshToken)
            ? SelectInitialFlow(hasAccessToken, isConfidentialApp, isUserPassword)
            : AuthFlow.RefreshToken;

    public string RequestToken()
    {
        switch (SelectInitialFlow(!string.IsNullOrEmpty(_drive._psDrive.AccessToken), _isConfidentialApp, _isUserPassword))
        {
            case AuthFlow.PatReapply:
                // SelectInitialFlow only returns PatReapply when AccessToken is non-empty.
                _access_token = _drive._psDrive.AccessToken;
                return _access_token!;

            case AuthFlow.ClientCredentials:
                (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _drive._psDrive.AppId! },
                    { "client_secret", _drive._psDrive.AppSecret! },
                    { "scope", _drive._psDrive.Scope! }
                });
                return _access_token;

            case AuthFlow.Pkce:
                {
                    string codeVerifier = RandomString(80);
                    string authorizationCode = GetAuthorizationCode(codeVerifier);

                    // GetAuthorizationCode performs the token exchange inline so the success page
                    // can display the authenticated user's name. Skip the redundant exchange when
                    // it already succeeded.
                    if (string.IsNullOrEmpty(_access_token))
                    {
                        (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                        {
                            { "grant_type", "authorization_code" },
                            { "code", authorizationCode },
                            { "redirect_uri", _drive._psDrive.RedirectUrl! },
                            { "client_id", _drive._psDrive.AppId! },
                            { "code_verifier", codeVerifier }
                        });
                    }
                    return _access_token!;
                }

            default: // AuthFlow.UserPassword
                {
                    LogAuthSettings();

                    LoginModel payload = new()
                    {
                        tenancyName = OnpremiseTenancy,
                        usernameOrEmailAddress = _drive._psDrive.Username,
                        password = _drive._psDrive.Password
                    };

                    string url = BaseUrl + "/api/Account/Authenticate";
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    string strPayload = JsonSerializer.Serialize(payload);
                    request.Content = new StringContent(strPayload, Encoding.UTF8, @"application/json");

                    using var cts = new ConsoleCancelHandler();
                    using var response = SendWithLogging(request, cts.Token);
                    var body = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();

                    AjaxResponse? ajax = null;
                    try
                    {
                        ajax = JsonSerializer.Deserialize<AjaxResponse>(body);
                    }
                    catch (JsonException)
                    {
                        // Non-JSON body (e.g. a proxy's HTML error page). Don't dump it into
                        // the exception message (same PII/log rationale as GetAccessToken);
                        // the status line below carries the user-facing story.
                    }

                    if (!response.IsSuccessStatusCode || ajax?.error is not null)
                    {
                        string summary = $"Authentication failed: {(int)response.StatusCode} {response.StatusCode}";
                        if (!string.IsNullOrEmpty(ajax?.error?.message))
                            summary += $" — {ajax.error.message}";
                        throw new Exception(summary);
                    }

                    // Store the token like the other flows do, so IsAuthenticated /
                    // AccessToken / Claims diagnostics work for user-password drives.
                    // An absent `result` on a 200 stays "" — the session's SetToken /
                    // empty-token guard rejects it downstream.
                    _access_token = ajax?.result ?? "";
                    return _access_token;
                }
        }
    }

    public string? RenewAccessToken()
    {
        // The refresh_token grant is only valid for the interactive external-app
        // (PKCE) flow -- the one mode that actually returns a refresh token.
        // Confidential app (client_credentials), PAT, and on-prem user/password
        // have no refresh token; renewing them with a refresh_token grant sends
        // refresh_token=null and fails (on-prem user/password broke at the 1h
        // expiry fallback this way). Renew those by re-running the initial token
        // request: RequestToken re-applies a PAT, re-runs client_credentials, or
        // re-authenticates user/password against /api/Account/Authenticate.
        // Keep the confidential-app branch explicit so it always re-requests via
        // client_credentials regardless of whether a refresh token is present.
        if (SelectRenewalFlow(
                !string.IsNullOrEmpty(_drive._psDrive.AccessToken),
                _isConfidentialApp, _isUserPassword,
                !string.IsNullOrEmpty(_refresh_token)) != AuthFlow.RefreshToken)
        {
            return RequestToken();
        }

        (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", _drive._psDrive.AppId! },
            { "refresh_token", _refresh_token! }
        });

        return _access_token;
    }

    private (string access_token, string refresh_token) GetAccessToken(Dictionary<string, string> postData)
    {
        // Confidential App's client_credentials and refresh_token flows both
        // funnel through here; PKCE's code → token exchange also reuses this
        // method via the listener task. Cover the dump on all three by
        // logging at the entry of GetAccessToken — LogAuthSettings is
        // one-shot so the duplicate calls from PKCE / Confidential App
        // paths are harmless.
        LogAuthSettings();

        string endPoint;

        if (!string.IsNullOrEmpty(_drive._psDrive.IdentityUrl))
        {
            endPoint = _drive._psDrive.IdentityUrl + "/connect/token";
        }
        else
        {
            // Cloud and AS use the /identity_ suffix; on-prem uses /identity.
            endPoint = _drive._psDrive.IsCloud
                ? BaseUrl + "/identity_/connect/token"
                : BaseUrl + "/identity/connect/token";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endPoint)
        {
            Content = new FormUrlEncodedContent(postData)
        };

        using var cts = new ConsoleCancelHandler();
        using HttpResponseMessage response = SendWithLogging(request, cts.Token);

        string body = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            // Don't dump the raw response body into the exception message — it can land in
            // Start-Transcript / CI logs and may carry PII or short-lived tokens. The standard
            // OAuth2 error envelope is safe to display; fall back to status code otherwise.
            string summary = $"Token request failed: {(int)response.StatusCode} {response.StatusCode}";
            try
            {
                using var errDoc = JsonDocument.Parse(body);
                var errRoot = errDoc.RootElement;
                if (errRoot.TryGetProperty("error", out var err))
                {
                    summary += $" — {err.GetString()}";
                    if (errRoot.TryGetProperty("error_description", out var desc))
                        summary += $": {desc.GetString()}";
                }
            }
            catch (Exception ex)
            {
                // body wasn't JSON; status code alone is enough for the user-facing message.
                System.Diagnostics.Debug.WriteLine($"Token-error body was not JSON: {ex.GetType().Name}: {ex.Message}");
            }
            throw new Exception(summary);
        }

        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        var (access_token, refresh_token) = ParseTokens(root);

        // Capture the IdP-reported lifetime so the session can set a real expiry
        // instead of assuming 1h. Absent/zero → session keeps its 1h fallback.
        _expiresInSeconds = ParseExpiresInSeconds(root);

        return (access_token, refresh_token);
    }

    // Reads the access/refresh tokens from a token response body. Absent fields
    // yield "" rather than throwing (mirroring the tolerance applied to
    // expires_in) — a 200 response missing access_token is reported as an empty
    // token, which the session's SetToken guard then refuses to apply (instead of
    // pinning a stale Bearer header behind a fresh expiry). Pure / static so this
    // precondition of the stale-token guard is unit-testable.
    internal static (string accessToken, string refreshToken) ParseTokens(JsonElement root)
    {
        string accessToken = root.TryGetProperty("access_token", out JsonElement a) ? a.GetString() ?? "" : "";
        string refreshToken = root.TryGetProperty("refresh_token", out JsonElement r) ? r.GetString() ?? "" : "";
        return (accessToken, refreshToken);
    }

    // Reads the OAuth `expires_in` (seconds) from a token response body. RFC 6749
    // specifies a JSON number, but a quoted numeric string ("3600") is also
    // accepted so a non-conforming IdP's shorter-than-1h lifetime is honored
    // rather than discarded into the 1h fallback. Returns 0 when the value is
    // absent, non-numeric, or non-positive — which tells the session to use its
    // conservative 1h fallback. Pure / static so it can be unit-tested without an
    // HTTP round trip.
    internal static int ParseExpiresInSeconds(JsonElement root)
    {
        if (!root.TryGetProperty("expires_in", out JsonElement el))
            return 0;

        // TryGetInt32 THROWS on a non-number element, so branch on ValueKind and
        // parse a quoted value explicitly (invariant — `expires_in` is digits only).
        int seconds = el.ValueKind switch
        {
            JsonValueKind.Number when el.TryGetInt32(out int n) => n,
            JsonValueKind.String when int.TryParse(el.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int n) => n,
            _ => 0,
        };

        return seconds > 0 ? seconds : 0;
    }

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return RandomNumberGenerator.GetString(chars, length);
    }

    private static string LoadBotImageRandomly()
    {
        // List of embedded resource names
        var resourceNames = new List<string>
        {
            "autopilot.png",
            "caring.png",
            "flying.png",
            "listening.png",
            "processing.png",
            "receiving.png",
            "recording.png",
            "searching.png"
        };

        // Randomly select one
        var random = new Random();
        int index = random.Next(resourceNames.Count);
        string selectedResource = resourceNames[index];

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UiPathOrch.bot." + selectedResource);
        if (stream is null) return "";

        // Convert the stream to a byte array
        using var memoryStream = new MemoryStream();

        stream.CopyTo(memoryStream);
        byte[] imageBytes = memoryStream.ToArray();

        // Encode to Base64
        return Convert.ToBase64String(imageBytes);
    }

    // The "left the browser on an error page -> use Resolve-OrchAuthError" hint,
    // shared by every PKCE failure path below so the guidance stays consistent.
    private const string PkceErrorPageHint =
        "(e.g. An unknown error has occurred. (#200)), "
        + "copy that page's full URL from the address bar, "
        + "run `cd $HOME`, then "
        + "`Resolve-OrchAuthError '<url>'`.";

    // Compose the error surfaced when Identity calls the loopback listener back with
    // an OAuth error instead of an authorization code (RFC 6749 §4.1.2.1). Pure and
    // internal so the wording is unit-testable without a live Identity server.
    internal static string BuildOAuthCallbackErrorMessage(string error, string? description, string? errorUri)
    {
        var sb = new StringBuilder();
        sb.Append("PKCE sign-in failed: Identity returned '")
          .Append(error)
          .Append("' instead of an authorization code.");

        string? desc = description?.Trim();
        if (!string.IsNullOrEmpty(desc))
        {
            sb.Append(' ').Append(desc);
            if (!desc.EndsWith('.')) sb.Append('.');
        }

        if (string.Equals(error, "invalid_scope", StringComparison.OrdinalIgnoreCase))
        {
            // The case this path was added for: a scope list built against one
            // deployment moved to another whose Identity does not define it.
            sb.Append(" At least one requested scope is not recognized by this deployment's Identity")
              .Append(" or is not granted to this application. Scope names and availability differ")
              .Append(" between Orchestrator versions, so a list that works on one deployment does not")
              .Append(" necessarily work on another. Compare the drive's Scope against the external")
              .Append(" application's allowed scopes (Edit-OrchConfig opens the configuration file),")
              .Append(" then run Import-OrchConfig.");
        }
        else
        {
            sb.Append(" Verify the application registration, its allowed scopes and its redirect URI,")
              .Append(" then run Import-OrchConfig.");
        }

        string? uri = errorUri?.Trim();
        if (!string.IsNullOrEmpty(uri))
        {
            sb.Append(" More information: ").Append(uri).Append('.');
        }

        return sb.ToString();
    }

    // Target of the web banner's "Learn more" link.
    internal const string EntraLearnMoreUrl =
        "https://docs.uipath.com/automation-cloud/automation-cloud/latest/admin-guide/about-accounts";

    /// <summary>
    /// The Entra-ID local-user advisory, worded to match the banner Orchestrator's web UI shows
    /// for the same condition, in the same language.
    /// </summary>
    /// <remarks>
    /// The wording through the organization URL is taken from Orchestrator's own banner in each
    /// language, so a reader who has seen it in the web UI recognizes it here. What follows is
    /// ours: the banner is displayed in the browser the reader is already looking at, whereas
    /// this one has to say how to come back to PowerShell. The "[drive:]" prefix disambiguates
    /// which drive the notice is about when several are mounted.
    ///
    /// The trailing "&lt;label&gt;: {2}" shape is deliberate — BuildNoticeHtml collapses it into a
    /// link labelled with that language's "Learn more", the way the banner ends, while the
    /// console keeps the URL as text because there is nothing to click there.
    /// </remarks>
    // Separates the two forms inside one resource file: the notice above, the console summary
    // below. One file per language rather than two, and a file that loses the marker degrades to
    // "no summary", which the caller reads as "print the full text".
    private const string ConsoleFormMarker = "%%CONSOLE%%";

    /// <param name="prefixFullText">
    /// Whether the full notice opens with "[drive:]". The sign-in page is about one drive and
    /// already names it, so the prefix there is redundant and an odd way to begin a sentence;
    /// the console, where several drives' notices interleave, needs it. The summary always
    /// carries it — that form exists to be read out of context, in a transcript or by an agent.
    /// </param>
    internal static (string Full, string ConsoleSummary) BuildEntraIdSignInNotice(
        string driveNameColon, string orgUrl, bool prefixFullText)
    {
        string lang = ResolveNotificationLang(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        string template = ReadTextResource($"UiPathOrch.Resources.{lang}.EntraAdvisory.txt")
            ?? ReadTextResource("UiPathOrch.Resources.en.EntraAdvisory.txt")
            ?? "You are signed in with a local user account. Sign in through {0} instead. Learn more: {1}";

        string[] parts = template.Split(ConsoleFormMarker, 2, StringSplitOptions.None);
        string prefix = $"[{driveNameColon}] ";

        string full = (prefixFullText ? prefix : "") + string.Format(parts[0].Trim(), orgUrl, EntraLearnMoreUrl);
        string summary = parts.Length > 1 && parts[1].Trim().Length > 0
            ? prefix + string.Format(parts[1].Trim(), orgUrl, EntraLearnMoreUrl)
            : prefix + string.Format(parts[0].Trim(), orgUrl, EntraLearnMoreUrl);

        return (full, summary);
    }

    // Embedded UTF-8 text resource, or null when absent. Null rather than throwing: a missing
    // resource must degrade the advisory, never break the sign-in that carries it.
    private static string? ReadTextResource(string resourceName)
    {
        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream is null) return null;
            using StreamReader reader = new(stream);
            return reader.ReadToEnd().TrimEnd('\r', '\n');
        }
        catch
        {
            return null;
        }
    }

    // URLs in a notice become links: the Entra advisory's whole point is to send the reader
    // somewhere, and the page is the one surface where that can be a click instead of a
    // copy-paste. Two shapes, matched in one pass so neither can re-process the other's output:
    //
    //   "<label>: <url>" at the very END  -> a link LABELLED <label>, with the URL not shown.
    //       This is the web banner's trailing "Learn more" link. It has to carry its URL as
    //       text on the console, where there is nothing to click; on the page that URL is
    //       noise the reader was never meant to read.
    //   any other URL                     -> a link showing the URL, as the web banner does
    //       for the organization URL mid-sentence.
    //
    // Applied AFTER encoding, so the pattern only ever sees text that is already safe to emit,
    // and trailing sentence punctuation is kept outside the anchor -- a period swallowed into
    // the href 404s on click.
    // The label excludes sentence terminators (and cannot start with whitespace), which is what
    // keeps it to the trailing phrase: without that bound it is greedy back to the start of the
    // notice, and the whole message -- not "Learn more" -- becomes the link text. The set spans
    // the shipped languages, so Japanese '。' bounds the label exactly as '.' does elsewhere.
    private static readonly System.Text.RegularExpressions.Regex NoticeUrlPattern =
        new(@"(?<label>[^:<>\s.。！？!?][^:<>.。！？!?]{0,40}): (?<labelled>https?://[^\s<]+)$|(?<bare>https?://[^\s<]+)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string LinkifyEncoded(string encoded) =>
        NoticeUrlPattern.Replace(encoded, m =>
        {
            if (m.Groups["labelled"].Success)
            {
                string labelledUrl = m.Groups["labelled"].Value.TrimEnd('.', ',', ';', ':');
                // Trimmed because French writes "En savoir plus : <url>" -- the space before the
                // colon belongs to the typography, not to the link text.
                return Anchor(labelledUrl, m.Groups["label"].Value.Trim())
                    + m.Groups["labelled"].Value[labelledUrl.Length..];
            }

            string url = m.Groups["bare"].Value.TrimEnd('.', ',', ';', ':');
            return Anchor(url, url) + m.Groups["bare"].Value[url.Length..];
        });

    private static string Anchor(string href, string text) =>
        $"<a href=\"{href}\" target=\"_blank\" rel=\"noopener noreferrer\">{text}</a>";

    /// <summary>
    /// Renders the queued advisories for the sign-in page as a list, one item per notice.
    /// </summary>
    /// <remarks>
    /// When a browser is part of the flow it is where the user is already looking, and it
    /// is where these notices are actionable — so the page is a real consumer of
    /// PendingWarning: it clears the buffer once the response is safely written, and what
    /// it showed is not repeated on the console. The console drain in
    /// OrchestratorPSCmdlet.BeginProcessing remains the channel for everything the page
    /// cannot carry: drives that never open a browser (PAT, confidential app), advisories
    /// queued after the page was written, and a page that failed to render.
    /// </remarks>
    internal static string BuildNoticeHtml(string? pendingWarning)
    {
        if (string.IsNullOrWhiteSpace(pendingWarning)) return "";

        // Producers concatenate with "\n\n"; mirror the console drain's split so the two
        // surfaces agree on where one notice ends and the next begins.
        var sb = new StringBuilder();
        foreach (var segment in pendingWarning.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            string text = segment.Trim();
            if (text.Length == 0) continue;
            sb.Append("<li>").Append(LinkifyEncoded(WebUtility.HtmlEncode(text))).Append("</li>");
        }

        // All segments blank: return empty so the caller hides the block rather than
        // rendering an empty list, and leaves the buffer for the console.
        return sb.Length == 0 ? "" : "<ul>" + sb.ToString() + "</ul>";
    }

    /// <summary>
    /// Decides and queues the Entra-ID local-user advisory while the browser is still open, so it
    /// reaches the sign-in page instead of a console line the reader meets much later.
    /// </summary>
    /// <remarks>
    /// The same decision is otherwise taken during folder enumeration, which puts it two hops
    /// after sign-in: the probe runs only there, and the queue is drained only by the NEXT
    /// cmdlet — so a session that signs in and runs `Get-Orch*` never sees it at all. Everything
    /// the decision needs except the organization's auth setting is already in the JWT the
    /// exchange just produced.
    ///
    /// Best-effort by construction. On any failure or timeout the gate is left un-latched, which
    /// DecideEntraAdvisory already models as "inconclusive — retry", so the enumeration path
    /// picks it up exactly as it does today. An advisory is never worth a hung browser tab.
    /// </remarks>
    private async Task TryQueueEntraAdvisoryAsync(CancellationToken ct)
    {
        try
        {
            if (_drive.OrchAPISession.EntraIdWarningChecked) return;

            var kind = ClassifyEntraUserKind(_access_token);

            // Resolve only what the decision needs: the partition id and the org auth setting
            // matter for a local user and nobody else, and the setting costs a round trip.
            string? partitionGlobalId = null;
            string? authenticationSettingType = null;
            if (kind == EntraUserKind.LocalUser)
            {
                partitionGlobalId = GetPartitionGlobalIdFromJwt();
                if (!string.IsNullOrEmpty(partitionGlobalId))
                {
                    authenticationSettingType = await _drive.OrchAPISession.ProbeAuthenticationSettingTypeAsync(
                        partitionGlobalId!, _access_token!, TimeSpan.FromSeconds(3), ct);
                }
            }

            var decision = DecideEntraAdvisory(
                kind,
                partitionKnown: !string.IsNullOrEmpty(partitionGlobalId),
                authSettingFetched: authenticationSettingType is not null,
                authenticationSettingType);

            if (decision.Latch) _drive.OrchAPISession.EntraIdWarningChecked = true;
            if (decision.QueueWarning)
            {
                // Bound for the sign-in page, which is about this one drive and already names it.
                var (full, summary) = BuildEntraIdSignInNotice(_drive.NameColon, BaseUrl, prefixFullText: false);
                _drive.OrchAPISession.AppendPendingWarning(full, summary);
            }
        }
        catch { } // Advisory only — never let it disturb the sign-in it annotates.
    }

    // Render a minimal failure page so the browser tab does not sit on a blank
    // response after an error callback. Deliberately not one of the localized
    // MountSuccessNotification resources: this path shows a server-supplied
    // message that has to appear verbatim, so a per-language template would add
    // seven files without changing what the user actually reads.
    /// <summary>
    /// What the browser is told when the sign-in itself worked but the token exchange did not.
    /// </summary>
    /// <remarks>
    /// The distinction matters and the page used to hide it: a failed exchange left the success
    /// card on screen, announcing a connection that does not exist, with only the absent user-name
    /// row as a hint. That page is the one signal an operator has at that moment, and it was
    /// pointing the wrong way -- a customer reported "the browser says it connected" while
    /// PowerShell showed a connection error. The underlying message is included because it is the
    /// specific one (it carries the proxy annotation, for instance), and the reader is standing in
    /// front of it.
    /// </remarks>
    internal static string BuildTokenExchangeFailedMessage(Exception failure) =>
        "You signed in successfully, but UiPathOrch could not exchange that sign-in for a token, "
        + "so the drive is not connected. The sign-in was fine; this step runs from PowerShell, "
        + "not from the browser, so it is that side of the connection that failed: "
        + failure.Message;

    private static async Task WriteCallbackErrorPageAsync(
        HttpListenerContext context, string message, CancellationToken ct,
        string heading = "Sign-in failed", string? configSnippet = null)
    {
        // A code block is the part of the remedy the console cannot carry, which is why it is
        // built here rather than folded into the message both surfaces share.
        string snippetHtml = string.IsNullOrEmpty(configSnippet)
            ? ""
            : "<p>Add <code>\"UseProxy\": false</code> to the <code>\"Proxy\"</code> block in your "
              + "configuration file (<code>Edit-OrchConfig</code> opens it). Leave the rest as it is:</p>"
              + "<pre style=\"background:#f5f6f8;border:1px solid #e1e4e8;border-radius:4px;"
              + "padding:10px 12px;overflow-x:auto\">" + WebUtility.HtmlEncode(configSnippet) + "</pre>";

        string html =
            "<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
            + "<title>UiPath Orchestrator sign-in failed</title></head>"
            + "<body style=\"font-family:Segoe UI,Arial,sans-serif;margin:2rem;max-width:44rem\">"
            + "<h2>" + WebUtility.HtmlEncode(heading) + "</h2><p>" + WebUtility.HtmlEncode(message) + "</p>"
            + snippetHtml
            + "<p>You can close this tab and return to PowerShell.</p></body></html>";

        byte[] buffer = Encoding.UTF8.GetBytes(html);
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentLength64 = buffer.Length;
        context.Response.ContentType = "text/html; charset=UTF-8";
        context.Response.Headers["Connection"] = "close";

        await using var output = context.Response.OutputStream;
        await output.WriteAsync(buffer, ct);
        await output.FlushAsync(ct);
        context.Response.Close();
    }

    // Builds the Identity authorize URL. Extracted as a pure, testable function:
    // endpoint selection (explicit IdentityUrl / Cloud common path + acr_values /
    // on-prem) and the scope URL-encoding the v1.9.2 macOS fix depends on (a raw
    // space in scope truncates the launched URL and drops redirect_uri).
    internal static string BuildAuthorizeUrl(
        string? identityUrl, bool isCloud, string baseUrl,
        string? scope, string? appId, string? redirectUrl,
        bool useInPrivate, string? codeVerifier, string? state = null)
    {
        string endPoint;
        string acrValues = "";
        if (!string.IsNullOrEmpty(identityUrl))
        {
            endPoint = identityUrl + "/connect/authorize";
        }
        else if (isCloud)
        {
            // Cloud: The authorize endpoint uses a common path without the org prefix,
            // and specifies the organization name via acr_values (to accommodate UiPath Identity spec changes).
            var baseUri = new Uri(baseUrl);
            string orgName = baseUri.AbsolutePath.Trim('/');
            endPoint = $"{baseUri.Scheme}://{baseUri.Host}/identity_/connect/authorize";
            acrValues = useInPrivate
                ? "" // InPrivate: omit acr_values to display the authentication provider selection screen
                : $"&acr_values=tenantName:{orgName}";
        }
        else
        {
            endPoint = baseUrl + "/identity/connect/authorize";
        }

        // The scope value contains literal spaces (space-delimited scopes plus
        // " offline_access"), so it must be URL-encoded like redirect_uri.
        // Windows browsers normalized raw spaces to %20, masking the omission;
        // on macOS the launch path splits the URL at the first raw space, so
        // everything after the scope value (including redirect_uri) was lost
        // and Identity rejected the request with "Invalid redirect_uri".
        string encodedScope = WebUtility.UrlEncode($"{scope} offline_access");

        // RFC 8252 §8.9: the loopback redirect has to carry `state`, because PKCE alone does not
        // close the injection direction. PKCE stops an attacker from USING a code stolen from us;
        // it does nothing about an attacker's code being delivered INTO our listener, which ends
        // with the drive authenticated as them. `state` is what ties the callback back to the
        // request we made. Identity echoes it verbatim and ignores it otherwise, so this is
        // additive on every edition.
        string stateParam = string.IsNullOrEmpty(state) ? "" : $"&state={WebUtility.UrlEncode(state)}";

        return !string.IsNullOrEmpty(codeVerifier)
            ? $"{endPoint}?response_type=code&client_id={appId}&scope={encodedScope}&redirect_uri={WebUtility.UrlEncode(redirectUrl)}&code_challenge={GetHash(codeVerifier)}&code_challenge_method=S256{stateParam}{acrValues}"
            : $"{endPoint}?response_type=code&client_id={appId}&scope={encodedScope}&redirect_uri={WebUtility.UrlEncode(redirectUrl)}{stateParam}{acrValues}";
    }

    /// <summary>
    /// Whether a PKCE callback's `state` is the one this sign-in asked for.
    /// </summary>
    /// <remarks>
    /// Absent or empty on either side is a mismatch, deliberately: "no state" must never be the
    /// value that passes, or an injected callback that simply omits the parameter would sail
    /// through the check that exists to stop it. Pure and static so every branch is testable
    /// without a live endpoint.
    /// </remarks>
    internal static bool IsExpectedState(string? expected, string? received) =>
        !string.IsNullOrEmpty(expected)
        && !string.IsNullOrEmpty(received)
        && string.Equals(expected, received, StringComparison.Ordinal);

    // What the reader is told when a callback carries the wrong state. Names the realistic cause
    // rather than the alarming one: a second sign-in sharing the redirect port is far more likely
    // than an attack, and the fix differs.
    internal static string BuildStateMismatchMessage() =>
        "PKCE sign-in was refused: the browser callback did not carry the state value this "
        + "sign-in sent, so it was discarded instead of being exchanged for a token. That check is "
        + "what stops a sign-in response meant for somewhere else from connecting this drive as "
        + "someone else. Run Import-OrchConfig to try again. If it repeats, check that no other "
        + "sign-in is using the same redirect port (RedirectUrl in the configuration file, which "
        + "Edit-OrchConfig opens).";

    // The success-page language: the embedded notification HTML exists only for
    // these locales, so anything else falls back to English.
    internal static string ResolveNotificationLang(string twoLetterIsoLang)
    {
        string[] supportedLangs = ["de", "en", "fr", "ja", "ko", "ro", "tr"];
        return supportedLangs.Contains(twoLetterIsoLang) ? twoLetterIsoLang : "en";
    }

    // The Orchestrator drive plus the Du / Tm shadow drives that Import-OrchConfig
    // mounts alongside it — created when the drive's scope includes Du. / TM.
    // scopes (the same condition used to create them), and named <Name>Du /
    // <Name>Tm. Lists whichever apply, for display on the success page.
    internal static string FormatMountedDriveList(string driveColon, string scope)
    {
        string baseName = driveColon.TrimEnd(':');
        var mountedDrives = new List<string> { driveColon };
        if (scope.Contains("Du.")) mountedDrives.Add($"{baseName}Du:");
        if (scope.Contains("TM.")) mountedDrives.Add($"{baseName}Tm:");
        return string.Join(", ", mountedDrives);
    }

    // Starts the loopback HttpListener for the PKCE redirect, mapping a bind
    // failure to an actionable message (privileged port vs port-in-use).
    private HttpListener StartAuthListener()
    {
        var listener = new HttpListener();
        try
        {
            listener.Prefixes.Add(_drive._psDrive.HttpListener!);
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            // If starting the listener failed
            listener.Close();
            var uri = new Uri(_drive._psDrive.RedirectUrl!);
            string message = uri.Port <= 1024
                ? $"Failed to start the HttpListener. The port {uri.Port} specified in 'RedirectUrl' may require administrative privileges. Please ensure you have the necessary permissions or try changing this port in the configuration file, which can be opened using the Edit-OrchConfig cmdlet."
                : $"Failed to start the HttpListener. The port {uri.Port} specified in 'RedirectUrl' may be in use. Try changing this port in the configuration file, which can be opened using the Edit-OrchConfig cmdlet.";
            throw new InvalidOperationException(message, ex);
        }
        return listener;
    }

    // Opens the authorize URL in a browser. With -UseInPrivate on Windows, uses
    // Edge InPrivate + a throwaway profile for full cookie isolation; on other
    // platforms (or when Edge is absent) falls back to the default browser so
    // sign-in still completes — the isolation just can't be honored there.
    private void LaunchSignInBrowser(string authUrl)
    {
        if (UseInPrivate && OperatingSystem.IsWindows())
        {
            string edgePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft\Edge\Application\msedge.exe");
            if (!File.Exists(edgePath))
            {
                edgePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
                    @"Microsoft\Edge\Application\msedge.exe");
            }
            if (File.Exists(edgePath))
            {
                CleanUpStaleInPrivateProfiles();
                string tempProfile = Path.Combine(Path.GetTempPath(), "UiPathOrch_" + Guid.NewGuid().ToString("N")[..8]);
                Process.Start(new ProcessStartInfo(edgePath, $"--inprivate --user-data-dir=\"{tempProfile}\" \"{authUrl}\"") { UseShellExecute = false });
            }
            else
            {
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            }
        }
        else
        {
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        }
    }

    // Best-effort sweep of throwaway profiles left by earlier -UseInPrivate sign-ins:
    // Edge populates each --user-data-dir with tens of MB and nothing removes it when
    // the browser closes (Windows does not clean %TEMP% automatically). The current
    // sign-in's dir can't be deleted here — Edge may outlive the auth round-trip — so
    // each launch sweeps its predecessors instead. Skip dirs younger than an hour (a
    // concurrent sign-in's Edge may not have locked its dir yet); an in-use dir just
    // fails the delete and is picked up by a later sweep.
    private static void CleanUpStaleInPrivateProfiles()
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(Path.GetTempPath(), "UiPathOrch_*"))
            {
                try
                {
                    if (Directory.GetCreationTimeUtc(dir) < DateTime.UtcNow.AddHours(-1))
                        Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"InPrivate profile sweep skipped '{dir}': {ex.GetType().Name}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InPrivate profile sweep failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private string GetAuthorizationCode(string? codeVerifier)
    {
        // See _pkceLock declaration for the rationale. Held for the full
        // browser-auth round-trip; released as soon as the token exchange
        // completes (or fails) so the next pending drive's auth can proceed.
        lock (_pkceLock)
        {
            LogAuthSettings();

            // Fresh per attempt: a state reused across sign-ins would still accept a callback
            // captured from an earlier one.
            string expectedState = RandomString(32);

            string authUrl = BuildAuthorizeUrl(
                _drive._psDrive.IdentityUrl, _drive._psDrive.IsCloud, BaseUrl,
                _drive._psDrive.Scope, _drive._psDrive.AppId, _drive._psDrive.RedirectUrl,
                UseInPrivate, codeVerifier, expectedState);

            // Log the exact URL handed to the browser (when the drive's Logging is
            // enabled). This is the authorize request as Identity receives it, so a
            // failing interactive sign-in can be inspected without digging through
            // browser history. The URL carries no secrets -- only client_id,
            // redirect_uri, scope, and the public PKCE code challenge.
            LogAuthorizeUrl(authUrl);

            using var listener = StartAuthListener();
            LaunchSignInBrowser(authUrl);

            string? authorizationCode = null;
            Exception? capturedException = null;

            // Manage the Ctrl+C event with ConsoleCancelHandler
            using var consoleCancelHandler = new ConsoleCancelHandler();
            var cts = consoleCancelHandler.Token;

            // Start the listening in a separate task
            var listeningTask = Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening && !cts.IsCancellationRequested)
                    {
                        try
                        {
                            var context = await listener.GetContextAsync();
                            authorizationCode = context.Request.QueryString["code"];
                            if (!string.IsNullOrEmpty(authorizationCode))
                            {
                                // Before the exchange, never after: exchanging first would already
                                // have spent the code and put a token for the wrong principal in
                                // this session, which is the outcome the check exists to prevent.
                                if (!IsExpectedState(expectedState, context.Request.QueryString["state"]))
                                {
                                    authorizationCode = null;
                                    capturedException = new InvalidOperationException(BuildStateMismatchMessage());
                                    await WriteCallbackErrorPageAsync(context, capturedException.Message, cts);
                                    break;
                                }

                                // Exchange the auth code for tokens inline so we can display
                                // the authenticated user's name on the success page. The
                                // caller (RequestToken) will skip its own exchange when
                                // _access_token is already set. If exchange fails here, we
                                // continue to render the page without a username and let the
                                // caller's retry surface the error through the normal path.
                                string userName = "";
                                Exception? exchangeFailure = null;
                                if (!string.IsNullOrEmpty(codeVerifier))
                                {
                                    try
                                    {
                                        (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                                        {
                                        { "grant_type", "authorization_code" },
                                        { "code", authorizationCode },
                                        { "redirect_uri", _drive._psDrive.RedirectUrl! },
                                        { "client_id", _drive._psDrive.AppId! },
                                        { "code_verifier", codeVerifier }
                                        });

                                        try
                                        {
                                            using JsonDocument doc = ParseJwtPayload();
                                            if (doc.RootElement.TryGetProperty("preferred_username", out var puElement))
                                                userName = puElement.GetString() ?? "";
                                            else if (doc.RootElement.TryGetProperty("name", out var nameElement))
                                                userName = nameElement.GetString() ?? "";
                                        }
                                        catch (Exception ex)
                                        {
                                            // JWT unparseable; fall through to generic display.
                                            System.Diagnostics.Debug.WriteLine($"JWT parse failed: {ex.GetType().Name}: {ex.Message}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Reset so caller's retry path runs the exchange and surfaces the real error.
                                        _access_token = null;
                                        _refresh_token = null;
                                        exchangeFailure = ex;
                                    }
                                }

                                // The sign-in worked, the exchange did not, so there is no connection to
                                // announce. Rendering the success card here told the reader the opposite
                                // of what happened -- see BuildTokenExchangeFailedMessage. The caller's
                                // retry still runs and still raises the real error in PowerShell; this
                                // only stops the browser from contradicting it.
                                if (exchangeFailure is not null)
                                {
                                    string? snippet = null;
                                    try
                                    {
                                        snippet = UiPath.OrchAPI.OrchHttp.BuildDirectConnectionConfigSnippet(
                                            new Uri(_drive._psDrive.Root!), _drive._psDrive.Proxy);
                                    }
                                    catch { } // The page must render with or without the suggestion.

                                    await WriteCallbackErrorPageAsync(
                                        context,
                                        BuildTokenExchangeFailedMessage(exchangeFailure),
                                        cts,
                                        heading: "Signed in, but not connected",
                                        configSnippet: snippet);
                                    break;
                                }

                                // Send a response back to the browser
                                string lang = ResolveNotificationLang(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

                                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"UiPathOrch.Resources.{lang}.MountSuccessNotification.html");
                                using StreamReader reader = new(stream!);
                                string htmlTemplate = await reader.ReadToEndAsync();

                                bool hasUser = !string.IsNullOrEmpty(userName);
                                string userStyle = hasUser ? "" : "display:none";
                                string userEncoded = hasUser ? System.Net.WebUtility.HtmlEncode(userName) : "";

                                // Embed image and version information.
                                // Assembly.GetName().Version is always 4 parts (Major.Minor.Build.Revision),
                                // but the manifest / PSGallery version is 3-part SemVer — use ToString(3) so the
                                // rendered string and the PSGallery URL match what was actually published.
                                string versionStr = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "";

                                string mountedDrivesStr = FormatMountedDriveList(_drive.NameColon, _drive._psDrive.Scope ?? "");

                                // The queued advisories go here rather than to the console -- see
                                // BuildNoticeHtml. Decide the Entra advisory first (it is the one the
                                // reader can act on right here), then EnsureConfigWarningsEmitted, so the
                                // notices that depend only on drive configuration are present regardless
                                // of whether an HTTP call has already triggered them.
                                await TryQueueEntraAdvisoryAsync(cts);
                                _drive.OrchAPISession.EnsureConfigWarningsEmitted();
                                string noticeHtml = BuildNoticeHtml(_drive.OrchAPISession.PendingWarning);
                                string noticeStyle = noticeHtml.Length == 0 ? "display:none" : "";

                                // {6} shows or hides the block; the body is substituted afterwards so the
                                // notice markup needs no brace escaping of its own.
                                string responseString = string.Format(htmlTemplate, _drive._psDrive.Root, mountedDrivesStr, versionStr, LoadBotImageRandomly(), userStyle, userEncoded, noticeStyle)
                                    .Replace("<!--WARNINGS-->", noticeHtml);

                                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                context.Response.ContentLength64 = buffer.Length;
                                context.Response.ContentType = "text/html; charset=UTF-8";
                                context.Response.Headers["Connection"] = "close";

                                // Send the response
                                await using var output = context.Response.OutputStream;
                                await output.WriteAsync(buffer, cts);
                                await output.FlushAsync();

                                await Task.Delay(50); // The screen sometimes goes blank -- not sure if this delay helps...

                                // Explicitly close the response
                                context.Response.Close();

                                // Only now, with the page safely on the wire, is the buffer consumed. If
                                // anything above threw, the advisories are still queued and the console
                                // drain picks them up -- a notice must never be lost to a page that did
                                // not render. Downgrade rather than clear: what the page showed is not
                                // repeated in full, but a notice carrying real content leaves its one-line
                                // form behind, because the page is invisible to a transcript or an agent.
                                if (noticeHtml.Length > 0) _drive.OrchAPISession.DowngradePendingWarningAfterDisplay();

                                // Exit the loop
                                break;
                            }

                            // No `code` in the callback. Identity may have redirected
                            // back with an OAuth error instead (RFC 6749 §4.1.2.1) —
                            // invalid_scope is the one that bites when a scope list is
                            // moved between deployments. This loop used to ignore every
                            // callback without `code`, which made an error redirect
                            // indistinguishable from no redirect at all: the caller sat
                            // until the 3-minute timeout and the real reason was lost.
                            string? oauthError = context.Request.QueryString["error"];
                            if (string.IsNullOrEmpty(oauthError))
                            {
                                // Something else hit the loopback (favicon probe, stray
                                // request). Keep waiting for the real callback.
                                continue;
                            }

                            capturedException = new InvalidOperationException(
                                BuildOAuthCallbackErrorMessage(
                                    oauthError,
                                    context.Request.QueryString["error_description"],
                                    context.Request.QueryString["error_uri"]));

                            await WriteCallbackErrorPageAsync(context, capturedException.Message, cts);

                            // Exit the loop
                            break;
                        }
                        catch (Exception ex)
                        {
                            // Don't surface to the user — the outer Wait(cts) /
                            // capturedException path is responsible for that. But
                            // a silent break here historically hid PKCE listener
                            // bugs (e.g. port collisions, cert issues) entirely.
                            // Debug.WriteLine is compiled out in Release; in Debug
                            // it surfaces to DebugView / VS Output for diagnosis.
                            System.Diagnostics.Debug.WriteLine(
                                $"PKCE listener loop terminated: {ex.GetType().Name}: {ex.Message}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
            }, cts);

            // Abort a stalled PKCE wait so the cmdlet can't hang indefinitely.
            // When Identity leaves the browser on an error page (e.g. a partition
            // mismatch) it never redirects back to the local listener, so
            // GetContextAsync never returns. Interactive users can Ctrl+C (handled
            // below); the 3-minute timeout covers non-interactive contexts
            // (CI / automation) where no Ctrl+C ever arrives. On timeout Wait
            // returns false and the finally still runs Stop()/Close(), freeing the
            // port and unwinding the listener task.
            const int pkceTimeoutMs = 3 * 60 * 1000;
            bool completed = false;
            try
            {
                // Block the main thread until the task completes, is canceled
                // (Ctrl+C), or the timeout elapses.
                completed = listeningTask.Wait(pkceTimeoutMs, cts);
            }
            catch (OperationCanceledException oce)
            {
                // This is the path PKCE-failure users actually hit: the
                // browser was left on an Identity error page and never
                // called back to the local listener, so listeningTask.Wait
                // blocked until they Ctrl+C. Task.Wait(CancellationToken)
                // throws *bare* OperationCanceledException on token-fired
                // cancellation (NOT wrapped in AggregateException), so the
                // AggregateException catch below would not see it; the OCE
                // would propagate with its default ctor message ("The
                // operation was canceled.") and PowerShell would print that,
                // swallowing any hint we tried to attach. Re-throwing as
                // InvalidOperationException is what reliably surfaces the
                // hint message verbatim — Resolve-OrchAuthError exists
                // exactly for this, but the user has to be told.
                throw new InvalidOperationException(
                        "PKCE sign-in was canceled (Ctrl+C). If the browser was left on a sign-in error page "
                        + PkceErrorPageHint, oce);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Any(e => e is OperationCanceledException))
                {
                    // Rare path: listeningTask faulted with OCE before the
                    // outer Wait observed cancellation. Same hint applies;
                    // same exception-type swap reason as above.
                    throw new InvalidOperationException(
                        "PKCE sign-in was canceled (Ctrl+C). If the browser was left on a sign-in error page "
                        + PkceErrorPageHint, ae);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                // Stop the listener
                if (listener.IsListening)
                {
                    listener.Stop();
                }

                listener.Close();
            }

            if (!completed)
            {
                // Timed out: not Ctrl+C, not completed -- the browser never called
                // back. The finally above already stopped/closed the listener and
                // freed the port; surface an actionable terminating error rather
                // than let the caller proceed unauthenticated (which returns
                // misleading empty results).
                throw new InvalidOperationException(
                    "PKCE sign-in timed out after 3 minutes (no browser callback received). "
                    + "If the browser was left on a sign-in error page "
                    + PkceErrorPageHint);
            }

            if (capturedException is not null)
            {
                throw capturedException;
            }

            if (authorizationCode is null)
            {
                throw new InvalidOperationException(
                    "Authorization code was not received. If the browser showed an error page "
                    + PkceErrorPageHint);
            }

            return authorizationCode;
        }
    }

    private static string GetHash(string input)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(data).TrimEnd('=').Replace("+", "-").Replace("/", "_");
    }

    /// Tri-state classification of the signed-in principal for the Entra-ID
    /// local-user advisory. The third state (Unknown) is the whole point: a
    /// probe taken before the token is available must NOT be mistaken for
    /// "not a local user", or the caller would latch a premature "no warning"
    /// decision and suppress the advisory for the rest of the session.
    internal enum EntraUserKind
    {
        Unknown,             // no parseable token yet — retry on a later probe
        LocalUser,           // signed in with a local (non-directory) account
        EntraOrNotApplicable // signed in via Entra ID, or a principal the advisory doesn't apply to
    }

    /// Classify this session's signed-in principal (reads the current access token).
    internal EntraUserKind GetEntraUserKind() => ClassifyEntraUserKind(_access_token);

    /// Classify the signed-in principal from the JWT's ext_idp_disp_name claim.
    /// "aad" means the directory (Entra ID); any other display name (e.g.
    /// "GlobalIdp" for a local / social account) means a local user. A missing
    /// claim (Confidential App, robot account) is not applicable. No / unparseable
    /// token yields Unknown so the caller can retry instead of latching. Pure /
    /// static so the classification is unit-testable without a live token
    /// (see EntraAdvisoryTests).
    internal static EntraUserKind ClassifyEntraUserKind(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return EntraUserKind.Unknown;

        var parts = accessToken.Split('.');
        if (parts.Length != 3) return EntraUserKind.Unknown;

        try
        {
            using JsonDocument doc = JsonDocument.Parse(Jwt.DecodePayloadJson(parts[1]));
            if (doc.RootElement.TryGetProperty("ext_idp_disp_name", out JsonElement element))
            {
                return element.GetString() == "aad"
                    ? EntraUserKind.EntraOrNotApplicable
                    : EntraUserKind.LocalUser;
            }
            return EntraUserKind.EntraOrNotApplicable; // No ext_idp_disp_name — not applicable
        }
        catch
        {
            return EntraUserKind.Unknown;
        }
    }

    /// The once-per-session Entra-advisory gate decision: whether to QUEUE the
    /// advisory now, and whether to LATCH the gate so the probe doesn't repeat.
    internal readonly record struct EntraAdvisoryDecision(bool QueueWarning, bool Latch);

    /// Decide the Entra-ID local-user advisory from the classified principal and
    /// what the probe has resolved so far. The gate latches ONLY on a CONCLUSIVE
    /// outcome — a probe taken before the token, partition id, or org auth setting
    /// are available (Unknown / partition-unknown / setting-not-fetched) is left
    /// un-latched so a later enumeration retries it instead of permanently
    /// suppressing the advisory. Pure / static so every path is unit-testable
    /// without driving the provider or any API (see EntraAdvisoryTests).
    internal static EntraAdvisoryDecision DecideEntraAdvisory(
        EntraUserKind kind, bool partitionKnown, bool authSettingFetched, string? authenticationSettingType)
    {
        switch (kind)
        {
            case EntraUserKind.LocalUser:
                if (!partitionKnown || !authSettingFetched)
                    return new EntraAdvisoryDecision(QueueWarning: false, Latch: false); // inconclusive: retry
                return authenticationSettingType == "aad"
                    ? new EntraAdvisoryDecision(QueueWarning: true, Latch: true)   // org Entra-integrated: warn once
                    : new EntraAdvisoryDecision(QueueWarning: false, Latch: true); // org not Entra: conclusive no-warn

            case EntraUserKind.EntraOrNotApplicable:
                return new EntraAdvisoryDecision(QueueWarning: false, Latch: true); // conclusive no-warn

            default: // Unknown — no parseable token yet
                return new EntraAdvisoryDecision(QueueWarning: false, Latch: false); // retry
        }
    }

    /// Get the partition global ID (prt_id) from the JWT token.
    public string? GetPartitionGlobalIdFromJwt()
    {
        try
        {
            using JsonDocument doc = ParseJwtPayload();
            if (doc.RootElement.TryGetProperty("prt_id", out JsonElement element))
            {
                return element.GetString();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private JsonDocument ParseJwtPayload()
    {
        var parts = _access_token?.Split('.') ?? [];
        if (parts.Length != 3) throw new InvalidOperationException("Invalid JWT");

        return JsonDocument.Parse(Jwt.DecodePayloadJson(parts[1]));
    }

    public string DebugJwtToken()
    {
        var parts = _access_token?.Split('.') ?? [];
        if (parts.Length != 3) return "";

        return Jwt.DecodePayloadJson(parts[1]);
    }

    #region Auth diagnostics logging

    // Fields that must never appear in plaintext in the log file.
    private static readonly System.Collections.Generic.HashSet<string> _authSecretKeys = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "access_token", "refresh_token", "id_token",
        "client_secret", "code", "code_verifier", "assertion",
        "password",
        // The on-prem user/password flow (POST /api/Account/Authenticate) returns the bearer
        // token inside ABP's AjaxResponse envelope, where the key is `result` -- outside the
        // OAuth2/OIDC vocabulary the rest of this set is drawn from, so it used to be logged
        // verbatim at Trace/Verbose (the one auth flow whose token reached the log file, and
        // a direct contradiction of OrchLog's "logs are safe to share" contract). Only the
        // two auth endpoints' traffic reaches MaskAuthSecrets, and on Authenticate `result`
        // is never anything but the token -- so masking it costs no diagnostic detail.
        "result",
    };

    private const string _redactedValue = "***REDACTED***";

    /// <summary>
    /// Redact tokens / codes / secrets from a request URL, JSON body, or
    /// form-encoded body before it lands in the auth log file. We only
    /// recognize the limited set of OAuth2 / OIDC parameter names listed
    /// in <see cref="_authSecretKeys"/>; any other token-shaped value is
    /// left as-is (auth flow bodies are well-defined, no need to be
    /// over-eager and corrupt diagnostic detail).
    /// </summary>
    internal static string MaskAuthSecrets(string content, string? contentType)
    {
        if (string.IsNullOrEmpty(content)) return content;

        // application/json: replace "<key>" : "<value>" with "<key>" : "***REDACTED***"
        if (contentType is not null && contentType.Contains("json", System.StringComparison.OrdinalIgnoreCase))
        {
            return System.Text.RegularExpressions.Regex.Replace(
                content,
                @"""(?<k>[A-Za-z_][A-Za-z0-9_]*)""\s*:\s*""(?<v>[^""]*)""",
                match =>
                {
                    var key = match.Groups["k"].Value;
                    return _authSecretKeys.Contains(key)
                        ? $@"""{key}"": ""{_redactedValue}"""
                        : match.Value;
                });
        }

        // application/x-www-form-urlencoded or query string fragment.
        return System.Text.RegularExpressions.Regex.Replace(
            content,
            @"(?<k>[A-Za-z_][A-Za-z0-9_]*)=(?<v>[^&\s]*)",
            match =>
            {
                var key = match.Groups["k"].Value;
                return _authSecretKeys.Contains(key)
                    ? $"{key}={_redactedValue}"
                    : match.Value;
            });
    }

    /// <summary>
    /// Mask query-string secrets on a request URI (e.g. the PKCE redirect
    /// callback carries the authorization <c>code</c> as a query
    /// parameter).
    /// </summary>
    internal static string MaskAuthSecretsInUri(string uri)
    {
        var queryIdx = uri.IndexOf('?');
        if (queryIdx < 0) return uri;
        var prefix = uri[..(queryIdx + 1)];
        var query = uri[(queryIdx + 1)..];
        return prefix + MaskAuthSecrets(query, contentType: null);
    }

    /// <summary>
    /// Dump the drive's authentication-relevant PSDrive settings plus
    /// runtime info to the log file. Runs at most once per AuthManager
    /// instance (i.e. once per session per drive) and only when the
    /// drive's <c>Logging.Enabled</c> is on. Credentials (AppSecret,
    /// AccessToken, Password, proxy credentials) are intentionally
    /// excluded.
    /// </summary>
    internal void LogAuthSettings()
    {
        if (_authSettingsLogged) return;

        var logging = _drive._psDrive.Logging;
        if (!(logging?.Enabled.GetValueOrDefault() ?? false)) return;

        _authSettingsLogged = true;

        var psd = _drive._psDrive;
        string mode = !string.IsNullOrEmpty(psd.AccessToken) ? "Personal Access Token"
                    : _isConfidentialApp ? "Confidential App"
                    : _isUserPassword ? "Username/Password"
                    : "PKCE (Non-Confidential App)";

        var orchVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "(unknown)";
        var psAssembly = Assembly.GetAssembly(typeof(System.Management.Automation.PSCmdlet));
        var psVersion = psAssembly?.GetName().Version?.ToString() ?? "(unknown)";
        var dotnetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        var sb = new StringBuilder();
        sb.AppendLine($"{DateTime.Now:HH:mm:ss.fff} === Auth diagnostics for '{_drive.NameColonSeparator}' (mode: {mode}) ===");
        sb.AppendLine($"  UiPathOrch    : {orchVersion}");
        sb.AppendLine($"  PowerShell    : {psVersion}");
        sb.AppendLine($"  .NET          : {dotnetVersion}");
        sb.AppendLine($"  OS            : {osVersion}");
        sb.AppendLine($"  Root          : {psd.Root}");
        sb.AppendLine($"  Edition       : {psd.ResolvedEdition}");
        sb.AppendLine($"  IdentityUrl   : {psd.IdentityUrl ?? "(default)"}");
        sb.AppendLine($"  AppId         : {psd.AppId ?? "(none)"}");
        sb.AppendLine($"  RedirectUrl   : {psd.RedirectUrl ?? "(none)"}");
        sb.AppendLine($"  HttpListener  : {psd.HttpListener ?? "(none)"}");
        sb.AppendLine($"  Scope         : {psd.Scope ?? "(none)"}");
        sb.AppendLine($"  Username      : {psd.Username ?? "(none)"}");
        sb.AppendLine($"  UseInPrivate  : {UseInPrivate}");
        sb.AppendLine($"  IgnoreSslErrors: {psd.IgnoreSslErrors.GetValueOrDefault()}");
        sb.AppendLine($"  ProxyEnabled  : {psd.Proxy?.Enabled.GetValueOrDefault() ?? false}");
        sb.AppendLine();

        _drive.OrchAPISession.WriteLogBlock(sb.ToString());
    }

    /// <summary>
    /// Log the authorize URL handed to the browser, when the drive's
    /// <c>Logging.Enabled</c> is on. Mirrors <see cref="LogAuthSettings"/>'s
    /// fire-and-forget write. The URL contains no secrets -- only client_id,
    /// redirect_uri, scope, and the public PKCE code challenge -- so it is safe
    /// to persist and share for diagnosing a failing interactive sign-in.
    /// </summary>
    private void LogAuthorizeUrl(string authUrl)
    {
        var logging = _drive._psDrive.Logging;
        if (!(logging?.Enabled.GetValueOrDefault() ?? false)) return;

        _drive.OrchAPISession.WriteLogBlock(
            $"{DateTime.Now:HH:mm:ss.fff} === Authorize URL handed to the browser ===\n{authUrl}\n\n");
    }

    /// <summary>
    /// Wrapper around <c>_httpClient.Send</c> that mirrors
    /// <see cref="OrchAPISession.HttpClient_Send"/>'s logging behavior so
    /// PKCE / Confidential App / Username-password auth call traffic
    /// shows up in the drive's HTTP log file (LoggingLevel-controlled,
    /// secrets redacted). The X-UIPATH-TenantName header / Bearer header
    /// injection that <c>HttpClient_Send</c> does for API calls is
    /// intentionally skipped here — auth calls don't carry a tenant or
    /// access token.
    /// </summary>
    private HttpResponseMessage SendWithLogging(HttpRequestMessage request, System.Threading.CancellationToken token)
    {
        var session = _drive.OrchAPISession;
        var logging = _drive._psDrive.Logging;
        bool logEnabled = logging?.Enabled.GetValueOrDefault() ?? false;

        DateTime reqTime = DateTime.Now;
        DateTime resTime = reqTime;
        HttpResponseMessage? ret = null;
        bool hasException = false;
        int callId = session.NextCallId();

        try
        {
            reqTime = DateTime.Now;
            ret = _httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead, token);
            resTime = DateTime.Now;

            // Buffer body up-front so the async logger doesn't race the
            // caller's response.Content read. Same pattern as
            // HttpClient_Send.
            if (logEnabled && ret.Content != null)
            {
                var level = logging?.InternalLogLevel ?? LoggingLevel.Info;
                if (!ret.IsSuccessStatusCode || level >= LoggingLevel.Trace)
                {
                    ret.Content.LoadIntoBufferAsync().GetAwaiter().GetResult();
                }
            }

            return ret;
        }
        catch (Exception ex)
        {
            resTime = DateTime.Now;
            hasException = true;

            // Same annotation as the API chokepoint, and needed here too: the token exchange is
            // often the FIRST request a session makes, so a proxy that cannot be reached fails
            // here rather than on any Orchestrator call -- while the browser, using its own proxy
            // handling, has already shown a completed sign-in.
            if (ex is HttpRequestException hre
                && OrchHttp.AnnotateProxyFailure(hre, request.RequestUri, _drive._psDrive.Proxy) is Exception annotated)
            {
                throw annotated;
            }

            throw;
        }
        finally
        {
            if (logEnabled)
            {
                // Mask request URI's query string in place so the log block
                // never carries an authorization code (PKCE redirect URI
                // case). Restore after building the block to avoid changing
                // observable behavior for callers that read request.RequestUri
                // after Send.
                var originalUri = request.RequestUri;
                if (originalUri is not null)
                {
                    var masked = MaskAuthSecretsInUri(originalUri.ToString());
                    if (masked != originalUri.ToString())
                    {
                        request.RequestUri = new Uri(masked, UriKind.RelativeOrAbsolute);
                    }
                }

                string? combinedLogBlock;
                try
                {
                    combinedLogBlock = hasException
                        ? $"{reqTime:HH:mm:ss.fff} #{callId:D4} {request.Method} {request.RequestUri}\n{resTime:HH:mm:ss.fff} RES Status: ERROR/CANCELLED\n\n"
                        : OrchAPISession.BuildCombinedLogBlock(reqTime, request, resTime, ret, callId, logging?.InternalLogLevel);

                    if (combinedLogBlock is not null)
                    {
                        // Mask any tokens that may have leaked into the request/response body section.
                        var requestContentType = request.Content?.Headers.ContentType?.MediaType;
                        var responseContentType = ret?.Content?.Headers.ContentType?.MediaType;
                        combinedLogBlock = MaskAuthSecrets(combinedLogBlock, requestContentType);
                        if (responseContentType is not null && responseContentType != requestContentType)
                        {
                            combinedLogBlock = MaskAuthSecrets(combinedLogBlock, responseContentType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Auth log block build failed: {ex.Message}");
                    combinedLogBlock = null;
                }
                finally
                {
                    request.RequestUri = originalUri;
                }

                // Inline, like the API log path -- see OrchAPISession.WriteLogBlock for why the
                // Task.Run offload was removed.
                session.WriteLogBlock(combinedLogBlock);
            }
        }
    }

    #endregion
}
