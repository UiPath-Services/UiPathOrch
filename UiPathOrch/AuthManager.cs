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

    private string? _access_token;
    private string? _refresh_token;

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

    public string RequestToken()
    {
        if (!string.IsNullOrEmpty(_drive._psDrive.AccessToken))
        {
            _access_token = _drive._psDrive.AccessToken;
            return _access_token;
        }

        if (_isConfidentialApp)
        {
            {
                (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _drive._psDrive.AppId! },
                    { "client_secret", _drive._psDrive.AppSecret! },
                    { "scope", _drive._psDrive.Scope! }
                });
                return _access_token;
            }
        }
        else if (!_isUserPassword)
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
        else // user/pass auth
        {
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
            using var response = _httpClient.Send(request, cts.Token);
            var body = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<AjaxResponse>(body)?.result ?? "";
        }
    }

    public string? RenewAccessToken()
    {
        if (_isConfidentialApp)
        {
            return RequestToken(); // no need to pass drive name for confidential app
        }
        else
        {
            (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", _drive._psDrive.AppId! },
                { "refresh_token", _refresh_token! }
            });
        }

        return _access_token;
    }

    private (string access_token, string refresh_token) GetAccessToken(Dictionary<string, string> postData)
    {
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
        using HttpResponseMessage response = _httpClient.Send(request, cts.Token);

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

        string access_token = root.TryGetProperty("access_token", out JsonElement accessTokenElement) ? accessTokenElement.GetString() ?? "" : "";
        string refresh_token = root.TryGetProperty("refresh_token", out JsonElement refreshTokenElement) ? refreshTokenElement.GetString() ?? "" : "";

        return (access_token, refresh_token);
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

        string authUrl = !string.IsNullOrEmpty(codeVerifier)
            ? $"{endPoint}?response_type=code&client_id={_drive._psDrive.AppId}&scope={_drive._psDrive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(_drive._psDrive.RedirectUrl)}&code_challenge={GetHash(codeVerifier)}&code_challenge_method=S256{acrValues}"
            : $"{endPoint}?response_type=code&client_id={_drive._psDrive.AppId}&scope={_drive._psDrive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(_drive._psDrive.RedirectUrl)}{acrValues}";

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

        if (UseInPrivate)
        {
            // Open in InPrivate browser (Edge + temporary profile for complete cookie isolation)
            string edgePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft\Edge\Application\msedge.exe");
            if (!File.Exists(edgePath))
            {
                edgePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
                    @"Microsoft\Edge\Application\msedge.exe");
            }
            string tempProfile = Path.Combine(Path.GetTempPath(), "UiPathOrch_" + Guid.NewGuid().ToString("N")[..8]);
            Process.Start(new ProcessStartInfo(edgePath, $"--inprivate --user-data-dir=\"{tempProfile}\" \"{authUrl}\"") { UseShellExecute = false });
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
                            string responseString = string.Format(htmlTemplate, _drive._psDrive.Root, _drive.NameColon, versionStr, LoadBotImageRandomly(), userStyle, userEncoded);

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
                    catch (Exception)
                    {
                        //Console.WriteLine($"Unexpected exception: {ex}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                capturedException = ex;
            }
        }, cts);

        try
        {
            // Block the main thread until the task completes or is canceled
            listeningTask.Wait(cts);
        }
        catch (AggregateException ae)
        {
            if (ae.InnerExceptions.Any(e => e is OperationCanceledException))
            {
                throw new OperationCanceledException("The operation was canceled by the user.", ae);
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

        if (capturedException is not null)
        {
            throw capturedException;
        }

        if (authorizationCode is null)
        {
            throw new InvalidOperationException("Authorization code was not received.");
        }

        return authorizationCode;
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

        // Base64URL decode
        var payload = parts[1];
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        payload = payload.Replace('-', '+').Replace('_', '/');

        var jsonBytes = Convert.FromBase64String(payload);
        var json = Encoding.UTF8.GetString(jsonBytes);
        return JsonDocument.Parse(json);
    }

    public string DebugJwtToken()
    {
        var parts = _access_token?.Split('.') ?? [];
        if (parts.Length != 3) return "";

        var payload = parts[1];
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        payload = payload.Replace('-', '+').Replace('_', '/');

        var jsonBytes = Convert.FromBase64String(payload);
        var json = Encoding.UTF8.GetString(jsonBytes);
        return json;
    }
}
