using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.OrchAPI
{
    internal class OrchestratorAuthManager
    {
        private readonly HttpClient _httpClient;
        private readonly PSDrive _drive;
        public readonly string _baseUrl;
        private readonly string? _onpremiseTenancy;
        private readonly bool _isConfidentialApp;
        private readonly bool _isUserScope;
        public readonly bool _isUserPassword;

        internal bool IsConfidentialApp
        {
            get { return _isConfidentialApp; }
        }

        public string? IdentityUrl;

        private string? _access_token;
        private string? _refresh_token;

        internal bool IsAuthenticated => !string.IsNullOrEmpty(_access_token);
        internal bool IsCloud => _drive.Root?.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase) ?? false;

        internal string? AccessToken => _access_token;

        public OrchestratorAuthManager(PSDrive drive, HttpClient httpClient)
        {
            _httpClient = httpClient;
            this._drive = drive;

            _isConfidentialApp = !string.IsNullOrEmpty(_drive.AppSecret);
            _isUserScope = !string.IsNullOrEmpty(_drive.RedirectUrl);
            _isUserPassword = !string.IsNullOrEmpty(_drive.Password);
            IdentityUrl = _drive.IdentityUrl?.TrimEnd('/');

            //_baseUrl = (_drive.Root!.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
            _baseUrl = IsCloud ? _drive!.Root!.Substring(0, _drive.Root.IndexOf("uipath.com") + "uipath.com".Length)
                : _drive.Root!.TrimEnd('/');

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

            if (!string.IsNullOrEmpty(drive.RedirectUrl) && drive.RedirectUrl!.EndsWith('/'))
                _drive.RedirectUrl = drive.RedirectUrl!.TrimEnd('/');

            if (!string.IsNullOrEmpty(drive.HttpListener) && !drive.HttpListener!.EndsWith('/'))
                _drive.HttpListener += '/';
        }

        public string RequestToken(string driveLetter)
        {
            if (_isConfidentialApp)
            {
                if (_isUserScope) ////// Conf User Scope // ここ動くようにしたい、、
                {
                    string authorizationCode = GetAuthorizationCode(null, driveLetter);
                    (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                    {
                        { "grant_type", "client_credentials" },
                        { "code", authorizationCode },
                        { "client_id", _drive.AppId! },
                        { "client_secret", _drive.AppSecret! },
                        { "scope", _drive.Scope! }
                    });
                    return _access_token;
                }
                else /////////////////// Conf App Scope
                {
                    (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                    {
                        { "grant_type", "client_credentials" },
                        { "client_id", _drive.AppId! },
                        { "client_secret", _drive.AppSecret! },
                        { "scope", _drive.Scope! }
                    });
                    return _access_token;
                }
            }
            else if (!_isUserPassword) /////////////////////// Non-Conf User Scope
            {
                string codeVerifier = RandomString(80);
                string authorizationCode = GetAuthorizationCode(codeVerifier, driveLetter);

                (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode },
                    { "redirect_uri", _drive.RedirectUrl! },
                    { "client_id", _drive.AppId! },
                    { "code_verifier", codeVerifier }
                });
                return _access_token;
            }
            else // user/pass auth
            {
                LoginModel payload = new()
                {
                    tenancyName = _onpremiseTenancy,
                    usernameOrEmailAddress = _drive.Username,
                    password = _drive.Password
                };

                string url = _baseUrl + "/api/Account/Authenticate";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                string strPayload = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(strPayload, Encoding.UTF8, @"application/json");

                var response = _httpClient.Send(request);
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
                { "scope", _drive.Scope! }
            });
        }

        public string? RenewAccessToken()
        {
            if (_isConfidentialApp)
            {
                return RequestToken(""); // no need to pass drive name for confidential app
            }
            else
            {
                (_access_token, _refresh_token) = GetAccessToken(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _drive.AppId! },
                    { "refresh_token", _refresh_token! }
                });
            }

            return _access_token;
        }

        //private (string access_token, string refresh_token) GetAccessToken(Dictionary<string, string> postData)
        //{
        //    string endPoint = _baseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
        //        ? _baseUrl + "/identity_/connect/token"
        //        : _baseUrl + "/identity/connect/token";

        //    var request = new HttpRequestMessage(HttpMethod.Post, endPoint)
        //    {
        //        Content = new FormUrlEncodedContent(postData)
        //    };

        //    using var consoleCancelHandler = new ConsoleCancelHandler();
        //    HttpResponseMessage response = _httpClient.Send(request, consoleCancelHandler.Token);

        //    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        return ("", "");
        //        //throw new Exception(body);
        //    }

        //    dynamic? doc = JsonConvert.DeserializeObject(body);
        //    string access_token = doc?.access_token ?? "";
        //    string refresh_token = doc?.refresh_token ?? "";
        //    return (access_token, refresh_token);
        //}

        private (string access_token, string refresh_token) GetAccessToken(Dictionary<string, string> postData)
        {
            string endPoint;

            if (!string.IsNullOrEmpty(IdentityUrl))
            {
                endPoint = IdentityUrl + "/connect/token";
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

            using var consoleCancelHandler = new ConsoleCancelHandler();
            HttpResponseMessage response = _httpClient.Send(request, consoleCancelHandler.Token);

            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

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

        // driveName は、ブラウザーにマウント成功のメッセージを表示するために必要
        private string GetAuthorizationCode(string? codeVerifier, string driveName)
        {
            string endPoint;
            if (!string.IsNullOrEmpty(IdentityUrl))
            {
                endPoint = IdentityUrl + "/connect/authorize";
            }
            else
            {
                endPoint = _baseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
                    ? _baseUrl + "/identity_/connect/authorize"
                    : _baseUrl + "/identity/connect/authorize";
            }

            string authUrl = null;
            if (!string.IsNullOrEmpty(codeVerifier))
            {
                string codeChallenge = GetHash(codeVerifier);
                authUrl = endPoint + $"?response_type=code&client_id={_drive.AppId}&scope={_drive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(_drive.RedirectUrl)}&code_challenge={codeChallenge}&code_challenge_method=S256";
            }
            else
            {
                authUrl = endPoint + $"?response_type=code&client_id={_drive.AppId}&scope={_drive.Scope} offline_access&redirect_uri={WebUtility.UrlEncode(_drive.RedirectUrl)}";
            }

            using var listener = new HttpListener();
            listener.Prefixes.Add(_drive.HttpListener!);
            listener.Start();

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            string? authorizationCode = null;
            Exception capturedException = null;

            // Manage the Ctrl+C event with ConsoleCancelHandler
            using var consoleCancelHandler = new ConsoleCancelHandler();
            var cts = consoleCancelHandler.Token;

            // Start the listening in a separate task
            var listeningTask = Task.Run(async () =>
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        if (listener.IsListening)
                        {
                            var context = await listener.GetContextAsync();
                            authorizationCode = context.Request.QueryString["code"];
                            if (!string.IsNullOrEmpty(authorizationCode))
                            {
                                // Send a response back to the browser
                                Assembly assembly = Assembly.GetExecutingAssembly();
                                using Stream stream = assembly.GetManifestResourceStream("OrchProvider.MountSuccessNotification.html");
                                using StreamReader reader = new(stream!);
                                string htmlTemplate = reader.ReadToEnd();

                                string responseString = string.Format(htmlTemplate, _drive.Root, driveName);
                                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                context.Response.ContentLength64 = buffer.Length;
                                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cts);
                                context.Response.OutputStream.Close();
                                break;  // Break the loop once the code is obtained
                            }
                        }
                        else
                        {
                            // If listener is not listening, delay a bit before the next check
                            await Task.Delay(100, cts);
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

            if (capturedException != null)
            {
                throw capturedException;
            }

            listener.Stop();

            if (authorizationCode == null)
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
}
