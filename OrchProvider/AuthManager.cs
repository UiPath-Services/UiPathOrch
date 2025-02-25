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
    public readonly string _baseUrl;
    private readonly string? _onpremiseTenancy;
    private readonly bool _isConfidentialApp;
    //private readonly bool _isUserScope;
    public readonly bool _isUserPassword;

    internal bool IsConfidentialApp
    {
        get { return _isConfidentialApp; }
    }

    //public string? IdentityUrl;

    private string? _access_token;
    private string? _refresh_token;

    internal bool IsAuthenticated => !string.IsNullOrEmpty(_access_token);

    internal string? AccessToken => _access_token;

    public OrchestratorAuthManager(OrchDriveInfo drive, HttpClient httpClient)
    {
        _httpClient = httpClient;
        this._drive = drive;

        _isConfidentialApp = !string.IsNullOrEmpty(_drive._psDrive.AppSecret);
        // 機密アプリのユーザーモードをサポートしたいのだけど、RedirectUrl がカスケードして
        // 設定されてしまうと、なんだかうまくいかないな。。
        //_isUserScope = !string.IsNullOrEmpty(_drive.RedirectUrl);
        _isUserPassword = !string.IsNullOrEmpty(_drive._psDrive.Password);
        //IdentityUrl = _drive._psDrive.IdentityUrl?.TrimEnd('/');

        //_baseUrl = (_drive.Root!.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
        _baseUrl = drive._psDrive.IsCloud ? _drive!._psDrive.Root!.Substring(0, _drive._psDrive.Root.IndexOf("uipath.com") + "uipath.com".Length)
            : _drive._psDrive.Root!.TrimEnd('/');

        if (_isUserPassword) /////////////////////// Non-Conf User Scope
        {
            // 最後のスラッシュの位置を取得
            int lastSlashIndex = _baseUrl.LastIndexOf('/');

            // 最後のスラッシュの位置が見つかった場合に分割
            if (lastSlashIndex < 0)
            {
                throw new InvalidOperationException("The provided Root is not in the expected format 'https://domain/tenancy'.");
            }
            _onpremiseTenancy = _baseUrl.Substring(lastSlashIndex + 1).TrimEnd('/');
            _baseUrl = _baseUrl.Substring(0, lastSlashIndex);
        }
    }

    public string RequestToken()
    {
        if (_isConfidentialApp)
        {
            //if (_isUserScope) ////// Conf User Scope // 機密アプリのユーザースコープ。ここ動くようにしたい、、
            //{
            //    string authorizationCode = GetAuthorizationCode(null, driveLetter);
            //    (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
            //    {
            //        { "grant_type", "client_credentials" },
            //        { "code", authorizationCode },
            //        { "client_id", _drive.AppId! },
            //        { "client_secret", _drive.AppSecret! },
            //        { "scope", _drive.Scope! }
            //    });
            //    return _access_token;
            //}
            //else /////////////////// Conf App Scope
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
        else if (!_isUserPassword) /////////////////////// Non-Conf User Scope
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
                tenancyName = _onpremiseTenancy,
                usernameOrEmailAddress = _drive._psDrive.Username,
                password = _drive._psDrive.Password
            };

            string url = _baseUrl + "/api/Account/Authenticate";
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
            endPoint = _baseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
                ? _baseUrl + "/identity_/connect/token"
                : _baseUrl + "/identity/connect/token";
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
        Random random = new();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
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
            endPoint = _baseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
                ? _baseUrl + "/identity_/connect/authorize"
                : _baseUrl + "/identity/connect/authorize";
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

    // 80 ポート以外のポートも開けるコード？
    //private string GetAuthorizationCode(string codeVerifier, string driveLetter)
    //{
    //    string codeChallenge = GetHash(codeVerifier);

    //    // リダイレクト用のエンドポイントを指定（ここでは5000番ポートを使用）
    //    string redirectEndpoint = "http://localhost:5000/oauth-callback";
    //    string authUrl = _baseUrl + $"/identity_/connect/authorize?response_type=code&client_id={_drive.AppId}&scope={_drive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(redirectEndpoint)}&code_challenge={codeChallenge}&code_challenge_method=S256";

    //    Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

    //    string authorizationCode = "";
    //    var hostCompletionSource = new TaskCompletionSource<object>();

    //    var host = new WebHostBuilder()
    //        .UseKestrel()
    //        .Configure(app =>
    //        {
    //            app.Map("/oauth-callback", builder =>
    //            {
    //                builder.Run(async context =>
    //                {
    //                    authorizationCode = context.Request.Query["code"];

    //                    Assembly assembly = Assembly.GetExecutingAssembly();
    //                    using Stream stream = assembly.GetManifestResourceStream("OrchProvider.MountSuccessNotification.html");
    //                    using StreamReader reader = new StreamReader(stream!);
    //                    string htmlTemplate = reader.ReadToEnd();

    //                    string responseString = string.Format(htmlTemplate, driveLetter);
    //                    await context.Response.WriteAsync(responseString);

    //                    hostCompletionSource.SetResult(null);  // Signal the host to shut down.
    //                });
    //            });
    //        })
    //        .Build();

    //    host.Start();

    //    hostCompletionSource.Task.Wait();  // Wait for the callback to be processed.

    //    host.StopAsync(TimeSpan.FromSeconds(5)).Wait();  // Gracefully stop the server.

    //    return authorizationCode ?? "";
    //}

    //private int GetAvailablePort(int startRange, int endRange)
    //{
    //    IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
    //    IPEndPoint[] endPoints = ipGlobalProperties.GetActiveTcpListeners();

    //    List<int> usedPorts = endPoints.Select(p => p.Port).ToList();
    //    for (int port = startRange; port <= endRange; port++)
    //    {
    //        if (!usedPorts.Contains(port))
    //        {
    //            return port;
    //        }
    //    }
    //    throw new InvalidOperationException("No available port found in the specified range.");
    //}

    private static string GetHash(string input)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(data).TrimEnd('=').Replace("+", "-").Replace("/", "_");
    }
}
