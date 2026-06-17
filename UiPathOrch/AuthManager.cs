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
                    return JsonSerializer.Deserialize<AjaxResponse>(body)?.result ?? "";
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

    private string GetAuthorizationCode(string? codeVerifier)
    {
        // See _pkceLock declaration for the rationale. Held for the full
        // browser-auth round-trip; released as soon as the token exchange
        // completes (or fails) so the next pending drive's auth can proceed.
        lock (_pkceLock)
        {
            LogAuthSettings();
            string endPoint;
            string acrValues = "";
            if (!string.IsNullOrEmpty(_drive._psDrive.IdentityUrl))
            {
                endPoint = _drive._psDrive.IdentityUrl + "/connect/authorize";
            }
            else if (_drive._psDrive.IsCloud)
            {
                // Cloud: The authorize endpoint uses a common path without the org prefix,
                // and specifies the organization name via acr_values (to accommodate UiPath Identity spec changes).
                var baseUri = new Uri(BaseUrl);
                string orgName = baseUri.AbsolutePath.Trim('/');
                endPoint = $"{baseUri.Scheme}://{baseUri.Host}/identity_/connect/authorize";
                acrValues = UseInPrivate
                    ? "" // InPrivate: omit acr_values to display the authentication provider selection screen
                    : $"&acr_values=tenantName:{orgName}";
            }
            else
            {
                endPoint = BaseUrl + "/identity/connect/authorize";
            }

            // The scope value contains literal spaces (space-delimited scopes plus
            // " offline_access"), so it must be URL-encoded like redirect_uri.
            // Windows browsers normalized raw spaces to %20, masking the omission;
            // on macOS the launch path splits the URL at the first raw space, so
            // everything after the scope value (including redirect_uri) was lost
            // and Identity rejected the request with "Invalid redirect_uri".
            string encodedScope = WebUtility.UrlEncode($"{_drive._psDrive.Scope} offline_access");
            string authUrl = !string.IsNullOrEmpty(codeVerifier)
                ? $"{endPoint}?response_type=code&client_id={_drive._psDrive.AppId}&scope={encodedScope}&redirect_uri={WebUtility.UrlEncode(_drive._psDrive.RedirectUrl)}&code_challenge={GetHash(codeVerifier)}&code_challenge_method=S256{acrValues}"
                : $"{endPoint}?response_type=code&client_id={_drive._psDrive.AppId}&scope={encodedScope}&redirect_uri={WebUtility.UrlEncode(_drive._psDrive.RedirectUrl)}{acrValues}";

            // Log the exact URL handed to the browser (when the drive's Logging is
            // enabled). This is the authorize request as Identity receives it, so a
            // failing interactive sign-in can be inspected without digging through
            // browser history. The URL carries no secrets -- only client_id,
            // redirect_uri, scope, and the public PKCE code challenge.
            LogAuthorizeUrl(authUrl);

            using var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add(_drive._psDrive.HttpListener!);
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                // If starting the listener failed
                var uri = new Uri(_drive._psDrive.RedirectUrl!);
                string message = uri.Port <= 1024
                    ? $"Failed to start the HttpListener. The port {uri.Port} specified in 'RedirectUrl' may require administrative privileges. Please ensure you have the necessary permissions or try changing this port in the configuration file, which can be opened using the Edit-OrchConfig cmdlet."
                    : $"Failed to start the HttpListener. The port {uri.Port} specified in 'RedirectUrl' may be in use. Try changing this port in the configuration file, which can be opened using the Edit-OrchConfig cmdlet.";
                throw new InvalidOperationException(message, ex);
            }

            if (UseInPrivate && OperatingSystem.IsWindows())
            {
                // Open in InPrivate browser (Edge + temporary profile for complete cookie
                // isolation). This path is Edge-on-Windows only; on other platforms (or when
                // Edge is not installed) we fall back to the default browser below so the
                // sign-in still completes — the InPrivate isolation just can't be honored.
                string edgePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
                    @"Microsoft\Edge\Application\msedge.exe");
                if (!File.Exists(edgePath))
                {
                    edgePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
                        @"Microsoft\Edge\Application\msedge.exe");
                }
                if (File.Exists(edgePath))
                {
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
                                // Exchange the auth code for tokens inline so we can display
                                // the authenticated user's name on the success page. The
                                // caller (RequestToken) will skip its own exchange when
                                // _access_token is already set. If exchange fails here, we
                                // continue to render the page without a username and let the
                                // caller's retry surface the error through the normal path.
                                string userName = "";
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
                                    catch
                                    {
                                        // Reset so caller's retry path runs the exchange and surfaces the real error.
                                        _access_token = null;
                                        _refresh_token = null;
                                    }
                                }

                                // Send a response back to the browser
                                string lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                                string[] supportedLangs = ["de", "en", "fr", "ja", "ko", "ro", "tr"];
                                if (!supportedLangs.Contains(lang)) lang = "en";

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

                                // The Orchestrator drive plus the Du / Tm shadow drives that
                                // Import-OrchConfig mounts alongside it — created when the drive's
                                // scope includes Du. / TM. scopes (the same condition used to create
                                // them), and named <Name>Du / <Name>Tm. List whichever apply.
                                string baseName = _drive.NameColon.TrimEnd(':');
                                string driveScope = _drive._psDrive.Scope ?? "";
                                var mountedDrives = new List<string> { _drive.NameColon };
                                if (driveScope.Contains("Du.")) mountedDrives.Add($"{baseName}Du:");
                                if (driveScope.Contains("TM.")) mountedDrives.Add($"{baseName}Tm:");
                                string mountedDrivesStr = string.Join(", ", mountedDrives);

                                string responseString = string.Format(htmlTemplate, _drive._psDrive.Root, mountedDrivesStr, versionStr, LoadBotImageRandomly(), userStyle, userEncoded);

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

                                // Exit the loop
                                break;
                            }
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
                        "PKCE sign-in was canceled (Ctrl+C). If the browser "
                        + "was left on a sign-in error page (e.g. "
                        + "An unknown error has occurred. (#200)), "
                        + "copy that page's full URL from the address bar, "
                        + "run `cd $HOME`, then "
                        + "`Resolve-OrchAuthError '<url>'`.", oce);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Any(e => e is OperationCanceledException))
                {
                    // Rare path: listeningTask faulted with OCE before the
                    // outer Wait observed cancellation. Same hint applies;
                    // same exception-type swap reason as above.
                    throw new InvalidOperationException(
                        "PKCE sign-in was canceled (Ctrl+C). If the browser "
                        + "was left on a sign-in error page (e.g. "
                        + "An unknown error has occurred. (#200)), "
                        + "copy that page's full URL from the address bar, "
                        + "run `cd $HOME`, then "
                        + "`Resolve-OrchAuthError '<url>'`.", ae);
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
                    + "If the browser was left on a sign-in error page (e.g. "
                    + "An unknown error has occurred. (#200)), "
                    + "copy that page's full URL from the address bar, "
                    + "run `cd $HOME`, then "
                    + "`Resolve-OrchAuthError '<url>'`.");
            }

            if (capturedException is not null)
            {
                throw capturedException;
            }

            if (authorizationCode is null)
            {
                throw new InvalidOperationException(
                    "Authorization code was not received. If the browser "
                    + "showed an error page (e.g. "
                    + "An unknown error has occurred. (#200)), "
                    + "copy that page's full URL from the address bar, "
                    + "run `cd $HOME`, then "
                    + "`Resolve-OrchAuthError '<url>'`.");
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

    /// Check whether the current user is NOT signed in via Entra ID.
    /// Returns true if the user is a local (non-Entra ID) user.
    public bool IsNonEntraIdUser()
    {
        try
        {
            using JsonDocument doc = ParseJwtPayload();
            if (doc.RootElement.TryGetProperty("ext_idp_disp_name", out JsonElement element))
            {
                return element.GetString() != "aad";
            }
            return false; // No ext_idp_disp_name (e.g., Confidential App) — not applicable
        }
        catch
        {
            return false;
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

        var block = sb.ToString();
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try { await _drive.OrchAPISession.WriteLogBlockAsync(block, System.Threading.CancellationToken.None); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Auth settings log write failed: {ex.Message}"); }
        });
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

        var block = $"{DateTime.Now:HH:mm:ss.fff} === Authorize URL handed to the browser ===\n{authUrl}\n\n";
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try { await _drive.OrchAPISession.WriteLogBlockAsync(block, System.Threading.CancellationToken.None); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Authorize URL log write failed: {ex.Message}"); }
        });
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
        catch
        {
            resTime = DateTime.Now;
            hasException = true;
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

                if (!string.IsNullOrEmpty(combinedLogBlock))
                {
                    var blockToWrite = combinedLogBlock;
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try { await session.WriteLogBlockAsync(blockToWrite, System.Threading.CancellationToken.None); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Async auth log write failed: {ex.Message}"); }
                    });
                }
            }
        }
    }

    #endregion
}
