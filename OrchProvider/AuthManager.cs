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
            // 1. 空チェック
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new InvalidOperationException("The provided URL is null or empty.");
            }

            // 2. Uri としてパースを試みる
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("The provided URL is not a valid absolute URI.");
            }

            // 3. 絶対パスから末尾のスラッシュを除去し、'/' 区切りで分割
            var path = uri.AbsolutePath.TrimEnd('/'); // "/" → ""
            var segments = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

            // 4. ドメインに "uipath.com" を含む場合は、必ずパスにテナンシーが存在すること
            if (uri.Host.Contains("uipath.com", StringComparison.OrdinalIgnoreCase) && segments.Length == 0)
            {
                throw new InvalidOperationException(
                    "For domains containing 'uipath.com', the URL must be in the format 'https://domain/org/tenancy'."
                );
            }

            if (segments.Length == 0)
            {
                // 【ケース1】テナンシーが指定されていない場合（かつ uipath.com 以外のドメイン）
                OnpremiseTenancy = string.Empty;
                BaseUrl = uri.GetLeftPart(UriPartial.Authority); // 例: "https://orchestrator.local"
            }
            else
            {
                // 【ケース2】テナンシーが指定されている場合
                OnpremiseTenancy = segments.Last();

                // テナンシーを除いた残りのパスを再構築
                var remainingSegments = segments.Take(segments.Length - 1);
                string newPath = remainingSegments.Any()
                    ? "/" + string.Join("/", remainingSegments) // 例: "/folder1"
                    : "/"; // ドメイン直下しかない場合は "/" をセット

                // UriBuilder で scheme+authority+新しいパスを組み立て
                var builder = new UriBuilder(uri)
                {
                    Path = newPath
                };

                // 末尾のスラッシュを除去して BaseUrl にセット
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

            (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", authorizationCode },
                { "redirect_uri", _drive._psDrive.RedirectUrl! },
                { "client_id", _drive._psDrive.AppId! },
                { "code_verifier", codeVerifier }
            });
            return _access_token;
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
            var response = _httpClient.Send(request, cts.Token);
            var body = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<AjaxResponse>(body)?.result ?? "";
        }
    }

    public (string access_token, string refresh_token) RequestRefreshToken()
    {
        return GetAccessToken(new Dictionary<string, string>
        {
            { "access_token", _access_token! },
            { "expires_in", "3600" },
            { "token_type", "Bearer" },
            { "refresh_token", "" },
            { "scope", _drive._psDrive.Scope! }
        });
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
            endPoint = BaseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
                ? BaseUrl + "/identity_/connect/token"
                : BaseUrl + "/identity/connect/token";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endPoint)
        {
            Content = new FormUrlEncodedContent(postData)
        };

        using var cts = new ConsoleCancelHandler();
        HttpResponseMessage response = _httpClient.Send(request, cts.Token);

        string body = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            //return ("", "");
            throw new Exception(body);
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
        // 埋め込みリソースの名前一覧
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

        // ランダムに1つ選択
        var random = new Random();
        int index = random.Next(resourceNames.Count);
        string selectedResource = resourceNames[index];

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OrchProvider.bot." + selectedResource);
        if (stream is null) return "";

        // ストリームをバイト配列に変換
        using var memoryStream = new MemoryStream();

        stream.CopyTo(memoryStream);
        byte[] imageBytes = memoryStream.ToArray();

        // Base64にエンコード
        return Convert.ToBase64String(imageBytes);
    }

    private string GetAuthorizationCode(string? codeVerifier)
    {
        string endPoint;
        if (!string.IsNullOrEmpty(_drive._psDrive.IdentityUrl))
        {
            endPoint = _drive._psDrive.IdentityUrl + "/connect/authorize";
        }
        else
        {
            endPoint = BaseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
                ? BaseUrl + "/identity_/connect/authorize"
                : BaseUrl + "/identity/connect/authorize";
        }

        string authUrl = !string.IsNullOrEmpty(codeVerifier)
            ? $"{endPoint}?response_type=code&client_id={_drive._psDrive.AppId}&scope={_drive._psDrive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(_drive._psDrive.RedirectUrl)}&code_challenge={GetHash(codeVerifier)}&code_challenge_method=S256"
            : $"{endPoint}?response_type=code&client_id={_drive._psDrive.AppId}&scope={_drive._psDrive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(_drive._psDrive.RedirectUrl)}";

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

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

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
                            // Send a response back to the browser
                            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OrchProvider.Resources.en.MountSuccessNotification.html");
                            using StreamReader reader = new(stream!);
                            string htmlTemplate = await reader.ReadToEndAsync();

                            // 画像とバージョン情報の埋め込み
                            var version = Assembly.GetExecutingAssembly().GetName().Version;
                            string responseString = string.Format(htmlTemplate, _drive._psDrive.Root, _drive.NameColon, version, LoadBotImageRandomly());

                            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.ContentType = "text/html; charset=UTF-8";
                            context.Response.Headers["Connection"] = "close";

                            // レスポンスを送信
                            await using var output = context.Response.OutputStream;
                            await output.WriteAsync(buffer, cts);
                            await output.FlushAsync();

                            await Task.Delay(50); // 画面が真っ白になることがあるんだけど、これで回避できるのかな。。

                            // レスポンスを明示的に終了
                            context.Response.Close();

                            // ループを終了
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
            // リスナーの停止
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

    /// Entra ID 警告メッセージを表示すべきかチェック
    /// 警告を表示すべき場合は true
    public bool ShouldShowEntraIdWarning()
    {
        try
        {
            var parts = _access_token?.Split('.') ?? [];
            if (parts.Length != 3) return false;

            // Base64URLデコード
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            payload = payload.Replace('-', '+').Replace('_', '/');

            var jsonBytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);

            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ext_idp_disp_name", out JsonElement element))
            {
                return element.GetString() == "GlobalIdp";
            }

            return false;
        }
        catch
        {
            return false;
        }
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
