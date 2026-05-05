#pragma warning disable IDE1006 // Naming styles

using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using Job = UiPath.PowerShell.Entities.Job;
using Release = UiPath.PowerShell.Entities.Release;
using Session = UiPath.PowerShell.Entities.Session;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.OrchAPI;

// Limits access to resources.
// Maximum concurrent accesses: maxConcurrentRequests
// Maximum requests per second: maxRequestsPerSecond
// Thread limits are capped by the OrchThreadPoolImpl class, so they are excluded from this class.
public class RateLimiter : IDisposable
{
    private readonly SemaphoreSlim rateLimitSemaphore;
    private readonly int maxRequestsPerSecond;
    private readonly Timer refillTimer; // Keep a reference to the timer so it is not garbage collected

    public RateLimiter(int maxRequestsPerSecond)
    {
        this.maxRequestsPerSecond = maxRequestsPerSecond;
        this.rateLimitSemaphore = new SemaphoreSlim(maxRequestsPerSecond, maxRequestsPerSecond);

        // Set up a timer to refill rate limit tokens every second
        refillTimer = new Timer(RefillRateLimitTokens!, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(1));
    }

    private void RefillRateLimitTokens(object state)
    {
        {
            // Calculate the number of tokens to release
            int tokensToRelease = maxRequestsPerSecond - rateLimitSemaphore.CurrentCount;
            if (tokensToRelease > 0)
            {
                rateLimitSemaphore.Release(tokensToRelease);
            }
        }
    }

    public void Wait()
    {
        rateLimitSemaphore.Wait();
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await rateLimitSemaphore.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        refillTimer.Dispose();
        rateLimitSemaphore.Dispose();
    }
}

public partial class OrchAPISession : IDisposable
{
    // Partial method for async log writer disposal
    partial void DisposeAsyncLogWriter();

    private readonly HttpClient _httpClient;
    private HttpClient HttpClient
    {
        get
        {
            EnsureAuthenticated();
            return _httpClient;
        }
    }

    // Limit the number of requests that can be sent per second to 15
    private readonly RateLimiter limiter = new(15);

    private int http_call_num = 0;
    private HttpResponseMessage HttpClient_Send(HttpRequestMessage message, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        limiter.Wait();
        HttpResponseMessage? ret = null;
        DateTime reqTime = DateTime.Now;
        DateTime resTime = reqTime;
        int callId = Interlocked.Increment(ref http_call_num);
        bool hasException = false;
        var logging = _drive._psDrive.Logging;
        bool logEnabled = logging?.Enabled.GetValueOrDefault() ?? false;
        if (logEnabled) EnsureLoggingWarningEmitted();
        if (_drive._psDrive.IgnoreSslErrors.GetValueOrDefault()) EnsureSslWarningEmitted();

        try
        {
            // Add tenant name to headers if specified in on-premises environment
            if (!string.IsNullOrEmpty(_authManager.OnpremiseTenancy))
            {
                message.Headers.TryAddWithoutValidation("X-UIPATH-TenantName", _authManager.OnpremiseTenancy);
            }

            reqTime = DateTime.Now;
            HttpClient hc = httpClient ?? HttpClient;
            ret = hc.Send(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resTime = DateTime.Now;

            // Buffer response body when the async logger will need to read it,
            // preventing "stream already consumed" race between caller and logger.
            if (logEnabled && httpClient == null && ret.Content != null)
            {
                var level = logging?.InternalLogLevel ?? LoggingLevel.Info;
                if (!ret.IsSuccessStatusCode || level >= LoggingLevel.Trace)
                {
                    ret.Content.LoadIntoBufferAsync().GetAwaiter().GetResult();
                }
            }

            return ret;
        }
        catch (Exception)
        {
            resTime = DateTime.Now;
            hasException = true;
            throw; // Re-throw the exception
        }
        finally
        {
            if (logEnabled)
            {
                // Build the log block synchronously while `ret` is still owned by us;
                // the response body has already been buffered above via LoadIntoBufferAsync,
                // so this does not block on additional network I/O. Only the disk write is
                // pushed to the background, which avoids racing with the caller's `using`
                // disposal of the HttpResponseMessage.
                string? combinedLogBlock;
                try
                {
                    if (hasException)
                    {
                        combinedLogBlock = $"{reqTime:HH:mm:ss.fff} #{callId:D4} {message.Method} {message.RequestUri}\n{resTime:HH:mm:ss.fff} RES Status: ERROR/CANCELLED\n\n";
                    }
                    else
                    {
                        combinedLogBlock = BuildCombinedLogBlock(reqTime, message, resTime, ret, callId, logging?.InternalLogLevel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Log block build failed: {ex.Message}");
                    combinedLogBlock = null;
                }

                if (!string.IsNullOrEmpty(combinedLogBlock))
                {
                    var blockToWrite = combinedLogBlock;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await WriteLogBlockAsync(blockToWrite, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Async log write failed: {ex.Message}");
                        }
                    });
                }
            }
        }
    }

    private DateTime _expiryTime; // Holds the exact time when the token expires
    private readonly OrchestratorAuthManager _authManager;

    internal OrchestratorAuthManager AuthManager
    {
        get { return _authManager; }
    }

    internal readonly string _base_url;
    internal readonly string _base_url_identity;
    internal readonly string _base_url_portal;
    internal volatile bool _isAuthenticated = false;
    private bool _disposed = false;
    private readonly OrchDriveInfo _drive;
    public double? ApiVersion;

    // Deferred warning message to be displayed when a cmdlet runs (not during tab completion)
    internal string? PendingWarning { get; set; }
    internal void ClearPendingWarning() => PendingWarning = null;

    // Whether the Entra ID warning check has been performed
    internal bool EntraIdWarningChecked { get; set; }

    // Emitted once per session via PendingWarning when IgnoreSslErrors is set, so users
    // are reminded that MITM attacks would go undetected on this drive's connections.
    private bool _sslWarningEmitted;

    private void EnsureSslWarningEmitted()
    {
        if (_sslWarningEmitted) return;
        _sslWarningEmitted = true;

        string warning =
            $"TLS certificate validation is disabled for '{_drive.NameColonSeparator}' " +
            "(IgnoreSslErrors = true). Man-in-the-middle attacks on this connection would " +
            "not be detected. Use only on trusted networks (VPN / internal LAN).";

        PendingWarning = string.IsNullOrEmpty(PendingWarning)
            ? warning
            : PendingWarning + "\n\n" + warning;
    }

    #region Authentication

    internal HttpClient InitializeHttpClient(OrchDriveInfo drive)
    {
        HttpClient ret;

        if (drive._psDrive.Proxy?.Enabled ?? false)
        {
            HttpClientHandler handler;
            IWebProxy iWebProxy;
            try
            {
                if (drive._psDrive.Proxy.UseDefaultWebProxy.GetValueOrDefault())
                {
                    iWebProxy = WebRequest.DefaultWebProxy;
                }
                else
                {
                    Uri proxyUri = new(drive._psDrive.Proxy.Url ?? "");

                    var proxy = new WebProxy
                    {
                        Address = proxyUri,
                        BypassProxyOnLocal = drive._psDrive.Proxy.BypassProxyOnLocal ?? true,
                        UseDefaultCredentials = drive._psDrive.Proxy.UseDefaultCredentials ?? false
                    };

                    if (drive._psDrive.Proxy?.Credentials is not null && !proxy.UseDefaultCredentials)
                    {
                        proxy.Credentials = new NetworkCredential(
                            userName: drive._psDrive.Proxy.Credentials.Username,
                            password: drive._psDrive.Proxy.Credentials.Password);
                    }

                    iWebProxy = proxy;
                }

                handler = new HttpClientHandler
                {
                    Proxy = iWebProxy,
                    UseProxy = true
                };

                // Ignore exceptions when SSL certificates are missing
                if (drive._psDrive.IgnoreSslErrors.GetValueOrDefault(false))
                {
                    handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Proxy: {ex.Message}", ex);
            }

            ret = new HttpClient(handler);
        }
        else
        {
            // Ignore exceptions when SSL certificates are missing
            if (drive._psDrive.IgnoreSslErrors.GetValueOrDefault(false))
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true,
                };
                ret = new HttpClient(handler);
            }
            else
            {
                ret = new HttpClient();
            }
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string userAgent = $"UiPathOrch/{version}";
        ret.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return ret;
    }

    public OrchAPISession(OrchDriveInfo drive)
    {
        _drive = drive;

        _httpClient = InitializeHttpClient(drive);

        _authManager = new OrchestratorAuthManager(drive, _httpClient);
        if (drive._psDrive.IsCloud)
        {
            _base_url = drive._psDrive.Root!;
            int slashIndex = _base_url.LastIndexOf('/');
            _base_url_identity = string.Concat(_base_url.AsSpan(0, slashIndex), "/identity_");
            _base_url_portal = string.Concat(_base_url.AsSpan(0, slashIndex), "/portal_");
        }
        else
        {
            // On-premises: use _baseUrl with tenancy path removed.
            // Tenancy is specified via the X-UIPATH-TenantName header.
            _base_url = _authManager.BaseUrl;
            _base_url_identity = _base_url + "/identity";
            _base_url_portal = _base_url + "/portal";
        }

        if (!string.IsNullOrEmpty(drive._psDrive.IdentityUrl))
        {
            _base_url_identity = drive._psDrive.IdentityUrl;
        }

        _drive = drive;
    }

    private readonly object _authLock = new();
    internal void ClearAuthentication()
    {
        lock (_authLock)
        {
            _isAuthenticated = false;
        }
    }

    internal void EnsureAuthenticated()
    {
        if (!_isAuthenticated)
        {
            lock (_authLock)
            {
                if (!_isAuthenticated)
                {
                    // Set initial token
                    string token = _authManager.RequestToken();
                    SetToken(token);

                    _isAuthenticated = true;
                    _expiryTime = DateTime.Now.AddHours(1);

                    if (ApiVersion is null && _drive is not null &&
                        (_drive._psDrive.Scope?.Contains("OR.Settings") ?? false))
                    {
                        try
                        {
                            var activitySettings = _drive.ActivitySettings.Get();
                            if (double.TryParse(activitySettings?.ApiVersion, out var version))
                            {
                                ApiVersion = version;
                            }
                        }
                        catch (Exception ex)
                        {
                            // ApiVersion stays null; the rest of the session falls back to a
                            // conservative default. Surface it to Debug so it can be diagnosed
                            // when an API behaves like an older version than expected.
                            System.Diagnostics.Debug.WriteLine($"ApiVersion fetch failed: {ex.Message}");
                        }
                    }
                }
            }
        }

        // Refresh the token if we are past 5 minutes before expiry
        DateTime now = DateTime.Now;
        if (now > _expiryTime.AddMinutes(-5))
        {
            lock (_authLock)
            {
                if (now > _expiryTime.AddMinutes(-5))
                {
                    SetToken(RenewAccessToken());
                    _expiryTime = now.AddHours(1);
                }
            }
        }
    }

    private string? RenewAccessToken()
    {
        try
        {
            return _authManager.RenewAccessToken();
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;
            // Diagnostic only — the exception is rethrown so the caller surfaces it via WriteError.
            // Console.WriteLine here would bypass PowerShell's structured streams and corrupt pipeline output.
            System.Diagnostics.Debug.WriteLine($"renew token failed on {_drive.NameColonSeparator}: {ex.Message}");
            throw;
        }
    }

    private void EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _isAuthenticated = false;
            }
            throw new HttpResponseException(GetBody(response), response);
        }
    }

    private void EnsureVersionSupport(float requiredVersion)
    {
        if (ApiVersion < requiredVersion)
        {
            //throw new InvalidOperationException($"Orchestrator API Version {ApiVersion:F1} does not support this operation. Required version: {requiredVersion:F1}.");
            throw new InvalidOperationException($"Orchestrator API Version {ApiVersion:F1} does not support this operation.");
        }
    }

    private void SetToken(string? access_token)
    {
        if (!string.IsNullOrEmpty(access_token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            if (disposing)
            {
                // Dispose managed resources
                _httpClient?.Dispose();
                _httpClientForBucketItem?.Dispose();
                limiter?.Dispose();

                // Dispose the async log writer
                DisposeAsyncLogWriter();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // No need to have destructor, since this class has no unmanaged resource.
    //~OrchAPISession()
    //{
    //    Dispose(false);
    //}

    #endregion

    #region Common Tools

    private static string GetBody(HttpResponseMessage response)
    {
        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(body))
        {
            return response.ReasonPhrase ?? "";
        }
        else
        {
            return body;
        }
    }

    private IEnumerable<T> GetEnumerable<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;

        //using var cancelHandler = new ConsoleCancelHandler();
        while (true)
        {
            //cancelHandler.Token.ThrowIfCancellationRequested();

            ulong top = Math.Min(first - total, 1000);
            if (top == 0) break;

            string url = $"{_base_url}{endPoint}?$top={top}&$skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            HttpBodyValues<T> body = JsonSerializer.Deserialize<HttpBodyValues<T>>(strBody);

            if (body is not null && body.value is not null && body.value.Length != 0)
            {
                foreach (var value in body.value!)
                {
                    yield return value;
                }
                if (body.value.Length < 1000)
                    break;
                total += (ulong)body.value.Length;
                skip += (ulong)body.value!.Length;
            }
            else
                yield break;
        }
    }

    private IEnumerable<T> GetEnumerableIdentity<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;

        //using var cancelHandler = new ConsoleCancelHandler();
        while (true)
        {
            //cancelHandler.Token.ThrowIfCancellationRequested();

            ulong top = Math.Min(first - total, 1000);
            if (top == 0) break;

            // Note: parameter names do not have a $ prefix. They are "top" and "skip", not "$top" and "$skip".
            string url = $"{_base_url_identity}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            //{
            //    Content = new StringContent("", Encoding.UTF8, @"application/json")
            //};
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            HttpBodyResults<T> body = JsonSerializer.Deserialize<HttpBodyResults<T>>(strBody);
            if (body is not null && body.results is not null && body.results.Length != 0)
            {
                foreach (var value in body.results)
                {
                    yield return value;
                    ++total;
                    if (total == first)
                        break;
                }
                if (body.results?.Length == 0 || total == first)
                    break;
                skip += (ulong)body.results!.Length;
            }
            else
                break;
        }
    }

    private static StringContent CreateEmptyContent() => new("", Encoding.UTF8, @"application/json");

    private IEnumerable<T> GetEnumerablePortal<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;

        //using var cancelHandler = new ConsoleCancelHandler();
        while (true)
        {
            //cancelHandler.Token.ThrowIfCancellationRequested();

            ulong top = Math.Min(first - total, 1000);
            if (top == 0) break;

            // Note: parameter names do not have a $ prefix. They are "top" and "skip", not "$top" and "$skip".
            string url = $"{_base_url_portal}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                // The following line is needed even when the content is empty.
                // Without it, the Get-OrchPmUser endpoint returns an error.
                // Almost all other endpoints work fine without this line, though.
                Content = CreateEmptyContent()
            };
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var body = JsonSerializer.Deserialize<HttpBodyResults<T>>(strBody);
            if (body is not null && body.results is not null && body.results.Length != 0)
            {
                foreach (var value in body.results)
                {
                    yield return value;
                    ++total;
                    if (total == first)
                        break;
                }
                if (body.results.Length < 1000 || total == first)
                    break;
                skip += (ulong)body.results.Length;
            }
            else
                break;
        }
    }

    // No pagination support
    private TColl? GetEnumerableWithoutPagingImpl<TColl>(string baseUrl, string endPoint, Int64? folderId = null, string? query = null)
    {
        endPoint = $"{baseUrl}{endPoint}?$count=false{query}";
        //endPoint = $"{baseUrl}{endPoint}{query}";

        var request = new HttpRequestMessage(HttpMethod.Get, endPoint);
        //{
        //    Content = new StringContent("", Encoding.UTF8, @"application/json")
        //};
        if (folderId.HasValue)
        {
            request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
        }

        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<TColl>(strBody);
    }

    private T[]? GetEnumerableWithoutPaging<T>(string endPoint, Int64? folderId = null, string? query = null)
    {
        var body = GetEnumerableWithoutPagingImpl<HttpBodyValues<T>>(_base_url, endPoint, folderId, query);
        return body?.value;
    }

    private T[]? GetEnumerableWithoutPagingIdentity<T>(string endPoint, Int64? folderId = null, string? query = null)
    {
        return GetEnumerableWithoutPagingImpl<T[]>(_base_url_identity, endPoint, folderId, query);
    }

    public string HttpRequestImpl(HttpMethod method, string baseUrl, string endPoint, Int64? folderId, string? payload = null)
    {
        string url = baseUrl + endPoint;
        var request = new HttpRequestMessage(method, url);
        if (payload is not null)
        {
            request.Content = new StringContent(payload, Encoding.UTF8, @"application/json");
        }

        if (folderId.HasValue)
        {
            request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
        }

        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return body;
    }

    public string HttpRequest(HttpMethod method, string endPoint, Int64? folderId, string payload)
    {
        return HttpRequestImpl(method, _base_url, endPoint, folderId, payload);
    }

    public string HttpRequestIdentity(HttpMethod method, string endPoint, Int64? folderId, string payload)
    {
        return HttpRequestImpl(method, _base_url_identity, endPoint, folderId, payload);
    }

    public string HttpRequestImpl(HttpMethod method, string baseUrl, string endPoint, Int64? folderId = null, object? payload = null)
    {
        if (payload is null)
        {
            return HttpRequestImpl(method, baseUrl, endPoint, folderId, (string?)null);
        }
        else
        {
            string strPayload = JsonSerializer.Serialize(payload, JsonTools.jsoWhenWritingNull);
            return HttpRequestImpl(method, baseUrl, endPoint, folderId, strPayload);
        }
    }

    public string HttpRequest(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null)
    {
        return HttpRequestImpl(method, _base_url, endPoint, folderId, payload);
    }

    public string HttpRequestIdentity(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null)
    {
        return HttpRequestImpl(method, _base_url_identity, endPoint, folderId, payload);
    }

    // For calling undocumented/private APIs
    public string HttpRequestPortal(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null)
    {
        return HttpRequestImpl(method, _base_url_portal, endPoint, folderId, payload);
    }

    public T? HttpRequest<T>(HttpMethod method, string endPoint, Int64? folderId = null, object? query = null)
    {
        // When query is already a string, call the string overload directly
        // to avoid double-serialization via JsonSerializer.Serialize(string).
        string body = query is string strQuery
            ? HttpRequest(method, endPoint, folderId, strQuery)
            : HttpRequest(method, endPoint, folderId, query);
        if (string.IsNullOrEmpty(body)) return default;
        return JsonSerializer.Deserialize<T>(body);
    }

    public T? HttpRequestIdentity<T>(HttpMethod method, string endPoint, Int64? folderId = null, object? query = null)
    {
        string body = HttpRequestIdentity(method, endPoint, folderId, query);
        return JsonSerializer.Deserialize<T>(body);
    }

    public T? HttpRequestPortal<T>(HttpMethod method, string endPoint, Int64? folderId = null, object? query = null)
    {
        string body = HttpRequestPortal(method, endPoint, folderId, query);
        return JsonSerializer.Deserialize<T>(body);
    }

    // Public hook used by Invoke-OrchApi: send a fully-prepared HttpRequestMessage through
    // the same pipeline (auth refresh, rate limiting, logging) without raising on non-success.
    // Caller owns disposal of the returned response.
    public HttpResponseMessage SendApiRequest(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return HttpClient_Send(request, cancellationToken: cancellationToken);
    }

    #endregion

    #region Settings
    public IEnumerable<Settings> GetSettings()
    {
        return GetEnumerable<Settings>("/odata/Settings");
    }

    public ActivitySettings? GetActivitySettings()
    {
        return HttpRequest<ActivitySettings>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetActivitySettings");
    }

    public UpdateSettings? GetUpdateSettings()
    {
        return HttpRequest<UpdateSettings>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetUpdateSettings");
    }

    public ExecutionSettingsConfiguration? GetExecutionSettings(int scope)
    {
        return HttpRequest<ExecutionSettingsConfiguration>(HttpMethod.Get, $"/odata/Settings/UiPath.Server.Configuration.OData.GetExecutionSettingsConfiguration(scope={scope})");
    }

    public void UpdateSettingsBulk(IEnumerable<Settings> settings)
    {
        var payload = new { settings = settings.Select(s => new { s.Name, s.Value }).ToArray() };
        HttpRequest(HttpMethod.Post, "/odata/Settings/UiPath.Server.Configuration.OData.UpdateBulk", null, payload);
    }

    public ResponseDictionary? GetWebSettings()
    {
        return HttpRequest<ResponseDictionary>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings");
    }

    public ResponseDictionary? GetAuthenticationSettings()
    {
        return HttpRequest<ResponseDictionary>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings");
    }

    public ODataValueOfString? GetConnectionString()
    {
        return HttpRequest<ODataValueOfString>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetConnectionString");
    }

    public License? GetLicenseSettings()
    {
        return HttpRequest<License>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetLicense");
    }

    #endregion

    #region Alert
    public IEnumerable<Alert> GetAlerts(string? query, ulong skip, ulong first)
    {
        try
        {
            // Because GetEnumerable uses lazy evaluation via yield return,
            // ToList() must be called here for immediate execution, otherwise try/catch will not work.
            return GetEnumerable<Alert>("/odata/Alerts", null, query, skip, first).ToList();
        }
        catch (Exception ex) when (ApiVersion >= 18)
        {
            throw new InvalidOperationException(
                $"The Alerts API has been deprecated since Orchestrator API version 18.0.", ex);
        }
    }
    #endregion

    #region Queues

    public IEnumerable<QueueDefinition> GetQueues(Int64 folderId)
    {
        return GetEnumerable<QueueDefinition>("/odata/QueueDefinitions", folderId);
    }

    public QueueDefinition? GetQueue(Int64 folderId, Int64 queueId)
    {
        // TODO: Which one should be called when ApiVersion is 17 or 18?
        if (ApiVersion >= 19)
        {
            return HttpRequest<QueueDefinition>(HttpMethod.Get, $"/odata/QueueDefinitions/UiPath.Server.Configuration.OData.GetQueue(id={queueId})", folderId);
        }
        else
        {
            var queue = HttpRequest<QueueDefinition>(HttpMethod.Get, $"/odata/QueueDefinitions({queueId})", folderId);
            if (queue is null) return null;

            if (16 <= ApiVersion) // && ApiVersion < 19)
            {
                try
                {
                    var retention = GetQueueRetention(folderId, queueId);
                    if (retention is not null)
                    {
                        queue.RetentionAction = retention.Action;
                        queue.RetentionPeriod = retention.Period;
                        queue.RetentionBucketId = retention.BucketId;
                    }
                }
                catch
                {
                    // Is it OK to swallow this?
                }
            }

            return queue;
        }
    }

    // This appears to be an undocumented API.
    // Would like to use it to display progress for the Copy-OrchQueueItem cmdlet, but...
    //public void ListQueues()
    //{
    //  GET "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.ListQueues"
    //}

    public QueueRetentionSetting? GetQueueRetention(Int64 folderId, Int64 queueId)
    {
        EnsureVersionSupport(14);
        return HttpRequest<QueueRetentionSetting>(HttpMethod.Get, $"/odata/QueueRetention({queueId})", folderId);
    }

    public QueueRetentionSetting? GetQueueRetention(Int64 folderId, Int64 queueId, string retentionType)
    {
        EnsureVersionSupport(14);
        return HttpRequest<QueueRetentionSetting>(HttpMethod.Get, $"/odata/QueueRetention({queueId})?retentionType={retentionType}", folderId);
    }

    public QueueDefinition? CreateQueue(Int64 folderId, QueueDefinition queue)
    {
        // Confirmed that StaleRetention is not present in the web interface for ApiVersion = 17
        if (ApiVersion < 19)
        {
            queue.StaleRetentionAction = null;
            queue.StaleRetentionPeriod = null;
            queue.StaleRetentionBucketId = null;
            queue.StaleRetentionBucketName = null;
        }

        // RetryAbandonedItems was added to QueueDefinitionDto in WebApi v18.0
        // (per swagger v15.0-v17.0 vs v18.0-v20.0). MSI 25.10 reports ApiVersion 17.0
        // and does not have the field; sending it triggers strict-deserialization failure
        // with "command must not be null" / "queueDef must not be null" (HTTP 400).
        if (ApiVersion < 18)
        {
            queue.RetryAbandonedItems = null;
        }

        // Verified on OC 22.10.1 (15.0) POST /odata/QueueDefinitions
        // Verified on OC 23.4.0 (16.0) POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue
        // Verified on OC 23.10.0 (17.0) POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue
        if (ApiVersion >= 16)
        {
            return HttpRequest<QueueDefinition>(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue", folderId, queue);
        }
        else
        {
            // TODO: Should the following only be done when ApiVersion < 19?
            // Should EditQueue() also perform the same processing?
            queue.RetentionAction = null;
            queue.RetentionPeriod = null;
            queue.RetentionBucketId = null;
            queue.RetentionBucketName = null;
            // Tags, Encrypted, RetryAbandonedItems, IsProcessInCurrentFolder,
            // FoldersCount: not present in v11-v13
            queue.Tags = null;
            queue.Encrypted = null;
            queue.RetryAbandonedItems = null;
            queue.IsProcessInCurrentFolder = null;
            queue.FoldersCount = null;
            return HttpRequest<QueueDefinition>(HttpMethod.Post, "/odata/QueueDefinitions", folderId, queue);
        }
    }

    public void EditQueue(Int64 folderId, QueueDefinition queue)
    {
        // Strip fields that are not in QueueDefinitionDto for the target ApiVersion;
        // see CreateQueue for the rationale.
        if (ApiVersion < 19)
        {
            queue.StaleRetentionAction = null;
            queue.StaleRetentionPeriod = null;
            queue.StaleRetentionBucketId = null;
            queue.StaleRetentionBucketName = null;
        }
        if (ApiVersion < 18)
        {
            queue.RetryAbandonedItems = null;
        }

        // Returns nothing
        HttpRequest(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.EditQueue", folderId, queue);
    }

    public void PutQueueDefinition(Int64 folderId, QueueDefinition queue)
    {
        if (ApiVersion < 19)
        {
            queue.StaleRetentionAction = null;
            queue.StaleRetentionPeriod = null;
            queue.StaleRetentionBucketId = null;
            queue.StaleRetentionBucketName = null;
        }
        if (ApiVersion < 18)
        {
            queue.RetryAbandonedItems = null;
        }

        HttpRequest(HttpMethod.Put, $"/odata/QueueDefinitions({queue.Id!.Value})", folderId, queue);
    }

    public void PutQueueRetention(Int64 folderId, Int64 queueId, QueueRetentionSetting setting)
    {
        HttpRequest(HttpMethod.Put, $"/odata/QueueRetention({queueId})", folderId, setting);
    }

    public void RemoveQueue(Int64 folderId, Int64 queueId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/QueueDefinitions({queueId})", folderId);
    }

    public AccessibleFoldersDto? GetFoldersForQueue(Int64 folderId, Int64 queueId)
    {
        return HttpRequest<AccessibleFoldersDto>(HttpMethod.Get, $"/odata/QueueDefinitions/UiPath.Server.Configuration.OData.GetFoldersForQueue(id={queueId})", folderId);
    }

    public void ShareQueuesToFolders(Int64 folderId, List<Int64> queueIds, List<Int64> toAddFolderIds, List<Int64> toRemoveFolderIds)
    {
        QueueFoldersShare foldersShare = new();
        foldersShare.QueueIds = queueIds;
        foldersShare.ToAddFolderIds = toAddFolderIds;
        foldersShare.ToRemoveFolderIds = toRemoveFolderIds;

        HttpRequest(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.ShareToFolders", folderId, foldersShare);
    }


    public void AddQueueItem(Int64 folderId, string queueName, Dictionary<string, object> specificContent, QueuePriority priority = QueuePriority.Normal)
    {
        var payload = new
        {
            itemData = new QueueItemData
            {
                Name = queueName,
                Priority = priority.ToString(),
                SpecificContent = specificContent,
            }
        };

        HttpRequest(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.AddQueueItem", folderId, payload);
    }

    // filter must be passed in the format "&$filter=()"
    public IEnumerable<QueueItem> GetQueueItems(Int64 folderId, string? filter, ulong skip, ulong first, string? orderBy = null, bool orderAscending = false)
    {
        if (string.IsNullOrEmpty(orderBy)) orderBy = "EndProcessing";
        string order;
        if (orderAscending)
        {
            order = $"&$orderby={orderBy} asc";
        }
        else
        {
            order = $"&$orderby={orderBy} desc";
        }

        return GetEnumerable<QueueItem>("/odata/QueueItems", folderId, $"{filter}{order}&$expand=Robot,ReviewerUser&orderby=Id%20desc", skip, first);
    }

    public QueueItem? GetQueueItemById(Int64 folderId, Int64 id)
    {
        return HttpRequest<QueueItem>(HttpMethod.Get, $"/odata/QueueItems({id})", folderId);
    }

    public BulkOperationResponse? RetryQueueItem(Int64 folderId, IEnumerable<RetryQueueItem> items)
    {
        RetryQueueItemRequest payload = new()
        {
            queueItems = items,
            status = "Retried"
        };
        return HttpRequest<BulkOperationResponse>(HttpMethod.Post, "/odata/QueueItems/UiPathODataSvc.SetItemReviewStatus", folderId, payload);
    }

    public QueueItem? PostQueueItemComments(Int64 folderId, Int64 queueItemId, string comment)
    {
        QueueItemComment payload = new()
        {
            QueueItemId = queueItemId,
            Text = comment
        };
        return HttpRequest<QueueItem>(HttpMethod.Post, "/odata/QueueItemComments", folderId, payload);
    }

    public BulkOperationResponseOfInt64? DeleteBulkQueueItem(Int64 folderId, QueueItemDeleteBulkRequest payload)
    {
        return HttpRequest<BulkOperationResponseOfInt64>(HttpMethod.Post, "/odata/QueueItems/UiPathODataSvc.DeleteBulk", folderId, payload);
    }

    public IEnumerable<RobotsFromFolderModel> GetRobotsFromFolder(Int64 folderId)
    {
        return GetEnumerable<RobotsFromFolderModel>($"/odata/Robots/UiPath.Server.Configuration.OData.GetRobotsFromFolder(folderId={folderId})");
    }

    // Used when creating/updating machines. Note the filter is set. This filter text might be better held on the cache side.
    public IEnumerable<ExtendedRobot> FindAllRobotsAcrossFolders()
    {
        return GetEnumerable<ExtendedRobot>($"/odata/Robots/UiPath.Server.Configuration.OData.FindAllAcrossFolders", null,
            "&$filter=Type%20eq%20%272%27%20and%20ProvisionType%20eq%20%271%27&$expand=User");
    }

    public IEnumerable<SimpleUser> GetReviewers(Int64 folderId)
    {
        return GetEnumerable<SimpleUser>("/odata/QueueItems/UiPath.Server.Configuration.OData.GetReviewers()", folderId, "&$filter=(Type%20eq%20%27DirectoryUser%27)");
    }

    public BulkOperationResponseDtoOfFailedQueueItem? BulkAddQueueItem(Int64 folderId, BulkAddQueueItemsRequest payload)
    {
        return HttpRequest<BulkOperationResponseDtoOfFailedQueueItem>(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.BulkAddQueueItems", folderId, payload);
    }

    public BulkOperationResponseDtoOfFailedQueueItem? BulkAddQueueItem(Int64 folderId, string payload)
    {
        return HttpRequest<BulkOperationResponseDtoOfFailedQueueItem>(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.BulkAddQueueItems", folderId, payload);
    }

    // This API can only be called from Robot...
    //public QueueItem? StartTransaction(Int64 folderId, TransactionData payload)
    //{
    //    QueuesStartTransactionRequest pl = new()
    //    {
    //        transactionData = payload
    //    };
    //    var ret = HttpRequest(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.StartTransaction", folderId, pl);
    //    return null;
    //}

    #endregion

    #region CredentialStores

    // Cannot retrieve Type. It is needed when posting, though...
    public IEnumerable<CredentialStore> GetCredentialStores()
    {
        //return GetEnumerable<CredentialStore>("/odata/CredentialStores?$expand=Type");
        return GetEnumerable<CredentialStore>("/odata/CredentialStores");
    }

    public CredentialStore? GetCredentialStore(Int64 credentialStoreId)
    {
        //return GetEnumerable<CredentialStore>("/odata/CredentialStores?$expand=Type");
        return HttpRequest<CredentialStore>(HttpMethod.Get, $"/odata/CredentialStores({credentialStoreId})");
    }

    public CredentialStore? CreateCredentialStore(CredentialStore credentialStore)
    {
        return HttpRequest<CredentialStore>(HttpMethod.Post, "/odata/CredentialStores", null, credentialStore);
    }

    public void RemoveCredentialStore(Int64 credentialStoreId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/CredentialStores({credentialStoreId})?forceDelete=true");
    }

    #endregion

    #region Webhooks
    public IEnumerable<Webhook> GetWebhooks()
    {
        return GetEnumerable<Webhook>("/odata/Webhooks");
    }

    // Returns empty
    public void RemoveWebhooks(Int64 webhookId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Webhooks({webhookId})");
    }

    public Webhook? CreateWebhook(Webhook webhook)
    {
        // Name, Description, Key: added in v16.0
        if (ApiVersion < 16)
        {
            webhook.Name = null;
            webhook.Description = null;
            webhook.Key = null;
        }
        return HttpRequest<Webhook>(HttpMethod.Post, "/odata/Webhooks", null, webhook);
    }

    public Webhook? PatchWebhook(Int64 webhookId, Webhook webhook)
    {
        return HttpRequest<Webhook>(HttpMethod.Patch, $"/odata/Webhooks({webhookId})", null, webhook);
    }

    public IEnumerable<WebhookEventType> GetWebhookEventTypes()
    {
        return GetEnumerable<WebhookEventType>("/odata/Webhooks/UiPath.Server.Configuration.OData.GetEventTypes");
    }

    public WebhookPingResult? PingWebhook(Int64 webhookId)
    {
        return HttpRequest<WebhookPingResult>(HttpMethod.Post, $"/odata/Webhooks({webhookId})/UiPath.Server.Configuration.OData.Ping");
    }
    #endregion

    #region BusinessRules

    public IEnumerable<BusinessRule> GetBusinessRules(Int64 folderId)
    {
        return GetEnumerable<BusinessRule>("/odata/BusinessRules", folderId);
    }

    public BusinessRule? GetBusinessRule(Int64 folderId, string businessRuleKey)
    {
        return HttpRequest<BusinessRule>(HttpMethod.Get, $"/odata/BusinessRules({businessRuleKey})", folderId);
    }

    public void RemoveBusinessRule(Int64 folderId, string businessRuleKey)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/BusinessRules({businessRuleKey})", folderId);
    }

    public BusinessRule? CreateBusinessRule(Int64 folderId, BusinessRule businessRule, string fileName, byte[] file)
    {
        // Browser HAR shows POST /odata/BusinessRules with multipart/form-data:
        //   businessRule (text)  : JSON-serialized BusinessRuleDto (PascalCase fields)
        //   file (binary)        : the rule definition file (.dmn), Content-Type application/octet-stream
        // X-UIPATH-OrganizationUnitId carries the folder context.
        return PostMultipartBusinessRule(HttpMethod.Post, "/odata/BusinessRules", folderId, businessRule, fileName, file);
    }

    public void UpdateBusinessRule(Int64 folderId, string businessRuleId, BusinessRule businessRule, string? fileName = null, byte[]? file = null)
    {
        // PUT /odata/BusinessRules({id}). Same multipart shape as POST, but `file` is optional —
        // omitting it preserves the existing rule definition while updating metadata only.
        PostMultipartBusinessRule(HttpMethod.Put, $"/odata/BusinessRules({businessRuleId})", folderId, businessRule, fileName, file);
    }

    private BusinessRule? PostMultipartBusinessRule(HttpMethod method, string endpoint, Int64 folderId, BusinessRule businessRule, string? fileName, byte[]? file)
    {
        using var content = new MultipartFormDataContent("----UiPathOrchBoundary");

        string brJson = JsonSerializer.Serialize(businessRule, JsonTools.jsoWhenWritingNull);
        content.Add(new StringContent(brJson), "businessRule");

        if (file is not null)
        {
            var fileContent = new ByteArrayContent(file);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName ?? "rule.dmn");
        }

        var request = new HttpRequestMessage(method, _base_url + endpoint) { Content = content };
        request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());

        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(body)) return null;
        return JsonSerializer.Deserialize<BusinessRule>(body);
    }

    #endregion

    #region Environent
    public IEnumerable<PowerShell.Entities.Environment> GetEnvironments(Int64 folderId)
    {
        return GetEnumerable<PowerShell.Entities.Environment>("/odata/Environments", folderId, "&$expand=Robots");
    }
    #endregion

    #region Folders

    public IEnumerable<Folder> GetFolders()
    {
        return GetEnumerable<Folder>("/odata/Folders");
    }

    // This endpoint cannot be called from OAuth external app..
    //public void GetAllFoldersForCurrentUser()
    //{
    //    HttpRequest(HttpMethod.Get, "/api/FoldersNavigation/GetAllFoldersForCurrentUser");
    //}

    // This endpoint cannot be called from OAuth external app..
    //public string GetAllRolesForUser(string userName, string? type = null)
    //{
    //    type = "User";
    //    return HttpRequest(HttpMethod.Get, $"/api/FoldersNavigation/GetAllRolesForUser?username={userName}&type={type}");
    //}

    public Folder? GetFolderById(Int64 Id)
    {
        return HttpRequest<Folder>(HttpMethod.Get, $"/odata/Folders({Id})");
    }

    public Folder? CreateFolder(string displayName, string? description, string? feedType, Int64? parentFolderId = null)
    {
        Folder payload = new()
        {
            DisplayName = displayName,
            Description = description,
            FeedType = feedType ?? "Processes",
            PermissionModel = "FineGrained",
            ProvisionType = "Automatic",
            ParentId = parentFolderId
        };

        var newFolder = HttpRequest<Folder>(HttpMethod.Post, "/odata/Folders", null, payload);
        if (newFolder is not null)
        {
            newFolder.FeedType = feedType; // OC Issue: FeedType always becomes Process for some reason, so fix it
        }
        return newFolder;
    }

    public void RemoveFolder(Int64 folderId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Folders({folderId})");
    }

    public void EditFolder(Folder folder, string displayName, string? description = null)
    {
        Folder payload = new()
        {
            Id = folder.Id,
            DisplayName = displayName,
            Description = folder.Description,
            ParentId = folder.ParentId,
            FeedType = folder.FeedType,
            PermissionModel = folder.PermissionModel,
            ProvisionType = folder.ProvisionType
        };

        if (description is not null)
        {
            payload.Description = description;
        }

        HttpRequest(HttpMethod.Put, $"/odata/Folders({folder.Id})", null, payload);
    }

    public void MoveFolder(Int64 folderId, Int64? parentFolderId = null)
    {
        string endPoint = $"/odata/Folders({folderId})/UiPath.Server.Configuration.OData.MoveFolder";
        if (parentFolderId is not null)
        {
            endPoint += $"?targetParentId={parentFolderId}";
        }

        // Returns nothing
        HttpRequest(HttpMethod.Put, endPoint);
    }

    public LibraryFeed[]? GetLibraryFeeds()
    {
        return HttpRequest<LibraryFeed[]>(HttpMethod.Get, "/api/PackageFeeds/GetLibraryFeeds");
    }

    // This method should not return ""
    // The caller needs to convert null to "" as necessary
    // (e.g., when used as a dictionary key)
    public string? GetFolderFeedId(Int64? folderId)
    {
        if (folderId is null || folderId == 0)
            return null;
        string endPoint = $"/api/PackageFeeds/GetFolderFeed?folderId={folderId}";
        string feedId = HttpRequest(HttpMethod.Get, endPoint, null);
        if (feedId is null || feedId == "null")
            return null;
        feedId = feedId.TrimStart('"').TrimEnd('"');
        return feedId;
    }

    public IEnumerable<UserRoles> GetUsersForFolder(Int64 folderId, bool includeInherited = false)
    {
        return GetEnumerable<UserRoles>($"/odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={folderId},includeInherited={includeInherited.ToString().ToLower()})", null, "&includeAlertsEnabled=true");
    }

    private class FolderAssignDomainUserRequest
    {
        public DomainUserAssignment? assignment { set; get; }
    }

    public void AssignDomainUser(DomainUserAssignment user)
    {
        FolderAssignDomainUserRequest request = new()
        {
            assignment = user
        };
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.AssignDomainUser", null, request);
    }

    public void AssignDirectoryUser(DomainUserAssignment user)
    {
        FolderAssignDomainUserRequest request = new()
        {
            assignment = user
        };
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.AssignDirectoryUser", null, request);
    }

    public void AssignUser(Int64 folderId, Int64 userId, IEnumerable<Int64>? roleIds)
    {
        var payload = new Dictionary<string, object?>();
        payload["assignments"] = new Dictionary<string, object?>()
        {
            { "UserIds", new Int64[] { userId } },
            { "RolesPerFolder",
                new object[] { new Dictionary<string, object?>()
                    {
                        { "FolderId", folderId },
                        { "RoleIds", roleIds }
                    }
                }
            }
        };

        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.AssignUsers", null, payload);
    }

    public IEnumerable<MachineFolder> GetMachinesAssignedTo(Int64 folderId, string? query = null)
    {
        return GetEnumerable<MachineFolder>($"/odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder(key={folderId})", null, query);
    }

    // FolderMachineInheritDto
    private class FolderMachineInherit(long? machineId, long? folderId, bool? inheritEnabled)
    {
        public Int64? MachineId { get; set; } = machineId;
        public Int64? FolderId { get; set; } = folderId;
        public bool? InheritEnabled { get; set; } = inheritEnabled;
    }

    // Verified on ApiVersion = 15
    public void SetFolderMachineInherit(Int64 folderId, Int64 machineId, bool enabled)
    {
        FolderMachineInherit payload = new(machineId, folderId, enabled);
        // Returns ""
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.ToggleFolderMachineInherit", folderId, payload);
    }

    // TODO: Specifying the filter here, but that should be fine... It might be better to implement on the cache side later.
    public IEnumerable<ExtendedRobot> GetFolderRobots(Int64 folderId, MachineFolder machine)
    {
        return GetEnumerable<ExtendedRobot>($"/odata/Robots/UiPath.Server.Configuration.OData.GetFolderRobots(folderId={folderId},machineId={machine.Id})",
            null,
            "&$filter=Type eq '2' and ProvisionType eq 'Automatic'&$expand=User");
    }

    public IEnumerable<RobotUser> GetMachineRobots(Int64 folderId, MachineFolder machine)
    {
        return GetEnumerable<RobotUser>($"/odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machine.Id})");
    }

    public void SetMachineRobots(SetMachineRobotsCmd cmd)
    {
        // Returns nothing
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots", null, cmd);
    }

    public IEnumerable<UserRobots> GetUserRobots(Int64 folderId)
    {
        return GetEnumerable<UserRobots>("/odata/Sessions/UiPath.Server.Configuration.OData.GetUserRobots", folderId, "&$filter=startswith(Robot/Username,'autogen\\') eq false and Robot/Username ne ''");
    }

    public void AddMachinesToFolder(Int64 folderId, IEnumerable<Int64> machineIds)
    {
        UpdateMachinesToFolderAssociationsRequest payload = new()
        {
            associations = new()
            {
                FolderId = folderId,
                AddedMachineIds = machineIds.ToArray(),
                RemovedMachineIds = []
            }
        };

        // Returns nothing
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations", folderId, payload);
    }

    // TODO: This API might be deprecated. Automation Cloud calls a different API.
    public void UnassignMachinesFromFolder(Int64 folderId, IEnumerable<Int64> machineIds)
    {
        UpdateMachinesToFolderAssociationsRequest payload = new()
        {
            associations = new()
            {
                FolderId = folderId,
                AddedMachineIds = [],
                RemovedMachineIds = machineIds.ToArray()
            }
        };

        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations", folderId, payload);
    }

    #endregion

    public AvailableVersions? GetAvailableVersions()
    {
        return HttpRequest<AvailableVersions>(HttpMethod.Get, "/api/UpdateServer/GetAvailableVersions?onlyLatestPatchVersions=true");
    }

    #region PersonalWorkspaces

    public PersonalWorkspace? GetPersonalWorkspace()
    {
        return HttpRequest<PersonalWorkspace>(HttpMethod.Get, "/odata/PersonalWorkspaces/UiPath.Server.Configuration.OData.GetPersonalWorkspace");
    }

    public IEnumerable<PersonalWorkspace> GetPersonalWorkspaces()
    {
        return GetEnumerable<PersonalWorkspace>("/odata/PersonalWorkspaces");
    }

    // Currently, this API cannot be called because it does not support OAuth.
    //public void StartExploringPersonalWorkspace(Int64? folderId)
    //{
    //    string body = HttpRequest(HttpMethod.Get, $"/odata/PersonalWorkspaces({folderId})/UiPath.Server.Configuration.OData.StartExploring");
    //}

    public EntitiesSummary? GetEntitiesSummary(Int64 folderId)
    {
        return HttpRequest<EntitiesSummary>(HttpMethod.Get, $"/odata/Folders/UiPath.Server.Configuration.OData.GetEntitiesSummary(folderId={folderId})?includeShared=true");
    }

    #endregion

    #region Jobs

    public IEnumerable<Job> GetJobs(Int64 folderId, string? filter, ulong skip, ulong first, string? orderBy, bool orderAscending)
    {
        if (string.IsNullOrEmpty(orderBy)) orderBy = "CreationTime";
        string order;
        if (orderAscending)
        {
            order = $"&$orderby={orderBy} asc";
        }
        else
        {
            order = $"&$orderby={orderBy} desc";
        }

        string expand;
        // TODO: Is this number correct?
        // Confirmed that Machine must not be included when ApiVersion == 11
        // Confirmed that Machine can be included when ApiVersion == 13
        if (ApiVersion < 12)
        {
            expand = "&$expand=Robot,Release";
        }
        else
        {
            expand = "&$expand=Robot,Machine,Release";
        }

        return GetEnumerable<Job>("/odata/Jobs", folderId, $"{filter}{expand}{order}", skip, first);
    }

    public Job? GetJob(Int64 folderId, Int64 jobId)
    {
        return HttpRequest<Job>(HttpMethod.Get, $"/odata/Jobs({jobId})?$expand=Robot,Machine,Release", folderId);
    }

    public MachineRuntime[] GetRuntimesForFolder(Int64 folderId)
    {
        var ret = GetEnumerableWithoutPaging<MachineRuntime>($"/odata/Machines/UiPath.Server.Configuration.OData.GetRuntimesForFolder(folderId={folderId})", folderId);
        if (ret is null) return [];
        return ret;
    }

    public Job[] StartJobs(Int64 folderId, string processKey, string? runtimeType, int? jobsCount, string? inputArguments)
    {
        StartProcess sp = new()
        {
            ReleaseKey = processKey,
            Strategy = "ModernJobsCount",
            RuntimeType = runtimeType,
            JobsCount = jobsCount,
            InputArguments = inputArguments
        };

        Dictionary<string, object> payload = new() {
            { "startInfo", sp }
        };

        var httpBody = HttpRequest<HttpBodyValues<Job>>(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.StartJobs", folderId, payload);
        return httpBody?.value ?? [];
    }

    public void StopJobs(Int64 folderId, IEnumerable<Int64> jobIds, bool force = false)
    {
        var payload = new { strategy = force ? "2" : "1", jobIds };

        HttpRequest(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.StopJobs", folderId, payload);
    }

    public Job? RestartJob(Int64 folderId, Int64 jobId)
    {
        var payload = new { jobId };
        return HttpRequest<Job>(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.RestartJob", folderId, payload);
    }

    public Job? ResumeJob(Int64 folderId, string jobKey)
    {
        var payload = new { jobKey };
        return HttpRequest<Job>(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.ResumeJob", folderId, payload);
    }

    //public string? StartRemoteControl(Int64 folderId, string jobKey)
    //{
    //    var payload = new Dictionary<string, string>();
    //    payload["JobKey"] = jobKey;

    //    var res = HttpRequest<RemoteControlStart>(HttpMethod.Post, "/api/RemoteControl/Start", folderId, payload);
    //    return res!.uri;
    //}

    #endregion

    #region Machines

    public IEnumerable<ExtendedMachine> GetMachines(string? query = null)
    {
        return GetEnumerable<ExtendedMachine>("/odata/Machines", null, query);
    }

    public CreatedMachine? AddMachine(ExtendedMachine machine) //, IEnumerable<RobotUser> robotUsers)
    {
        // Strip properties not supported by older API versions.
        // AutomationCloudTestAutomationSlots: added in v16.0
        // AutomationCloudSlots, AutomationType, TargetFramework: added in v15.0
        if (ApiVersion < 16)
        {
            machine.AutomationCloudTestAutomationSlots = null;
        }
        if (ApiVersion < 15)
        {
            machine.AutomationCloudSlots = null;
            machine.AutomationType = null;
            machine.TargetFramework = null;
            machine.Tags = null;
        }
        // RobotUsers, UpdatePolicy, MaintenanceWindow: not supported on v11
        if (ApiVersion < 13)
        {
            machine.RobotUsers = null;
            machine.UpdatePolicy = null;
            machine.MaintenanceWindow = null;
        }
        return HttpRequest<CreatedMachine>(HttpMethod.Post, "/odata/Machines", null, machine);
    }

    public MachineClientSecretResponse[]? GetMachineClientSecret(string licenseKey)
    {
        return HttpRequest<MachineClientSecretResponse[]>(HttpMethod.Get, $"/api/clientsecrets/{licenseKey}");
    }

    public MachineClientSecretResponse? AddMachineClientSecret(string licenseKey)
    {
        return HttpRequest<MachineClientSecretResponse>(HttpMethod.Post, $"/api/clientsecrets/{licenseKey}");
    }

    public void DeleteMachineClientSecret(string secretId)
    {
        HttpRequest(HttpMethod.Delete, $"/api/clientsecrets/{secretId}");
    }

    public void PatchMachine(ExtendedMachine machine)
    {
        // Returns an empty string
        HttpRequest(HttpMethod.Patch, $"/odata/Machines({machine.Id!.Value})", null, machine);
    }

    public void RemoveMachine(Int64 machineId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Machines({machineId})");
    }

    public void RemoveMachines(IEnumerable<Int64> machineIds)
    {
        // POST is correct for some reason.
        HttpRequest(HttpMethod.Post, $"/odata/Machines/UiPath.Server.Configuration.OData.DeleteBulk", null, new { machineIds });
    }

    #endregion

    #region Packages

    public IEnumerable<Library> GetLibraries(string? feedId = null)
    {
        //return GetEnumerable<Library>("/odata/Libraries?$orderby=Id%20desc"); // doesn't work for some reason?
        return GetEnumerable<Library>("/odata/Libraries", null, feedId is null ? null : $"&feedId={feedId}");
    }

    public IEnumerable<Package> GetPackages(string? feedId = null)
    {
        string query = "";
        if (!string.IsNullOrEmpty(feedId))
        {
            query = $"&feedId={feedId}";
        }
        return GetEnumerable<Package>("/odata/Processes", null, query);
    }

    public IEnumerable<LibraryVersion> GetLibraryVersions(string libraryId, string? feedId = null)
    {
        string endPoint = $"/odata/Libraries/UiPath.Server.Configuration.OData.GetVersions(packageId='{HttpUtility.UrlEncode(libraryId)}')";
        return GetEnumerable<LibraryVersion>(endPoint, null, feedId is null ? null : $"&feedId={feedId}");
    }

    public IEnumerable<Package> GetPackageVersions(string? feedId, string processId)
    {
        string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions(processId='{HttpUtility.UrlEncode(processId)}')";
        string query = null;
        if (feedId is not null)
        {
            query = $"&feedId={feedId}";
        }
        return GetEnumerable<Package>(endPoint, null, query);
    }

    public PackageEntryPoint? GetPackageMainEntryPoint(string? feedId, string packageId, string packageVersion)
    {
        if (ApiVersion < 12) return null; // Confirmed it returns Not Found on 11.1. TODO: What about 12+? Verify with New-OrchProcess.

        //string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetPackageMainEntryPoint(key='{HttpUtility.UrlEncode(packageId)}:{packageVersion}')";

        string key = $"{packageId}:{packageVersion}";
        string encodedKey = HttpUtility.UrlEncode(key);
        string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetPackageMainEntryPoint(key='{encodedKey}')";
        if (!string.IsNullOrEmpty(feedId))
        {
            endPoint += $"?feedId={feedId}";
        }
        return HttpRequest<PackageEntryPoint>(HttpMethod.Get, endPoint);
    }

    public IEnumerable<PackageEntryPoint> GetPackageEntryPoints(string? feedId, string packageId, string packageVersion)
    {
        // Confirmed Not Found on 11.1 (OC 20.10); the action endpoint was added in v15.0.
        // Mirror the GetPackageMainEntryPoint < 12 guard - callers degrade to "no entry
        // point metadata available" instead of a hard failure.
        if (ApiVersion < 12) return [];

        string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetPackageEntryPoints(key='{HttpUtility.UrlEncode(packageId)}:{packageVersion}')";
        if (!string.IsNullOrEmpty(feedId))
        {
            return GetEnumerable<PackageEntryPoint>(endPoint, null, $"&feedId={feedId}");
        }
        else
        {
            return GetEnumerable<PackageEntryPoint>(endPoint);
        }
    }

    public void RemoveLibrary(string libraryId, string libraryVersion)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Libraries('{HttpUtility.UrlEncode(libraryId)}:{libraryVersion}')");
    }

    public void RemovePackage(string processId, string processVersion, string? feedId = null)
    {
        if (!string.IsNullOrEmpty(feedId))
        {
            HttpRequest(HttpMethod.Delete, $"/odata/Processes('{HttpUtility.UrlEncode(processId)}:{processVersion}')?feedId={feedId}");
        }
        else
        {
            HttpRequest(HttpMethod.Delete, $"/odata/Processes('{HttpUtility.UrlEncode(processId)}:{processVersion}')");
        }
    }

    public BulkItemDtoOfString? UploadLibrary(string fileName, byte[] file)
    {
        // Create MultipartFormDataContent
        using var content = new MultipartFormDataContent("----UiPathOrchBoundary");

        // Read file contents as ByteArrayContent
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // Add file contents to the content
        content.Add(fileContent, "uploads[]", fileName);

        //var request = new HttpRequestMessage(HttpMethod.Post, $"/odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage()?feedId={feedId}");
        var request = new HttpRequestMessage(HttpMethod.Post, _base_url + "/odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage")
        {
            Content = content
        };

        // Send the HTTP POST request and get the response
        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var objBody = JsonSerializer.Deserialize<HttpBodyValues<BulkItemDtoOfString>>(body);
        return objBody?.value?[0];
    }

    public BulkItemDtoOfString? UploadLibrary(string libraryFilePath)
    {
        var fileName = System.IO.Path.GetFileName(libraryFilePath);
        var fileContent = File.ReadAllBytes(libraryFilePath);
        return UploadLibrary(fileName, fileContent);
    }

    public BulkItemDtoOfString? UploadPackage(string? feedId, string fileName, byte[] file)
    {
        // Create MultipartFormDataContent
        using var content = new MultipartFormDataContent("----UiPathOrchBoundary");
        // Add file information to send

        // Read file contents as ByteArrayContent
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // Add file contents to the content
        content.Add(fileContent, "uploads[]", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, _base_url + $"/odata/Processes/UiPath.Server.Configuration.OData.UploadPackage()?feedId={feedId}")
        {
            Content = content
        };

        // Send the HTTP POST request and get the response
        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        // Read the response content
        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var objBody = JsonSerializer.Deserialize<HttpBodyValues<BulkItemDtoOfString>>(body);
        return objBody?.value?[0];
    }

    public BulkItemDtoOfString? UploadPackage(string? feedId, string packageFilePath)
    {
        var fileName = System.IO.Path.GetFileName(packageFilePath);
        var fileContent = File.ReadAllBytes(packageFilePath);
        return UploadPackage(feedId, fileName, fileContent);
    }

    public (string? FileName, byte[] FileContent) DownloadLibrary(string libraryId, string libraryVersion)
    {
        string url = _base_url + $"/odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='{HttpUtility.UrlEncode(libraryId)}:{libraryVersion}')";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        var contentDisposition = response.Content.Headers.ContentDisposition;

        string? ret = null;
        if (contentDisposition is not null)
        {
            // Prefer using "filename*"
            var fileNameStar = contentDisposition.FileNameStar;
            if (!string.IsNullOrEmpty(fileNameStar))
            {
                ret = fileNameStar;
            }
            else
            {
                var fileName = contentDisposition.FileName;
                if (!string.IsNullOrEmpty(fileName))
                {
                    ret = fileName;
                }
            }
        }

        // Await the result of ReadAsByteArrayAsync
        var responseBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        return (ret, responseBytes);
    }

    public (string? FileName, byte[] FileContent) DownloadPackage(string feedId, string packageId, string packageVersion)
    {
        string url = _base_url + $"/odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{HttpUtility.UrlEncode(packageId)}:{packageVersion}')";
        if (!string.IsNullOrEmpty(feedId))
        {
            url += $"?feedId={feedId}";
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        var contentDisposition = response.Content.Headers.ContentDisposition;

        string? ret = null;
        if (contentDisposition is not null)
        {
            // Prefer using "filename*"
            var fileNameStar = contentDisposition.FileNameStar;
            if (!string.IsNullOrEmpty(fileNameStar))
            {
                ret = fileNameStar;
            }
            else
            {
                var fileName = contentDisposition.FileName;
                if (!string.IsNullOrEmpty(fileName))
                {
                    ret = fileName;
                }
            }
        }

        var responseBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        return (ret, responseBytes);
    }

    #endregion

    #region Process

    public IEnumerable<Release> ListReleases(Int64 folderId)
    {
        return GetEnumerable<Release>("/odata/Releases/UiPath.Server.Configuration.OData.ListReleases", folderId, "&$filter=ProcessType%20eq%20%271%27");
    }

    public IEnumerable<Release> GetReleases(Int64 folderId, string? query = null)
    {
        return GetEnumerable<Release>("/odata/Releases", folderId, query);
    }

    public Release? GetReleaseById(Int64 folderId, Int64 releaseId, string? query = null)
    {
        if (ApiVersion >= 19)
        {
            return HttpRequest<Release>(HttpMethod.Get, $"/odata/Releases({releaseId})/UiPath.Server.Configuration.OData.GetRelease", folderId, query);
        }
        else
        {
            return HttpRequest<Release>(HttpMethod.Get, $"/odata/Releases({releaseId})", folderId, query);
        }
    }

    public IEnumerable<SubtypedPackageResource> GetReleaseRequirement(Int64 folderId, Release release)
    {
        var result = HttpRequest<HttpBodyValues<SubtypedPackageResource>>(HttpMethod.Get, $"/odata/Releases({release.Id})/UiPath.Server.Configuration.OData.GetResources(processKey='{HttpUtility.UrlEncode(release.ProcessKey)}:{release.ProcessVersion}')", folderId);
        return result?.value ?? [];
    }

    // Strip ReleaseDto fields that the target ApiVersion does not have. Sending unknown
    // fields to /odata/Releases (or its CreateRelease action) triggers strict body
    // deserialization and HTTP 400 ("release/command must not be null"). System.Text.Json
    // is configured WhenWritingNull, so nulling a field excludes it from the JSON entirely.
    //
    // Empirical findings (OData $metadata + POST probe per OC build) drive these thresholds;
    // ApiVersion alone is not a reliable schema indicator (22.10.1 reports v15 but its
    // ReleaseDto already has RobotSize that the v15.0 swagger snapshot lacks).
    private void StripReleaseFieldsForApiVersion(Release release)
    {
        // Fields added in v19.0
        if (ApiVersion < 19)
        {
            release.EnvironmentVariables = null;
            release.MinRequiredRobotVersion = null;
            release.FolderKey = null;
            release.StaleRetentionPeriod = null;
            release.StaleRetentionAction = null;
            release.StaleRetentionBucketId = null;
            if (release.ProcessSettings is not null)
            {
                release.ProcessSettings.AutopilotForRobots = null;
            }
        }
        // Fields added in v17.0 (verified rejected by 22.10.1 / ApiVersion 15)
        if (ApiVersion < 17)
        {
            release.HiddenForAttendedUser = null;
            release.EntryPointPath = null;
        }
        // Fields added in v16.0 (verified rejected by 22.10.1 / ApiVersion 15).
        // RobotSize: same v15-era split as the ProcessSchedule v16 fields - 22.10.1
        // has it, 22.4.4 does not, both report ApiVersion 15. Bundled here under < 16.
        if (ApiVersion < 16)
        {
            release.RemoteControlAccess = null;
            release.VideoRecordingSettings = null;
            release.AutomationHubIdeaUrl = null;
            release.RobotSize = null;
        }
    }

    public Release? PostRelease(Int64 folderId, Release release)
    {
        if (release.RetentionPeriod == 0) release.RetentionPeriod = null;
        if (release.StaleRetentionPeriod == 0) release.StaleRetentionPeriod = null;

        StripReleaseFieldsForApiVersion(release);

        // Verified on OC 22.10.1 (15.0) POST /odata/Releases
        // Verified on OC 23.4.0 (16.0) POST /odata/Releases
        // Verified on OC 23.10.6 (17.0) POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease
        // Verified on Automation Cloud (19.0) POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease
        if (ApiVersion >= 19)
        {
            // It seems RetentionAction "None" cannot be used with Automation Cloud.
            // TODO: What about MSI Orchestrator? What about Automation Suite?
            if (_drive._psDrive.IsCloud)
            {
                if (string.IsNullOrEmpty(release.RetentionAction) || release.RetentionAction == "None")
                {
                    release.RetentionAction = "Delete";
                    release.RetentionPeriod ??= 30;
                }
            }
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.CreateRelease", folderId, release);
        }
        else if (ApiVersion >= 17)
        {
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.CreateRelease", folderId, release);
        }
        else
        {
            // Confirmed that non-null SpecificPriorityValue causes an error on 11.1
            // Confirmed that non-null SpecificPriorityValue causes an error on 13.0
            // TODO: What about 14 and later?
            if (ApiVersion < 14 && release.SpecificPriorityValue is not null)
            {
                if (release.SpecificPriorityValue >= 61) release.JobPriority = "High";
                else if (release.SpecificPriorityValue <= 30) release.JobPriority = "Low";
                else release.JobPriority = "Normal";
                release.SpecificPriorityValue = null;
            }
            release.VideoRecordingSettings = null;
            release.RetentionAction = null;
            release.RetentionPeriod = null;
            release.RetentionBucketId = null;
            release.Tags = null;
            release.ResourceOverwrites = null;
            release.FeedId = null;
            release.ProcessSettings = null;
            // EntryPointId: added in v15.0
            if (ApiVersion < 15) release.EntryPointId = null;
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases", folderId, release);
        }
    }

    public void PatchRelease(Int64 folderId, Release release)
    {
        StripReleaseFieldsForApiVersion(release);
        HttpRequest(HttpMethod.Patch, $"/odata/Releases({release.Id!.Value})", folderId, release);
    }

    #region ReleaseRetention
    public ReleaseRetentionSetting? GetReleaseRetention(Int64 folderId, Int64 releaseId)
    {
        // Could not read the retention policy with API ver 16.0.
        // Could read the retention policy with API ver 17.0.
        if (ApiVersion < 17) return null;
        return HttpRequest<ReleaseRetentionSetting>(HttpMethod.Get, $"/odata/ReleaseRetention({releaseId})", folderId);
    }

    public void PutReleaseRetention(Int64 folderId, Int64 releaseId, ReleaseRetentionSetting setting)
    {
        HttpRequest(HttpMethod.Put, $"/odata/ReleaseRetention({releaseId})", folderId, setting);
    }

    #endregion ReleaseRetention

    public void RemoveRelease(Int64 folderId, Int64 processId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Releases({processId})", folderId);
    }

    public void UpdateReleaseToLatestVersionBulk(Int64 folderId, IEnumerable<Int64> processIds)
    {
        var payload = new { releaseIds = processIds, mergePackageTags = false };

        HttpRequest(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersionBulk", folderId, payload);
    }

    public void UpdateReleaseToLatestVersion(Int64 folderId, Int64 processId)
    {
        HttpRequest(HttpMethod.Post, $"/odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersion?mergePackageTags=false", folderId, (object?)null);
    }

    public void UpdateReleaseToSpecificVersion(Int64 folderId, Int64 processId, string version)
    {
        HttpRequest(HttpMethod.Post, $"/odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToSpecificPackageVersion", folderId, new { packageVersion = version });
    }

    public void RollbackReleaseVersion(Int64 folderId, Int64 processIds)
    {
        HttpRequest(HttpMethod.Post, $"/odata/Releases({processIds})/UiPath.Server.Configuration.OData.RollbackToPreviousReleaseVersion?mergePackageTags=false", folderId, (object?)null);
    }

    public void CreateRelease(Int64 folderId, Package package, Int64? entryPointId = null)
    {
        var payload = new
        {
            Name = package.Id!,
            Description = package.Description!,
            ProcessKey = package.Id!,
            ProcessVersion = package.Version!,
            EntryPointId = entryPointId,
        };
        HttpRequest(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.CreateRelease", folderId, payload);
    }

    #endregion Process

    #region ProcessSchedule

    public IEnumerable<ProcessSchedule> GetProcessSchedules(Int64 folderId)
    {
        return GetEnumerable<ProcessSchedule>("/odata/ProcessSchedules", folderId);
    }

    public ProcessSchedule? GetProcessSchedule(Int64 folderId, Int64 processScheduleId)
    {
        return HttpRequest<ProcessSchedule>(HttpMethod.Get, $"/odata/ProcessSchedules({processScheduleId})", folderId);
    }

    // Get the ExecutorRobots of the trigger
    public Int64[]? GetRobotIdsForSchedule(Int64 folderId, Int64 processScheduleId)
    {
        return HttpRequest<HttpBodyValue<Int64[]>>(HttpMethod.Get, $"/odata/ProcessSchedules/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule(key={processScheduleId})", folderId)?.value;
    }

    // Strip ProcessScheduleDto fields per swagger v15-v20. Same WhenWritingNull rationale as
    // StripReleaseFieldsForApiVersion. /odata/ProcessSchedules POST/PUT is strict and replies
    // "model must not be null" (HTTP 400) when the body has unrecognised fields.
    private void StripProcessScheduleFieldsForApiVersion(ProcessSchedule schedule)
    {
        // Fields added in v19.0
        if (ApiVersion < 19)
        {
            schedule.EntryPointPath = null;
            // Tags is present in the swagger from v15 onward, but earlier OC builds rejected it
            // when sent on a newly-created schedule; preserve the long-standing < 19 gating.
            schedule.Tags = null;
        }
        // Fields added in v17.0 (verified rejected by 22.10.1 / ApiVersion 15)
        if (ApiVersion < 17)
        {
            schedule.ActivateOnJobComplete = null;
            schedule.ConsecutiveJobFailuresThreshold = null;
            schedule.JobFailuresGracePeriodInHours = null;
            schedule.CalendarKey = null;
        }
        // "v16" ProcessScheduleDto fields. Empirically the v15 / ApiVersion 15 era is
        // not uniform: 22.4.4 lacks these fields while 22.10.1 has them. Both report
        // ApiVersion 15, so the safer cut-off is < 16 — accept losing the ability to set
        // them on 22.10.1 (still works, just stripped) in exchange for working on 22.4.4.
        if (ApiVersion < 16)
        {
            schedule.AlertPendingExpression = null;
            schedule.AlertRunningExpression = null;
            schedule.RunAsMe = null;
            schedule.IsConnected = null;
        }

        // ItemsActivationThreshold: original < 15 gating preserved.
        if (ApiVersion < 15)
        {
            schedule.ItemsActivationThreshold = null;
        }
        // ResumeOnSameContext: not accepted by v11
        if (ApiVersion < 13)
        {
            schedule.ResumeOnSameContext = null;
        }
    }

    public ProcessSchedule? PostProcessSchedule(Int64 folderId, ProcessSchedule schedule)
    {
        StripProcessScheduleFieldsForApiVersion(schedule);
        // v13 requires StartProcessCronDetails and ExternalJobKey
        schedule.StartProcessCronDetails ??= $"{{\"advancedCron\":{JsonSerializer.Serialize(schedule.StartProcessCron ?? "")}}}";
        schedule.ExternalJobKey ??= "";
        return HttpRequest<ProcessSchedule>(HttpMethod.Post, "/odata/ProcessSchedules", folderId, schedule);
    }

    public void PutProcessSchedule(Int64 folderId, ProcessSchedule schedule)
    {
        StripProcessScheduleFieldsForApiVersion(schedule);
        // Returns nothing
        HttpRequest(HttpMethod.Put, $"/odata/ProcessSchedules({schedule.Id})", folderId, schedule);
    }

    public void DeleteProcessSchedule(Int64 folderId, Int64 processScheduleId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/ProcessSchedules({processScheduleId})", folderId);
    }

    public bool? EnableProcessSchedule(Int64 folderId, IEnumerable<Int64> processScheduleIds, bool enabled = true)
    {
        var payload = new Dictionary<string, object?>
        {
            ["scheduleIds"] = processScheduleIds,
            ["enabled"] = enabled
        };

        var ret = HttpRequest<HttpBodyValue<bool>>(HttpMethod.Post, "/odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled", folderId, payload);
        return ret?.value;
    }

    // Server-side validation of a ProcessSchedule's executability (robot/template/license checks)
    // without committing changes. Returns IsValid + Errors + ErrorCodes.
    public ValidationResult? ValidateProcessSchedule(Int64 folderId, ProcessSchedule schedule)
    {
        var payload = new { processSchedule = schedule };
        return HttpRequest<ValidationResult>(HttpMethod.Post, "/odata/ProcessSchedules/UiPath.Server.Configuration.OData.ValidateProcessSchedule", folderId, payload);
    }

    #endregion

    #region Buckets

    public IEnumerable<Bucket> GetBuckets(Int64 folderId)
    {
        return GetEnumerable<Bucket>("/odata/Buckets", folderId);
    }

    public Bucket? PostBucket(Int64 folderId, Bucket bucket)
    {
        if (ApiVersion < 15)
        {
            bucket.Tags = null;
        }
        return HttpRequest<Bucket>(HttpMethod.Post, "/odata/Buckets", folderId, bucket);
    }

    public void DeleteBucket(Int64 folderId, Int64 bucketId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Buckets({bucketId})", folderId);
    }

    // Returns nothing
    public void DeleteBucketItem(Int64 folderId, Int64 bucketId, string fullPath)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.DeleteFile?path={Uri.EscapeDataString(fullPath)}", folderId);
    }

    public IEnumerable<BlobFile> GetBucketDirectories(Int64 folderId, Int64 bucketId)
    {
        return GetEnumerable<BlobFile>($"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetDirectories", folderId, "&directory=%2F&recursive=true");
    }

    public IEnumerable<BlobFile> GetBucketFiles(Int64 folderId, Bucket bucket)
    {
        return GetEnumerable<BlobFile>($"/odata/Buckets({bucket.Id})/UiPath.Server.Configuration.OData.GetFiles", folderId, "&directory=%2F&recursive=true");
    }

    public BlobFileAccess? GetBucketReadUri(Int64 folderId, Int64 bucketId, string fullPath)
    {
        return HttpRequest<BlobFileAccess>(HttpMethod.Get, $"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetReadUri?path={Uri.EscapeDataString(fullPath)}", folderId);
    }

    public BlobFileAccess? GetBucketWriteUri(Int64 folderId, Int64 bucketId, string fullPath)
    {
        return HttpRequest<BlobFileAccess>(HttpMethod.Get, $"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetWriteUri?path={Uri.EscapeDataString(fullPath)}", folderId);
    }

    private static bool ShouldSkipHeader(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName)) return true;

        // Exclude typical headers that cannot or should not be set via HttpRequestMessage.Headers
        return headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Host", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Expect", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("TE", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Trailer", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteFileQuiet(string path)
    {
        try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
        catch { /* ignore */ }
    }

    private HttpClient? _httpClientForBucketItem = null;

    private static void AddHeaders(HttpRequestMessage req, BlobFileAccess access)
    {
        if (access.Headers?.Keys is { } keys && access.Headers?.Values is { } vals)
        {
            var n = Math.Min(keys.Length, vals.Length);
            for (int i = 0; i < n; i++)
            {
                var k = keys[i];
                var v = vals[i];
                if (!string.IsNullOrWhiteSpace(k) && v != null && !ShouldSkipHeader(k))
                {
                    req.Headers.TryAddWithoutValidation(k, v);
                }
            }
        }
    }

    public async Task ReadBucketItemAsync(BlobFileAccess? access, string destinationPath, CancellationToken cancellationToken = default)
    {
        // Check CancellationToken
        cancellationToken.ThrowIfCancellationRequested();

        if (access == null || string.IsNullOrWhiteSpace(access.Verb) || string.IsNullOrWhiteSpace(access.Uri))
            return;

        HttpMethod method = access.Verb.Equals("POST", StringComparison.OrdinalIgnoreCase)
            ? HttpMethod.Post
            : HttpMethod.Get;

        if (!Uri.TryCreate(access.Uri, UriKind.Absolute, out var _))
            throw new ArgumentException($"Invalid Uri: {access.Uri}", nameof(access));

        // Check CancellationToken before file operations
        cancellationToken.ThrowIfCancellationRequested();

        using var req = new HttpRequestMessage(method, access.Uri);
        AddHeaders(req, access);

        // When retrieving BucketItems, the Authorization header must be excluded.
        // Since Buckets are fetched using multiple threads, the Authorization header cannot be
        // temporarily removed from _httpClient default headers.
        // It is safe to prepare a dedicated HttpClient for BucketItems.
        _httpClientForBucketItem ??= InitializeHttpClient(_drive);

        // When access.RequiresAuth is true, should the request be sent with the Authorization header retained?
        using var res = HttpClient_Send(req, _httpClientForBucketItem, cancellationToken);

        res.EnsureSuccessStatusCode();

        using var httpStream = res.Content.ReadAsStream();
        try
        {
            using var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 128,
                options: FileOptions.SequentialScan
            );

            // Execute CopyTo asynchronously, passing the CancellationToken
            await httpStream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
        }
        catch
        {
            TryDeleteFileQuiet(destinationPath);
            throw;
        }
    }

    // Synchronous version
    public void ReadBucketItem(BlobFileAccess? access, string destinationPath, CancellationToken cancellationToken = default)
    {
        ReadBucketItemAsync(access, destinationPath, cancellationToken).GetAwaiter().GetResult();
    }

    public void WriteBucketItem(BlobFileAccess access, string filePath, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        // For now, only PUT is supported. POST support may need to be added later if needed.
        if (access.Verb!.ToUpper() != "PUT") throw new NotImplementedException();

        if (!Uri.TryCreate(access.Uri, UriKind.Absolute, out var _))
            throw new ArgumentException($"Invalid Uri: {access.Uri}", nameof(access));

        cancelToken.ThrowIfCancellationRequested();
        using var fs = File.OpenRead(filePath);
        using var content = new StreamContent(fs);
        string s = MimeTypeHelper.GetMimeType(filePath);
        content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeHelper.GetMimeType(filePath));
        //content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var req = new HttpRequestMessage(HttpMethod.Put, access.Uri)
        {
            Content = content
        };
        AddHeaders(req, access);

        // When retrieving BucketItems, the Authorization header must be excluded.
        // Since Buckets are fetched using multiple threads, the Authorization header cannot be
        // temporarily removed from _httpClient default headers.
        // It is safe to prepare a dedicated HttpClient for BucketItems.
        _httpClientForBucketItem ??= InitializeHttpClient(_drive);

        // When access.RequiresAuth is true, should the request be sent with the Authorization header retained?
        using var res = HttpClient_Send(req, _httpClientForBucketItem, cancelToken);

        res.EnsureSuccessStatusCode();
    }


    // TODO
    //public void GetBucketsAcrossFolders(Int64 folderId)
    //{
    //    string body = HttpRequest(HttpMethod.Get, "/odata/Buckets/UiPath.Server.Configuration.OData.GetBucketsAcrossFolders", folderId);
    //}

    public AccessibleFoldersDto? GetFoldersForBucket(Int64 folderId, Int64 bucketId)
    {
        return HttpRequest<AccessibleFoldersDto>(HttpMethod.Get, $"/odata/Buckets/UiPath.Server.Configuration.OData.GetFoldersForBucket(id={bucketId})", folderId);
    }

    public void ShareBucketsToFolders(Int64 folderId, List<Int64> bucketIds, List<Int64> toAddFolderIds, List<Int64> toRemoveFolderIds)
    {
        BucketFoldersShare foldersShare = new();
        foldersShare.BucketIds = bucketIds;
        foldersShare.ToAddFolderIds = toAddFolderIds;
        foldersShare.ToRemoveFolderIds = toRemoveFolderIds;

        HttpRequest(HttpMethod.Post, "/odata/Buckets/UiPath.Server.Configuration.OData.ShareToFolders", folderId, foldersShare);
    }

    #endregion

    #region API Trigger

    public IEnumerable<HttpTrigger> GetHttpTriggers(Int64 folderId)
    {
        EnsureVersionSupport(14);
        return GetEnumerable<HttpTrigger>("/odata/HttpTriggers", folderId);
    }

    public HttpTrigger? GetHttpTrigger(Int64 folderId, string triggerId)
    {
        return HttpRequest<HttpTrigger>(HttpMethod.Get, $"/odata/HttpTriggers({triggerId})", folderId);
    }

    public HttpTrigger? CreateHttpTrigger(Int64 folderId, HttpTrigger trigger)
    {
        return HttpRequest<HttpTrigger>(HttpMethod.Post, "/odata/HttpTriggers", folderId, trigger);
    }

    public void RemoveHttpTrigger(Int64 folderId, string triggerId)
    {
        // nothing returns
        HttpRequest(HttpMethod.Delete, $"/odata/HttpTriggers({triggerId})", folderId);
    }

    public bool? EnableHttpTriggers(Int64 folderId, string[] triggerIds, bool enabled = true)
    {
        var payload = new Dictionary<string, object?>();
        payload["enabled"] = enabled;
        payload["triggerIds"] = triggerIds;

        var ret = HttpRequest<HttpBodyValue<bool>>(HttpMethod.Post, "/odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled", folderId, payload);
        return ret?.value;
    }

    #endregion

    #region EventTrigger
    public IEnumerable<ApiTrigger> GetEventTriggers(Int64 folderId)
    {
        EnsureVersionSupport(14); // The exact number has not been confirmed.
        return GetEnumerable<ApiTrigger>("/odata/ApiTriggers", folderId);
    }

    public void RemoveEventTrigger(Int64 folderId, string triggerId)
    {
        // nothing returns
        HttpRequest(HttpMethod.Delete, $"/odata/ApiTriggers({triggerId})", folderId);
    }

    public bool? EnableEventTriggers(Int64 folderId, string triggerId, bool enabled = true)
    {
        var payload = new Dictionary<string, object?>
        {
            ["Enabled"] = enabled
        };

        var ret = HttpRequest<HttpBodyValue<bool>>(HttpMethod.Patch, $"/odata/ApiTriggers({triggerId})", folderId, payload);
        return ret?.value;
    }
    #endregion

    #region Logs

    public IEnumerable<Log> GetRobotLogs(Int64 folderId, string? query, ulong skip, ulong first, string? orderBy, bool orderAscending)
    {
        if (string.IsNullOrEmpty(orderBy)) orderBy = "TimeStamp";
        string order;
        if (orderAscending)
        {
            order = $"&$orderby={orderBy} asc";
        }
        else
        {
            order = $"&$orderby={orderBy} desc";
        }
        return GetEnumerable<Log>("/odata/RobotLogs", folderId, $"{query}{order}", skip, first);
    }

    public IEnumerable<AuditLog> GetAuditLogs(string? query, ulong skip, ulong first)
    {
        return GetEnumerable<AuditLog>("/odata/AuditLogs", null, query, skip, first);
    }

    public IEnumerable<AuditLogEntity>? GetAuditLogDetails(Int64 auditLogId)
    {
        return GetEnumerable<AuditLogEntity>($"/odata/AuditLogs/UiPath.Server.Configuration.OData.GetAuditLogDetails(auditLogId={auditLogId})");
    }

    #endregion

    #region Robot
    public IEnumerable<Robot> GetRobots()
    {
        return GetEnumerable<Robot>("/odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots", null, "&$expand=User");
    }
    #endregion

    #region Role

    public IEnumerable<Role> GetRoles()
    {
        return GetEnumerable<Role>("/odata/Roles", null, "&$expand=Permissions");
    }

    public Role? PostRole(Role role)
    {
        role.Id = null;
        role.IsEditable = null;
        role.IsStatic = null;
        // Type: added in v15.0
        if (ApiVersion < 15) role.Type = null;
        foreach (var p in role.Permissions ?? [])
        {
            p.Id = null;
            p.RoleId = null;
        }

        return HttpRequest<Role>(HttpMethod.Post, "/odata/Roles", null, role);
    }

    public void PutRole(Role role)
    {
        role.IsEditable = null;
        role.IsStatic = null;
        foreach (var p in role.Permissions ?? [])
        {
            p.Id = null;
            p.RoleId = null;
        }

        // Returns nothing
        HttpRequest(HttpMethod.Put, $"/odata/Roles({role.Id})", null, role);
    }

    public void DeleteRole(Int64 roleId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Roles({roleId})");
    }

    #endregion

    #region User

    public IEnumerable<User> GetUsers()
    {
        return GetEnumerable<User>("/odata/Users", null, "&$expand=OrganizationUnits,UserRoles");

        //return GetEnumerable<User>("/odata/Users", null, "&$expand=OrganizationUnits,UserRoles,UnattendedRobot"); // This causes an error
    }

    public User? GetUser(Int64 userId)
    {
        return HttpRequest<User>(HttpMethod.Get, $"/odata/Users({userId})?$expand=OrganizationUnits,UserRoles");
    }

    public UserPrivilege? GetUserPrivilege(Int64 userId)
    {
        return HttpRequest<UserPrivilege>(HttpMethod.Get, $"/api/Users/GetPrivileges?userId={userId}");
    }

    public User? PostUser(User user)
    {
        if (ApiVersion < 18)
        {
            if (user.NotificationSubscription is not null)
            {
                // Added in ApiVersion 18.
                user.NotificationSubscription.RateLimitsDaily = null;
                user.NotificationSubscription.RateLimitsRealTime = null;
            }
        }
        else
        {
            user.BypassBasicAuthRestriction = null; // Deprecated in ApiVersion 18.
        }

        //if (user.UnattendedRobot is not null)
        //{
        //    user.UnattendedRobot.ExecutionSettings ??= new();
        //}

        //user.UpdatePolicy ??= new() { Type = "None" };

        return HttpRequest<User>(HttpMethod.Post, "/odata/Users", null, user);
    }

    public void PutUser(User user)
    {
        HttpRequest(HttpMethod.Put, $"/odata/Users({user.Id ?? 0})", null, user);
    }

    public void PatchUser(User user)
    {
        HttpRequest(HttpMethod.Patch, $"/odata/Users({user.Id ?? 0})", null, user);
    }

    public void DeleteUser(Int64 userId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Users({userId})");
    }

    public User? GetCurrentUser()
    {
        if (!_authManager.IsConfidentialApp)
        {
            return HttpRequest<User>(HttpMethod.Get, "/odata/Users/UiPath.Server.Configuration.OData.GetCurrentUser");
        }
        throw new InvalidOperationException("Cannot retrieve the current user in a Confidential app. Use a Non-Confidential app or specify an AccessToken.");
    }

    public ExtendedUser? GetCurrentUserExtended()
    {
        return HttpRequest<ExtendedUser>(HttpMethod.Get, "/odata/Users/UiPath.Server.Configuration.OData.GetCurrentUserExtended?$expand=PersonalWorkspace");
    }

    public void UpdateCurrentUserURPassword(Int64 userId, string password)
    {
        var payload = new Dictionary<string, object?>();
        payload["UnattendedRobot"] = new Dictionary<string, object?>
        {
            ["Password"] = password
        };
        HttpRequest(HttpMethod.Patch, $"/odata/Users({userId})", null, payload);
    }

    public void UnassignUserFromFolder(Int64 folderId, Int64 userId)
    {
        var payload = new Dictionary<string, object?>()
        {
            { "userId", userId }
        };
        HttpRequest(HttpMethod.Post, $"/odata/Folders({folderId})/UiPath.Server.Configuration.OData.RemoveUserFromFolder", null, payload);
    }

    #endregion

    #region DirectoryService

    // The quota for this API is 300 calls per 5 minutes.
    // https://uipath-japan.slack.com/archives/C0175DZP4PQ/p1751336407409919?thread_ts=1751275792.210139&cid=C0175DZP4PQ
    private DateTime _lastSearchDirectory = DateTime.MinValue;
    private readonly object _lockSearchDirectory = new object();
    public DirectoryObject[]? SearchDirectory(string prefix)
    {
        lock (_lockSearchDirectory)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastSearchDirectory;
            var waitTime = TimeSpan.FromSeconds(1) - elapsed;

            if (waitTime > TimeSpan.Zero)
            {
                Thread.Sleep(waitTime);
            }

            _lastSearchDirectory = DateTime.UtcNow;
        }
        return HttpRequest<DirectoryObject[]>(HttpMethod.Get, $"/api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={HttpUtility.UrlEncode(prefix)}&searchContext=All");
    }

    #endregion

    #region License
    public IEnumerable<LicenseNamedUser> GetLicensesNamedUser(string robotType)
    {
        return GetEnumerable<LicenseNamedUser>($"/odata/LicensesNamedUser/UiPath.Server.Configuration.OData.GetLicensesNamedUser(robotType='{robotType}')");
    }

    public IEnumerable<LicenseRuntime> GetLicensesRuntime(string robotType)
    {
        //return GetEnumerable<LicenseRuntime>($"/odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime(robotType='{robotType}')", null, "&$expand='LastLoginDate'");
        return GetEnumerable<LicenseRuntime>($"/odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime(robotType='{robotType}')");
    }

    private struct LicensesToggleEnabledRequest
    {
        public string? key { get; set; }
        public string? robotType { get; set; }
        public bool enabled { get; set; }
    };

    public void ToggleLicenseRuntime(string robotType, string key, string machineName, bool enabled)
    {
        LicensesToggleEnabledRequest payload = new()
        {
            key = key,
            robotType = robotType,
            enabled = enabled
        };

        HttpRequest(HttpMethod.Post, $"/odata/LicensesRuntime('{machineName.Replace("'", "''").Replace(" ", "%20")}')/UiPath.Server.Configuration.OData.ToggleEnabled", null, payload);
    }
    #endregion

    #region Stats

    // Returns empty...
    //public IEnumerable<ConsumptionLicenseStatsModel> GetStatsLicenseConsumption(int tenantId, int days)
    //{
    //    string body = HttpRequest(HttpMethod.Get, $"/api/Stats/GetConsumptionLicenseStats?tenantId={tenantId}&days={days}");
    //    yield break;
    //}

    // This does not work either. All values return -1...
    // Requires authentication.
    //public IEnumerable<CountStats> GetCountStats()
    //{
    //    string body = HttpRequest(HttpMethod.Get, "/api/Stats/GetCountStats");
    //    return JsonSerializer.Deserialize<CountStats[]>(body) ?? [];
    //}

    public IEnumerable<LicenseStatsModel> GetLicenseStats(int tenantId, int days)
    {
        var ret = HttpRequest<LicenseStatsModel[]>(HttpMethod.Get, $"/api/Stats/GetLicenseStats?tenantId={tenantId}&days={days}");
        return ret ?? [];
    }

    public IEnumerable<CountStats> GetJobStats()
    {
        var ret = HttpRequest<CountStats[]>(HttpMethod.Get, "/api/Stats/GetJobsStats");
        return ret ?? [];
    }

    // Returns Not Found...
    public IEnumerable<CountStats> GetSessionStats()
    {
        var ret = HttpRequest<CountStats[]>(HttpMethod.Get, "/api/Stats/GetSessionsStats");
        return ret ?? [];
    }

    #endregion

    #region Asset

    public IEnumerable<Asset> GetAssets(Int64 folderId)
    {
        // On v20+ servers neither endpoint alone is sufficient:
        //   /odata/Assets silently drops Secret-typed assets, but returns CredentialUsername.
        //   GetFiltered   includes Secret, but returns CredentialUsername="" for Credential assets.
        // Merge: non-Secret from /odata/Assets + Secret-only from GetFiltered.
        var assets = GetEnumerable<Asset>("/odata/Assets", folderId, "&$expand=UserValues");
        if (ApiVersion < 20)
            return assets;

        var secrets = GetEnumerable<Asset>(
            "/odata/Assets/UiPath.Server.Configuration.OData.GetFiltered",
            folderId,
            "&$expand=UserValues&$filter=ValueType eq 'Secret'");
        return assets.Concat(secrets);
    }

    public Asset? GetAsset(Int64 folderId, Int64 assetId)
    {
        return HttpRequest<Asset>(HttpMethod.Get, $"/odata/Assets({assetId})?$expand=UserValues", folderId);
    }

    public Asset? AddAsset(Int64 folderId, Asset asset)
    {
        // Key, Tags: added in v15.0
        if (ApiVersion < 15)
        {
            asset.Key = null;
            asset.Tags = null;
        }
        // SecretValue was added in WebApi v20.0; AllowDirectApiAccess appears on the
        // current Cloud schema (ApiVersion 20) but is absent from older OC builds.
        // Sending either to a pre-v20 server triggers strict body deserialization
        // ("assetDto must not be null", HTTP 400). This breaks Cloud → on-prem Copy-Item
        // because DeepCopy carries the source value through.
        if (ApiVersion < 20)
        {
            asset.AllowDirectApiAccess = null;
            asset.SecretValue = null;
        }
        return HttpRequest<Asset>(HttpMethod.Post, $"/odata/Assets", folderId, asset);
    }

    public Asset? AddAsset(Int64 folderId, string name, string value, string? description = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["StringValue"] = value,
            ["ValueType"] = "Text",
            ["Name"] = name,
            ["ValueScope"] = "Global",
            ["HasDefaultValue"] = true
        };
        if (description is not null) payload["Description"] = description;

        return HttpRequest<Asset>(HttpMethod.Post, $"/odata/Assets", folderId, payload);
    }

    public Asset? AddAsset(Int64 folderId, string name, bool value, string? description = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["BoolValue"] = value,
            ["ValueType"] = "Bool",
            ["Name"] = name,
            ["ValueScope"] = "Global",
            ["HasDefaultValue"] = true
        };
        if (description is not null) payload["Description"] = description;

        return HttpRequest<Asset>(HttpMethod.Post, $"/odata/Assets", folderId, payload);
    }

    public Asset? AddAsset(Int64 folderId, string name, int value, string? description = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["IntValue"] = value,
            ["ValueType"] = "Integer",
            ["Name"] = name,
            ["ValueScope"] = "Global",
            ["HasDefaultValue"] = true
        };
        if (description is not null) payload["Description"] = description;

        return HttpRequest<Asset>(HttpMethod.Post, $"/odata/Assets", folderId, payload);
    }

    public Asset? AddCredentialAsset(Int64 folderId, string name, string userName, string password, Int64 credentialStoreId, string? description = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["ValueType"] = "Credential",
            ["CredentialUsername"] = userName,
            ["CredentialPassword"] = password,
            ["CredentialStoreId"] = credentialStoreId,
            ["Name"] = name,
            ["ValueScope"] = "Global",
            ["HasDefaultValue"] = true
        };
        if (description is not null) payload["Description"] = description;

        return HttpRequest<Asset>(HttpMethod.Post, $"/odata/Assets", folderId, payload);
    }

    public void PutAsset(Int64 folderId, Asset asset)
    {
        HttpRequest(HttpMethod.Put, $"/odata/Assets({asset.Id})", folderId, asset);
    }

    //// object: AssetUserValue or Dictionary<string, object>
    //public void PutAsset(Int64 folderId, Asset asset, string? value, IEnumerable<object>? userValues = null)
    //{
    //    var payload = new Dictionary<string, object?>
    //    {
    //        ["Id"] = asset.Id,
    //        ["ValueType"] = asset.ValueType,
    //        ["Name"] = asset.Name,
    //        ["Description"] = asset.Description,
    //        ["ValueScope"] = asset.ValueScope,
    //        ["HasDefaultValue"] = asset.HasDefaultValue,
    //        ["CanBeDeleted"] = asset.CanBeDeleted,
    //        ["Key"] = asset.Key,
    //        ["Tags"] = asset.Tags
    //    };
    //    if (!string.IsNullOrEmpty(asset.CredentialUsername))
    //        payload["CredentialUsername"] = asset.CredentialUsername;
    //    if (!string.IsNullOrEmpty(asset.CredentialPassword))
    //        payload["CredentialPassword"] = asset.CredentialPassword;
    //    if (asset.CredentialStoreId is not null)
    //        payload["CredentialStoreId"] = asset.CredentialStoreId;

    //    switch (asset.ValueType)
    //    {
    //        case "Text":
    //            payload["StringValue"] = value;
    //            payload["HasDefaultValue"] = true;
    //            break;
    //        case "Bool":
    //            if (value is not null)
    //            {
    //                payload["BoolValue"] = bool.Parse(value);
    //                payload["HasDefaultValue"] = true;
    //            }
    //            break;
    //        case "Integer":
    //            if (value is not null)
    //            {
    //                payload["IntValue"] = int.Parse(value);
    //                payload["HasDefaultValue"] = true;
    //            }
    //            break;
    //    }

    //    // If not specified as an argument, retain the original UserValues
    //    userValues ??= asset.UserValues;

    //    if (userValues is null || !userValues.Any())
    //    {
    //        payload["ValueScope"] = "Global";
    //    }
    //    else
    //    {
    //        payload["ValueScope"] = "PerRobot";

    //        var newUserValues = new List<object>();
    //        foreach (var userValue in userValues)
    //        {
    //            if (userValue is AssetUserValue)
    //            {
    //                AssetUserValue v = (AssetUserValue)userValue;
    //                var newUserValue = new Dictionary<string, object?>
    //                {
    //                    ["UserId"] = v.UserId,
    //                    ["UserName"] = v.UserName,
    //                    ["MachineId"] = v.MachineId,
    //                    ["Id"] = v.Id,
    //                    ["StringValue"] = v.StringValue,
    //                    ["BoolValue"] = v.BoolValue,
    //                    ["IntValue"] = v.IntValue,
    //                    ["ValueType"] = v.ValueType,
    //                    ["Value"] = v.Value,
    //                    ["CredentialUsername"] = v.CredentialUsername,
    //                    ["ExternalName"] = v.ExternalName,
    //                    ["CredentialStoreId"] = v.CredentialStoreId
    //                };
    //                if (!string.IsNullOrEmpty(v.CredentialPassword))
    //                    newUserValue["CredentialPassword"] = v.CredentialPassword;
    //                newUserValues.Add(newUserValue);
    //            }
    //            else
    //                newUserValues.Add(userValue);
    //        }
    //        payload["UserValues"] = newUserValues;
    //    }

    //    HttpRequest(HttpMethod.Put, $"/odata/Assets({asset.Id})", folderId, payload);
    //}

    public void RemoveAsset(Int64 folderId, Int64 assetId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Assets({assetId})", folderId);
    }

    public AccessibleFoldersDto? GetFoldersForAsset(Int64 folderId, Int64 assetId)
    {
        EnsureVersionSupport(12); // Probably a later version is actually required
        return HttpRequest<AccessibleFoldersDto>(HttpMethod.Get, $"/odata/Assets/UiPath.Server.Configuration.OData.GetFoldersForAsset(id={assetId})", folderId);
    }

    public void ShareAssetsToFolders(Int64 folderId, List<Int64> assetIds, List<Int64> toAddFolderIds, List<Int64> toRemoveFolderIds)
    {
        AssetFoldersShare foldersShare = new();
        foldersShare.AssetIds = assetIds;
        foldersShare.ToAddFolderIds = toAddFolderIds;
        foldersShare.ToRemoveFolderIds = toRemoveFolderIds;

        HttpRequest(HttpMethod.Post, "/odata/Assets/UiPath.Server.Configuration.OData.ShareToFolders", folderId, foldersShare);
    }

    #endregion

    #region ExecutionMedia
    public IEnumerable<ExecutionMedia> GetExecutionMedia(Int64 folderId, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        return GetEnumerable<ExecutionMedia>("/odata/ExecutionMedia", folderId, null, skip, first);
    }

    private struct ExecutionMediaDeleteMediaByJobIdRequest
    {
        public Int64 jobId { get; set; }
    };

    public void RemoveExecutionMedia(Int64 folderId, Int64 jobId)
    {
        var payload = new ExecutionMediaDeleteMediaByJobIdRequest
        {
            jobId = jobId
        };

        HttpRequest(HttpMethod.Post, "/odata/ExecutionMedia/UiPath.Server.Configuration.OData.DeleteMediaByJobId", folderId, payload);
    }

    // TODO: This endpoint seems to work even without the Authorization header?
    public async Task<(string? FileName, byte[] FileContent)> DownloadMediaByJobId(Int64 folderId, Int64 jobId)
    {
        string endPoint = _base_url + $"/odata/ExecutionMedia/UiPath.Server.Configuration.OData.DownloadMediaByJobId(jobId={jobId})";
        var request = new HttpRequestMessage(HttpMethod.Get, endPoint);
        request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());

        using var response = await _httpClient.SendAsync(request);
        EnsureSuccessStatusCode(response);

        var contentDisposition = response.Content.Headers.ContentDisposition;

        string? ret = null;
        if (contentDisposition is not null)
        {
            // Prefer using "filename*"
            var fileNameStar = contentDisposition.FileNameStar;
            if (!string.IsNullOrEmpty(fileNameStar))
            {
                ret = fileNameStar;
            }
            else
            {
                var fileName = contentDisposition.FileName;
                if (!string.IsNullOrEmpty(fileName))
                {
                    ret = fileName;
                }
            }
        }

        // Await the result of ReadAsByteArrayAsync
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        return (ret, responseBytes);
    }

    #endregion

    #region Session

    // Get classic folder robots
    #region Tasks

    public IEnumerable<OrchTask> GetTasks(Int64 folderId)
    {
        return GetEnumerable<OrchTask>("/odata/Tasks", folderId);
    }

    public IEnumerable<OrchTask> GetTasksAcrossFolders()
    {
        return GetEnumerable<OrchTask>("/odata/Tasks/UiPath.Server.Configuration.OData.GetTasksAcrossFolders");
    }

    public OrchTask? GetTask(Int64 folderId, Int64 taskId)
    {
        return HttpRequest<OrchTask>(HttpMethod.Get, $"/odata/Tasks({taskId})", folderId);
    }

    public void RemoveTask(Int64 folderId, Int64 taskId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Tasks({taskId})", folderId);
    }

    public void EditTaskMetadata(Int64 folderId, EditTaskMetadataRequest request)
    {
        HttpRequest(HttpMethod.Post, "/odata/Tasks/UiPath.Server.Configuration.OData.EditTaskMetadata", folderId, request);
    }

    #endregion

    public IEnumerable<Session> GetSessions(Int64 folderId)
    {
        return GetEnumerable<Session>("/odata/Sessions", folderId, "&$expand=Robot($expand=License)");
    }

    // Bulk delete inactive (disconnected / unresponsive) unattended sessions by id.
    // Returns 204 No Content. Tenant-level operation (no X-UIPATH-OrganizationUnitId
    // per the v20 swagger: parameters block only carries the body).
    public void DeleteInactiveSessions(IEnumerable<Int64> sessionIds)
    {
        var payload = new { sessionIds };
        HttpRequest(HttpMethod.Post, "/odata/Sessions/UiPath.Server.Configuration.OData.DeleteInactiveUnattendedSessions", null, payload);
    }

    // Enable/disable classic folder robots
    public void ToggleEnabledStatus(Int64 folderId, Int64 robotId, bool enabled)
    {
        RobotsToggleEnabledStatusRequest payload = new()
        {
            robotIds = [robotId],
            enabled = enabled
        };
        HttpRequest(HttpMethod.Post, "/odata/Robots/UiPath.Server.Configuration.OData.ToggleEnabledStatus", folderId, payload);
    }

    public IEnumerable<Session> GetGlobalSessions(string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        return GetEnumerable<Session>("/odata/Sessions/UiPath.Server.Configuration.OData.GetGlobalSessions", null, query, skip, first);
    }

    //public IEnumerable<MachineSessionRuntime> GetMachineSessionRuntimes(string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    public IEnumerable<MachineSessionRuntime> GetMachineSessionRuntimes()
    {
        //return GetEnumerable<MachineSessionRuntime>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes", null, query, skip, first);
        return GetEnumerable<MachineSessionRuntime>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes");
    }

    public IEnumerable<MachineSessionRuntime> GetMachineSessionRuntimesByFolderId(Int64 folderId, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        return GetEnumerable<MachineSessionRuntime>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId(folderId={folderId})", folderId, query, skip, first);
    }

    // Test
    //public void GetMachineSessions(Int64 machineId)
    //{
    //    HttpRequest(HttpMethod.Get, $"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessions({machineId})");
    //    //return GetEnumerable<Session>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessions({machineId})");
    //}

    #endregion

    #region TestCase

    public IEnumerable<TestCaseDefinition> GetTestCases(Int64 folderId)
    {
        return GetEnumerable<TestCaseDefinition>("/odata/TestCaseDefinitions", folderId);
    }

    public IEnumerable<TestCaseExecution> GetTestCaseExecutions(Int64 folderId, string? filter, ulong skip, ulong first)
    {
        return GetEnumerable<TestCaseExecution>("/odata/TestCaseExecutions", folderId, filter, skip, first);
    }

    public TestCaseExecution? GetTestCaseExecutionWithAssertions(Int64 folderId, Int64 testCaseExecutionId)
    {
        return HttpRequest<TestCaseExecution>(HttpMethod.Get, $"/odata/TestCaseExecutions({testCaseExecutionId})?$expand=TestCaseAssertions", folderId);
    }

    public void DownloadAssertionScreenshot(Int64 folderId, Int64 assertionId, string destinationPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string url = _base_url + $"/api/TestAutomation/GetAssertionScreenshot?testCaseAssertionId={assertionId}&organizationUnitId={folderId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = HttpClient_Send(request, _httpClient, cancellationToken);
        EnsureSuccessStatusCode(response);

        cancellationToken.ThrowIfCancellationRequested();

        using var httpStream = response.Content.ReadAsStream();
        try
        {
            using var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 128,
                options: FileOptions.SequentialScan
            );
            httpStream.CopyTo(fileStream);
            fileStream.Flush();
        }
        catch
        {
            TryDeleteFileQuiet(destinationPath);
            throw;
        }
    }

    private class BulkDeleteTestCases
    {
        public List<Int64>? testCaseDefinitionIds { get; set; }
    }

    public void RemoveTestCases(Int64 folderId, IEnumerable<Int64> testCaseIds)
    {
        BulkDeleteTestCases payload = new()
        {
            testCaseDefinitionIds = testCaseIds.ToList()
        };
        HttpRequest(HttpMethod.Post, "/odata/TestCaseDefinitions/UiPath.Server.Configuration.OData.BulkDelete", folderId, payload);
    }

    public IEnumerable<TestSet> GetTestSets(Int64 folderId)
    {
        return GetEnumerable<TestSet>("/odata/TestSets", folderId, "&$filter=(SourceType%20eq%20%27User%27)&$expand=Environment");
    }

    public TestSet? GetTestSetForEdit(Int64 folderId, Int64 testSetId)
    {
        return HttpRequest<TestSet>(HttpMethod.Get, $"/odata/TestSets({testSetId})/UiPath.Server.Configuration.OData.GetForEdit()", folderId);
    }

    public void CreateTestSet(Int64 folderId, TestSet testSet)
    {
        string body = HttpRequest(HttpMethod.Post, "/odata/TestSets", folderId, testSet);
    }

    public void RemoveTestSet(Int64 folderId, Int64 testSetId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/TestSets({testSetId})", folderId);
    }

    public Int64? StartTestSets(Int64 folderId, Int64 testSetId)
    {
        string body = HttpRequest(HttpMethod.Post, $"/api/TestAutomation/StartTestSetExecution?testSetId={testSetId}&triggerType=0", folderId);
        if (Int64.TryParse(body, out Int64 ret))
        {
            return ret;
        }
        return null;
    }

    public IEnumerable<TestSetExecution> GetTestSetExecutions(Int64 folderId, string? filter, ulong skip, ulong first)
    {
        return GetEnumerable<TestSetExecution>("/odata/TestSetExecutions", folderId, "&$expand=TestSet" + filter, skip, first);
    }

    public void CancelTestSetExecutions(Int64 folderId, Int64 testSetExecutionId)
    {
        // Appears to return "null"
        HttpRequest(HttpMethod.Post, $"/api/TestAutomation/CancelTestSetExecution?testSetExecutionId={testSetExecutionId}", folderId);
    }

    public string CancelTestCaseExecutions(Int64 folderId, Int64 testSetExecutionId)
    {
        return HttpRequest(HttpMethod.Post, $"/api/TestAutomation/CancelTestSetExecution?testCaseExecutionId={testSetExecutionId}", folderId);
    }

    public IEnumerable<TestSetSchedule> GetTestSetSchedules(Int64 folderId)
    {
        return GetEnumerable<TestSetSchedule>("/odata/TestSetSchedules", folderId);
    }

    public TestSetSchedule? CreateTestSetSchedule(Int64 folderId, TestSetSchedule testSetSchedule)
    {
        string body = HttpRequest(HttpMethod.Post, "/odata/TestSetSchedules", folderId, testSetSchedule);
        return JsonSerializer.Deserialize<TestSetSchedule>(body)!;
    }

    public void RemoveTestSetSchedules(Int64 folderId, Int64 testSetScheduleId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/TestSetSchedules({testSetScheduleId})", folderId);
    }

    // TestSetSchedulesEnabledRequest
    private class TestSetSchedulesEnabledRequest
    {
        public bool? enabled { get; set; }
        public IEnumerable<Int64>? testSetScheduleIds { get; set; }
    }

    public void EnableTestSetSchedules(Int64 folderId, bool enabled, IEnumerable<Int64> testSetScheduleIds)
    {
        TestSetSchedulesEnabledRequest payload = new()
        {
            enabled = enabled,
            testSetScheduleIds = testSetScheduleIds
        };

        HttpRequest(HttpMethod.Post, "/odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled", folderId, payload);
    }

    public TestDataQueue? CreateTestDataQueue(Int64 folderId, TestDataQueue testDataQueue)
    {
        EnsureVersionSupport(14);
        return HttpRequest<TestDataQueue>(HttpMethod.Post, "/odata/TestDataQueues", folderId, testDataQueue);
    }

    public IEnumerable<TestDataQueue> GetTestDataQueues(Int64 folderId)
    {
        EnsureVersionSupport(14);
        return GetEnumerable<TestDataQueue>("/odata/TestDataQueues", folderId);
    }

    public void RemoveTestDataQueue(Int64 folderId, Int64 testDataQueueId)
    {
        EnsureVersionSupport(14);
        HttpRequest(HttpMethod.Delete, $"/odata/TestDataQueues({testDataQueueId})", folderId);
    }

    public IEnumerable<TestDataQueueItem> GetTestDataQueueItems(Int64 folderId, TestDataQueue testDataQueue)
    {
        EnsureVersionSupport(14);
        return GetEnumerable<TestDataQueueItem>("/odata/TestDataQueueItems", folderId, $"&$filter=(TestDataQueueId%20eq%20{testDataQueue.Id})");
    }

    public void AddTestDataQueueItems(Int64 folderId, string testDataQueueName, string itemJsonArray)
    {
        EnsureVersionSupport(14);
        // itemJsonArray is a pre-serialized JSON array supplied by the caller, so embed it raw;
        // testDataQueueName is escaped via JsonSerializer to survive quotes/backslashes.
        var payload = $"{{\"queueName\":{JsonSerializer.Serialize(testDataQueueName ?? "")},\"items\":{itemJsonArray}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/BulkAddItems", folderId, payload);
    }

    public void SetAllTestDataQueueItemsConsumed(Int64 folderId, string testDataQueueName, bool isConsumed)
    {
        EnsureVersionSupport(14);
        var payload = $"{{\"queueName\":{JsonSerializer.Serialize(testDataQueueName ?? "")},\"isConsumed\":{(isConsumed ? "true" : "false")}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/SetAllItemsConsumed", folderId, payload);
    }

    #endregion

    #region Calendar

    public IEnumerable<ExtendedCalendar>? GetCalendars()
    {
        return GetEnumerable<ExtendedCalendar>($"/odata/Calendars");
    }

    // To get ExcludedDates, GetCalendars(id) must be called.
    public ExtendedCalendar? GetCalendar(Int64 calendarId)
    {
        return HttpRequest<ExtendedCalendar>(HttpMethod.Get, $"/odata/Calendars({calendarId})");
    }

    public ExtendedCalendar? PostCalendar(ExtendedCalendar calendar)
    {
        return HttpRequest<ExtendedCalendar>(HttpMethod.Post, "/odata/Calendars", null, calendar);
    }

    public ExtendedCalendar? PutCalendar(ExtendedCalendar calendar)
    {
        return HttpRequest<ExtendedCalendar>(HttpMethod.Put, $"/odata/Calendars({calendar.Id!.Value})", null, calendar);
    }

    public void RemoveCalendar(Int64 calendarId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Calendars({calendarId})");
    }

    #endregion

    #region Maintenance

    // Host only
    public MaintenanceSetting? GetMaintenance(int? tenantId)
    {
        return HttpRequest<MaintenanceSetting>(HttpMethod.Get, $"/api/Maintenance/Get?tenantId={tenantId}");
    }

    public void SetMaintenanceMode(Int64? sessionId, bool enabled, bool force = false)
    {
        SessionMaintenanceModeParameters payload = new()
        {
            sessionId = sessionId,
            maintenanceMode = enabled ? "Enabled" : "Default",
            //stopJobsStrategy = force ? "Kill" : "SoftStop"
            stopJobsStrategy = force ? "Kill" : null
        };
        // Returns nothing
        HttpRequest(HttpMethod.Post, $"/odata/Sessions/UiPath.Server.Configuration.OData.SetMaintenanceMode", null, payload);
    }

    #endregion

    #region Action Catalog

    public TaskCatalog? CreateTaskCatalog(Int64 folderId, TaskCatalog taskCatalog)
    {
        return HttpRequest<TaskCatalog>(HttpMethod.Post, "/odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog", folderId, taskCatalog);
    }

    public IEnumerable<TaskCatalog> GetTaskCatalogs(Int64 folderId)
    {
        return GetEnumerable<TaskCatalog>("/odata/TaskCatalogs", folderId);
    }

    public void RemoveTaskCatalog(Int64 folderId, Int64 catalogId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/TaskCatalogs({catalogId})", folderId);
    }

    #endregion

    #region Integration Service

    // Connection Service v1 endpoint: /connections_/api/v1/Connections
    // - Folder context is supplied via the X-UIPATH-FolderKey header (the folder GUID Key,
    //   NOT the Int64 Id used by Orchestrator OData's X-UIPATH-OrganizationUnitId).
    //   Confirmed via browser HAR capture against the live tenant.
    // - Response is a flat JSON array (not wrapped in {value:...} or {results:...}).
    // - Pagination uses pageIndex/pageSize (default page size 1000).
    public IEnumerable<Connection> GetConnections(Int64 folderId)
    {
        string? folderKey = _drive.GetFolders().FirstOrDefault(f => f.Id == folderId)?.Key;
        if (string.IsNullOrEmpty(folderKey))
            yield break;

        const int pageSize = 1000;
        int pageIndex = 0;

        while (true)
        {
            string url = $"{_base_url}/connections_/api/v1/Connections?pageIndex={pageIndex}&pageSize={pageSize}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-UIPATH-FolderKey", folderKey);

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Connection[]? body = JsonSerializer.Deserialize<Connection[]>(strBody);

            if (body is null || body.Length == 0)
                yield break;

            foreach (var c in body)
                yield return c;

            if (body.Length < pageSize)
                yield break;

            ++pageIndex;
        }
    }

    #endregion

    #region Platform Management

    //private static string TrimUrl(string url)
    //{
    //    // Split the URL by slashes
    //    string[] parts = url.Split('/');

    //    // Rejoin up to the 3rd slash from the end
    //    if (parts.Length > 3)
    //    {
    //        return string.Join("/", parts, 0, parts.Length - 2) + "/portal_/api/identity";
    //    }
    //    else
    //    {
    //        // If the split result has 3 or fewer parts, return as-is
    //        return url;
    //    }
    //}

    // For Platform Management environments with AD integration
    //private IEnumerable<T> GetEnumerablePmDirectory<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    //{
    //    //string anotherBaseUrl = TrimUrl(_base_url);
    //    ////int slashIndex = _base_url.LastIndexOf('/');
    //    ////string __base_url = _base_url.Substring(0, slashIndex);

    //    ulong total = 0;
    //    while (true)
    //    {
    //        ulong top = Math.Min(first - total, 50);
    //        // Note: parameter names do not have a $ prefix. They are "top" and "skip", not "$top" and "$skip".
    //        //string url = $"{_base_url_identity}{endPoint}?top={top}&skip={skip}{query}";
    //        string url = $"{_base_url_identity}{endPoint}?top={top}&skip={skip}{query}";

    //        var request = new HttpRequestMessage(HttpMethod.Get, url);
    //        request.Content = new StringContent("", Encoding.UTF8, @"application/json");

    //        if (folderId.HasValue)
    //        {
    //            request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
    //        }

    //        using var response = HttpClient_Send(request);
    //        EnsureSuccessStatusCode(response);

    //        string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    //        HttpBodyResults<T> body = JsonSerializer.Deserialize<HttpBodyResults<T>>(strBody);
    //        if (body is not null && body.results is not null && body.results.Any())
    //        {
    //            foreach (var value in body.results)
    //            {
    //                yield return value;
    //                ++total;
    //                if (total == first)
    //                    break;
    //            }
    //            if (body.results?.Count == 0 || total == first)
    //                break;
    //            skip += (ulong)body.results!.Count;
    //        }
    //        else
    //            break;
    //    }
    //}


    // Returns empty with status 200...
    public UserProfile? GetPmUserProfile()
    {
        return HttpRequestIdentity<UserProfile>(HttpMethod.Get, "/api/Account/Profile");
    }

    // This endpoint only works with host admin privileges
    public void GetPmSetting()
    {
        string body = HttpRequestIdentity(HttpMethod.Get, "/api/Setting");
    }

    public DirectoryScope[]? GetPmDirectoryScope(string partitionGlobalId)
    {
        return HttpRequestIdentity<DirectoryScope[]>(HttpMethod.Get, $"/api/Directory/Scopes/{partitionGlobalId}");
    }

    private bool _pmApiDeprecated = true;
    public bool PmApiDeprecated
    {
        get
        {
            // Always false if ApiVersion < 19.
            if (ApiVersion < 19) _pmApiDeprecated = false;

            // If ApiVersion == 19, it could be either...
            // We have no choice but to try the newer API first.

            // Always true if ApiVersion >= 20.
            if (ApiVersion >= 20) _pmApiDeprecated = true;

            return _pmApiDeprecated;
        }
        set
        {
            _pmApiDeprecated = value;
        }
    }

    public IEnumerable<PmUser> GetPmUsers(string partitionGlobalId)
    {
        if (_drive._psDrive.IsCloud)
        {
            if (PmApiDeprecated)
            {
                try
                {
                    // The current Automation Cloud appears to be able to call the following.
                    return GetEnumerablePortal<PmUser>("/api/identity/User/users/licenses");
                }
                catch
                {
                    // TODO: Currently falling back to the deprecated API for all exceptions, but ideally
                    // PmApiDeprecated should only be set to false for errors like not found.
                    // For Automation Cloud alone, not falling back would be safer,
                    // but we fall back considering the possibility of it not working on Automation Suite.
                    PmApiDeprecated = false;

                    // 2025/6/12 This should have been working until recently, but it now returns the following error.
                    // User Partition API is deprecated
                    return GetEnumerablePortal<PmUser>($"/api/identity/UserPartition/licenses", null, $"&partitionGlobalId={partitionGlobalId}");
                }
            }
            else
            {
                return GetEnumerablePortal<PmUser>($"/api/identity/UserPartition/licenses", null, $"&partitionGlobalId={partitionGlobalId}");
            }
        }
        else
        {
            // This is obsoleted, but MSI OC requires calling this one...
            return GetEnumerableIdentity<PmUser>($"/api/UserPartition/users/{partitionGlobalId}");
        }
    }

    // entityType: "user", "group", or "application"
    // Passing "robot" causes an error.
    // No "PM.xxx" scope needed
    public Dictionary<string, PmGroupMember>? PmBulkResolveByName(string partitionGlobalId, string entityType, IEnumerable<string> names)
    {
        var postData = new BulkResolveByNameCommand()
        {
            entityNames = names as string[] ?? names.ToArray(),
            entityType = entityType
        };

        string body = HttpRequestIdentity(HttpMethod.Post, $"/api/Directory/BulkResolveByName/{partitionGlobalId}", null, postData);
        return JsonSerializer.Deserialize<Dictionary<string, PmGroupMember>>(body, jsoMemberConverter);
    }

    public PmDirectoryEntityInfo[]? SearchPmDirectory(string partitionGlobalId, string userName)
    {
        return GetEnumerableWithoutPagingIdentity<PmDirectoryEntityInfo>($"/api/Directory/Search/{partitionGlobalId}", null, $"&startsWith={userName}");
        //+ "&sourceFilter=localUsers"
        //+ "&sourceFilter=localGroups"
        //+ "&sourceFilter=directoryUsers"
        //+ "&sourceFilter=directoryGroups"
        //+ "&sourceFilter=robotAccounts"
        //+ "&sourceFilter=applications");
    }

    // undocumented API
    //public IEnumerable<PmUser> GetPmDirectoryUsers(string partitionGlobalId)
    //{
    //    return GetEnumerablePm2<PmUser>("/api/UserPartition/licenses", null, $"&partitionGlobalId={partitionGlobalId}");
    //}

    public BulkCreateResponse? CreatePmUserBulk(CreateUsersCommand createUsersCommand)
    {
        return HttpRequestIdentity<BulkCreateResponse>(HttpMethod.Post, $"/api/User/BulkCreate", null, createUsersCommand);
    }

    // This API is disabled and cannot be used
    public void GetPmUserLoginAttempts(string userId)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/User/{userId}/loginAttempts");
    }

    public void PutPmUser(string userId, PowerShell.Entities.UpdateUserCommand command)
    {
        // Returns something like {"succeeded":true,"errors":[]}, but we can ignore it.
        // Errors are handled via exceptions anyway.
        HttpRequestIdentity(HttpMethod.Put, $"/api/User/{userId}", null, command);
    }

    public void RemovePmUserDeprecated(string userId)
    {
        HttpRequestIdentity(HttpMethod.Delete, $"/api/User/{userId}");
    }

    public void RemovePmUser(string partitionGlobalId, string userId)
    {
        RemoveUserCommand payload = new()
        {
            partitionGlobalId = partitionGlobalId,
            userIds = [userId],
            deleteCurrentUser = false,
            isHostMode = false
        };

        HttpRequestPortal(HttpMethod.Delete, "/portal_/api/identity/User", null, payload);
    }

    public void PutPmUserSetting(UpdatePmUserSettingPayload payload)
    {
        //HttpRequestPortal(HttpMethod.Put, $"/portal_/api/identity/Setting", null, payload);
        HttpRequestIdentity(HttpMethod.Put, $"/api/Setting", null, payload);
    }

    public PmGroup[] GetPmGroups(string partitionGlobalId)
    {
        return GetEnumerableWithoutPagingIdentity<PmGroup>($"/api/Group/{partitionGlobalId}") ?? [];
    }

    //public PmGroup[] GetPmGroups2(string partitionGlobalId)
    //{
    //    return GetEnumerableWithoutPagingPortal<PmGroup>($"/api/identity/Group/{partitionGlobalId}/licenses") ?? [];
    //}

    // This is an undocumented API. The URL cannot be easily constructed.
    // "/portal_/api/orchestrator/tags/yotsuda/svc3?skip=0&take=10&startsWith=&type=Label"
    //public void GetTags()
    //{
    //    string body = HttpRequestPortal(HttpMethod.Get, "/api/orchestrator/tags");
    //}

    // This is an undocumented API.
    public IEnumerable<PmAuditLog> GetPmAuditLog(string? partitionGlobalId, string? query, ulong skip, ulong first)
    {
        if (string.IsNullOrEmpty(partitionGlobalId)) return [];
        return GetEnumerablePortal<PmAuditLog>($"/api/auditLog/{partitionGlobalId}", null, query, skip, first);
    }

    // This is an undocumented API.
    // partitionGlobalId is not needed for some reason, but the parameter is added to integrate with the cache.
    public IEnumerable<AvailableUserBundle> GetPmLicenses(string? partitionGlobalId)
    {
        return HttpRequestPortal<AvailableUserBundle[]>(HttpMethod.Get, "/api/license/accountant/UserLicense") ?? [];
    }

    // Some Portal license-management endpoints return 415 Unsupported Media Type when
    // invoked without Content-Type (HttpClient omits it on GET by default). Attaching an
    // empty JSON body forces Content-Type: application/json on the request line.
    private T? HttpGetPortalWithJsonContentType<T>(string endPoint)
    {
        string url = _base_url_portal + endPoint;
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<T>(body);
    }

    // Extracts the JWT `sub` claim, which the Portal API uses as `accountUserId`.
    private string? GetAccountUserIdFromToken()
    {
        var token = _authManager.AccessToken;
        if (string.IsNullOrEmpty(token)) return null;
        var parts = token.Split('.');
        if (parts.Length != 3) return null;
        try
        {
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            payload = payload.Replace('-', '+').Replace('_', '/');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    // This is an undocumented API.
    // Returns per-tenant license allocations for an organization (Robots & Services tab).
    public IEnumerable<TenantAllocation> GetPmLicenseAllocations(string? partitionGlobalId)
    {
        if (string.IsNullOrEmpty(partitionGlobalId)) return [];
        return HttpRequestPortal<TenantAllocation[]>(HttpMethod.Get, $"/api/licensing/tenantAllocations?accountGlobalId={partitionGlobalId}") ?? [];
    }

    // This is an undocumented API.
    // Returns the organization-level license inventory dashboard
    // (product allocations + user bundle usage + entitlement pool usage + service catalog).
    // Requires Content-Type header — use the WithJsonContentType helper.
    public LicenseInventory? GetPmLicenseInventory(string? partitionGlobalId)
    {
        if (string.IsNullOrEmpty(partitionGlobalId)) return null;
        var accountUserId = GetAccountUserIdFromToken();
        if (string.IsNullOrEmpty(accountUserId)) return null;
        return HttpGetPortalWithJsonContentType<LicenseInventory>(
            $"/api/license/management/account/available?accountUserId={accountUserId}&accountGlobalId={partitionGlobalId}");
    }

    // This is an undocumented API.
    // Returns the full organization license contract (products, entitlements, templates, subscription).
    // Requires Content-Type header — use the WithJsonContentType helper.
    public AccountLicense? GetPmLicenseContract(string? partitionGlobalId)
    {
        if (string.IsNullOrEmpty(partitionGlobalId)) return null;
        var accountUserId = GetAccountUserIdFromToken();
        if (string.IsNullOrEmpty(accountUserId)) return null;
        var resp = HttpGetPortalWithJsonContentType<AccountLicenseResponse>(
            $"/api/license/management/account?accountUserId={accountUserId}&accountGlobalId={partitionGlobalId}");
        if (resp?.accountLicense is null) return null;
        resp.accountLicense.mlKeys = resp.mlKeys;
        return resp.accountLicense;
    }

    // This is an undocumented API.
    public AvailableUserBundles? GetPmLicensedGroupsAvailableLicenses(string? groupId)
    {
        if (groupId is null) return null;
        return HttpRequestPortal<AvailableUserBundles>(HttpMethod.Get, $"/api/license/accountant/UserLicense/group/?id={groupId}");
    }

    // This is an undocumented API.
    // partitionGlobalId is not needed for some reason, but the parameter is added to integrate with the cache.
    public IEnumerable<NuLicensedGroup> GetPmLicensedGroups(string? partitionGlobalId)
    {
        return GetEnumerablePortal<NuLicensedGroup>("/api/license/accountant/UserLicense/group/page");
    }

    private class RemovePmLicensedGroupCommand
    {
        public string? id { get; set; }
    }

    // This is an undocumented API.
    public void RemovePmLicensedGroup(string? groupId)
    {
        if (groupId is null) return;
        var removeGroup = new RemovePmLicensedGroupCommand
        {
            id = groupId
        };
        // nothing returns
        HttpRequestPortal(HttpMethod.Delete, "/api/license/accountant/UserLicense/group", null, removeGroup);
    }

    // This is an undocumented API.
    public IEnumerable<NuLicensedGroupMember> GetPmLicenseGroupAllocations(string? groupId)
    {
        return GetEnumerablePortal<NuLicensedGroupMember>($"/api/license/accountant/UserLicense/group/{groupId}/allocations");
    }

    // This is an undocumented API.
    public UpdateLicensedGroupResponse? PutPmLicenseGroup(UpdateLicensedGroupCommand command)
    {
        return HttpRequestPortal<UpdateLicensedGroupResponse>(HttpMethod.Put, "/api/license/accountant/UserLicense/group", null, command);
    }

    // This is an undocumented API.
    public void DeletePmLicenseGroupAllocations(string? groupId, string userId)
    {
        HttpRequestPortal(HttpMethod.Delete, $"/api/license/accountant/UserLicense/group/{groupId}/user/{userId}");
    }

    // This is an undocumented API.
    // partitionGlobalId is not needed for some reason, but the parameter is added to integrate with the cache.
    public IEnumerable<NuLicensedUser> GetPmLicensedUsers(string? partitionGlobalId)
    {
        return GetEnumerablePortal<NuLicensedUser>("/portal_/api/license/accountant/UserLicense/user/page");
    }

    public void PutLicensedUser(AddLicensedUserCommand payload)
    {
        HttpRequestPortal(HttpMethod.Post, "/portal_/api/license/accountant/UserLicense/users", null, payload);
    }

    internal static readonly JsonSerializerOptions jsoMemberConverter = new()
    {
        Converters = { new MemberConverter() }
    };

    public PmGroup? GetPmGroup(string partitionGlobalId, string? groupId)
    {
        if (groupId is null) return null;
        //string body = HttpRequestIdentity(HttpMethod.Get, $"/api/Group/{partitionGlobalId}/{groupId}", null, (object?)null);
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/Group/{partitionGlobalId}/{groupId}");
        return JsonSerializer.Deserialize<PmGroup>(body, jsoMemberConverter);
    }

    public PmGroup? CreatePmGroup(CreateGroupCommand newGroup)
    {
        string body = HttpRequestIdentity(HttpMethod.Post, "/api/Group", null, newGroup);
        return JsonSerializer.Deserialize<PmGroup>(body, jsoMemberConverter);
    }

    public PmGroup? PutPmGroup(string? groupId, UpdateGroupCommand updateGroup)
    {
        if (groupId is null) return null;
        string body = HttpRequestIdentity(HttpMethod.Put, $"/api/Group/{groupId}", null, updateGroup);
        return JsonSerializer.Deserialize<PmGroup>(body, jsoMemberConverter);
    }

    private class RemovePmGroupsInfo
    {
        public string[]? groupIDs { get; set; }
    }

    public void RemovePmGroup(string partitionGlobalId, string? groupId)
    {
        if (groupId is null) return;
        var removeGroup = new RemovePmGroupsInfo
        {
            groupIDs = [groupId]
        };
        // nothing returns
        HttpRequestIdentity(HttpMethod.Delete, $"/api/Group/{partitionGlobalId}", null, removeGroup);
    }

    public IEnumerable<PmRobotAccount> GetPmRobotAccounts(string partitionGlobalId)
    {
        return GetEnumerableIdentity<PmRobotAccount>($"/api/RobotAccount/{partitionGlobalId}");
    }

    public PmRobotAccount? CreatePmRobot(CreateRobotAccountCommand cmd)
    {
        return HttpRequestIdentity<PmRobotAccount>(HttpMethod.Post, $"/api/RobotAccount", null, cmd);
    }

    // partitionGlobalId is not needed for some reason.
    public PmRobotAccount? UpdatePmRobot(string robotId, UpdateRobotAccountCommand cmd)
    {
        return HttpRequestIdentity<PmRobotAccount>(HttpMethod.Put, $"/api/RobotAccount/{robotId}", null, cmd);
    }

    private class DeletePmRobotPosting
    {
        public string[]? robotAccountIDs { get; set; }
    }

    public void RemovePmRobot(string partitionGlobalId, string robotId)
    {
        var payload = new DeletePmRobotPosting()
        {
            robotAccountIDs = [robotId]
        };
        HttpRequestIdentity(HttpMethod.Delete, $"/api/RobotAccount/{partitionGlobalId}", null, payload);
    }

    // partitionGlobalId is not needed for some reason, but the parameter is added to integrate with the cache.
    public IEnumerable<ExternalResource> GetPmExternalApiResource(string partitionGlobalId)
    {
        return GetEnumerableWithoutPagingIdentity<ExternalResource>("/api/ExternalApiResource") ?? [];
    }

    public IEnumerable<ExternalClient> GetPmExternalClients(string partitionGlobalId)
    {
        return GetEnumerableWithoutPagingIdentity<ExternalClient>($"/api/ExternalClient/{partitionGlobalId}") ?? [];
    }

    public ExternalClient? GetPmExternalClient(string? partitionGlobalId, string id)
    {
        return HttpRequestIdentity<ExternalClient>(HttpMethod.Get, $"/api/ExternalClient/{partitionGlobalId}/{id}");
    }

    // This probably does not need a wrapper method in OrchDriveInfo.
    public ExternalClientCreated? PostPmExternalClient(CreateExternalClientCommand app)
    {
        return HttpRequestIdentity<ExternalClientCreated>(HttpMethod.Post, "/api/ExternalClient", null, app);
    }

    // Returns nothing
    public void DeletePmExternalClient(string partitionGlobalId, string id)
    {
        HttpRequestIdentity(HttpMethod.Delete, $"/api/ExternalClient/{partitionGlobalId}/{id}");
    }

    // Forbidden for external app
    //public IEnumerable<int> GetIdentityClient()
    //{
    //    return GetEnumerableIdentity<int>($"/Client");
    //}

    // The completer could use the return value of GetIdentityAvailableDirectoryTypes().
    // But what would it be useful for...
    // identifier needs to be a directory adapter.
    // e.g., "aad", "Saml2", "scim", etc.
    public void GetPmDirectoryConfiguration(string identifier)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/DirectoryConnection/DirectoryConfiguration?identifier={identifier}");
    }

    // Returns something like ["aad","Saml2","scim"].
    public string[]? GetPmAvailableDirectoryTypes()
    {
        return HttpRequestIdentity<string[]>(HttpMethod.Get, "/api/DirectoryConnection/AvailableDirectoryTypes");
    }

    // Seems to return an empty array. Hmm.
    public void GetPmExternalIdentityProvider(string partitionGlobalId)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/ExternalIdentityProvider?partitionGlobalId={partitionGlobalId}");
    }

    // Forbidden for external app
    //public void GetIdentityResource()
    //{
    //    string body = HttpRequestIdentity(HttpMethod.Get, "/IdentityResource");
    //}

    // Works correctly.
    // TODO: Implement a Get-OrchIdLanguage cmdlet.
    public void GetPmLanguage()
    {
        string body = HttpRequestIdentity(HttpMethod.Get, "/api/Language");
    }

    public void GetIdentitySetting(string partitionGlobalId, string userId)
    {
        HttpRequestIdentity(HttpMethod.Get, $"/api/Setting", null, (object)$"&partitionGlobalId={partitionGlobalId}&userId={userId}");
    }

    // Seems to return an empty array. Hmm.
    public void GetPmRule(string partitionGlobalId)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/Rule/{partitionGlobalId}");
    }

    // Unfortunately, this does not work with confidential apps. Returns empty.
    // Even when called from a non-confidential app, GetCurrentUser provides richer information. Not useful.
    //public void GetPmUserOrgsInfo()
    //{
    //    string body = HttpRequestPm(HttpMethod.Get, "/api/UserOrgs/userOrgsLocalByAuth0Token");
    //}

    // Forbidden for external app
    //public void GetPmUserOrgsInfo(string email)
    //{
    //    string body = HttpRequestIdentity(HttpMethod.Get, $"/api/UserOrgs/userOrgs?email={email}");
    //}

    // This should be able to get the partitionGlobalId.
    // With confidential apps, it seems partitionGlobalId may not always be retrievable?
    public PmAuthenticationRoot? GetPmAuthenticationSetting(string partitionGlobalId)
    {
        var body = HttpRequestIdentity<Dictionary<string, PmAuthenticationRoot>>(HttpMethod.Get, $"/api/AuthenticationSetting/getAll/{partitionGlobalId}");
        return body?.FirstOrDefault().Value;
    }

    public AccessAllowedMember[] GetPmPartitionAccessPolicy(string partitionGlobalId)
    {
        return HttpRequestPortal<AccessAllowedMember[]>(HttpMethod.Get, $"/api/identity/PartitionAccessPolicy/{partitionGlobalId}") ?? [];
    }

    #endregion

    #region Document Understanding

    public DuProject[]? GetDuProjects()
    {
        var body = HttpRequest<DuGetProjectsResponse>(HttpMethod.Get, "/du_/api/framework/projects?api-version=1");
        return body?.projects;
    }

    // Does not seem to work... This is problematic.
    public void CreateDuProjects(CreateDuProjectCmd cmd)
    {
        var body = HttpRequest(HttpMethod.Post, "/du_/api/app/web/projects?api-version=1.4", null, cmd);
    }

    public DuDocumentType[]? GetDuDocumentTypes(string? projectId)
    {
        var body = HttpRequest<DuGetDocumentTypesResponse>(HttpMethod.Get, $"/du_/api/framework/projects/{projectId}/document-types?api-version=1");
        return body?.documentTypes;
    }

    public DuClassifier[]? GetDuClassifiers(string? projectId)
    {
        var body = HttpRequest<DuGetClassifiersResponse>(HttpMethod.Get, $"/du_/api/framework/projects/{projectId}/classifiers?api-version=1");
        return body?.classifiers;
    }

    public DuExtractor[]? GetDuExtractors(string? projectId)
    {
        var body = HttpRequest<DuGetExtractorsResponse>(HttpMethod.Get, $"/du_/api/framework/projects/{projectId}/extractors?api-version=1");
        return body?.extractors;
    }

    // TODO: Should pagination be supported?
    public DuUser[]? GetDuUsers(string? partitionGlobalId, string? tenantKey, string? projectId)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/userroleassignments?scope=/tenant/{tenantKey}/DocumentUnderstanding/projects/{projectId}&serviceName=DocumentUnderstanding";

        string body = HttpRequestImpl(HttpMethod.Get, baseUrl, "");
        var results = JsonSerializer.Deserialize<HttpBodyResults<DuUser>>(body);
        return results?.results;
    }

    public DuUser[]? PatchDuUsers(string? partitionGlobalId, string? tenantKey, string? projectId)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/userroleassignments?scope=/tenant/{tenantKey}/DocumentUnderstanding/projects/{projectId}";

        string body = HttpRequestImpl(HttpMethod.Patch, baseUrl, "");
        var results = JsonSerializer.Deserialize<HttpBodyResults<DuUser>>(body);
        return results?.results;
    }

    // TODO: Should pagination be supported?
    public DuRole[]? GetDuRoles(string? partitionGlobalId)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/roles?scopeType=project&serviceName=DocumentUnderstanding";

        string body = HttpRequestImpl(HttpMethod.Get, baseUrl, "");
        var results = JsonSerializer.Deserialize<HttpBodyResults<DuRole>>(body);
        return results?.results;
    }

    // Returns nothing
    public void SetDuRoleToDuUser(string? partitionGlobalId, UserRoleAssignmentsCmd payload)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/userroleassignments";
        HttpRequestImpl(HttpMethod.Patch, baseUrl, "", null, payload);
    }
    #endregion

    #region TestManager

    // Pagination with PagingModel
    private IEnumerable<T> GetEnumerableTm<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, 100);
            string url = $"{_base_url}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            TmPagingModel<T> body = JsonSerializer.Deserialize<TmPagingModel<T>>(strBody);
            if (body is not null && body.data is not null && body.data.Count != 0)
            {
                foreach (var e in body.data!)
                {
                    yield return e;
                    ++total;
                    if (total == first)
                        break;
                }
                if (!(body.paging?.nextPage.GetValueOrDefault() ?? false))
                    break;
            }
            else
                break;
        }
    }

    // Pagination with PagingModel2
    private IEnumerable<T> GetEnumerableTm2<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, 100);
            string url = $"{_base_url}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            TmPagingModel2<T> body = JsonSerializer.Deserialize<TmPagingModel2<T>>(strBody);
            if (body is not null && body.data is not null && body.data.Count != 0)
            {
                foreach (var e in body.data!)
                {
                    yield return e;
                    ++total;
                    if (total == first)
                        break;
                }
                if (!body.hasNextPage.GetValueOrDefault())
                    break;
            }
            else
                break;
        }
    }

    // No pagination
    private IEnumerable<T> GetEnumerableTm3<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, 1000);
            string url = $"{_base_url}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);

            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            List<T> body = JsonSerializer.Deserialize<List<T>>(strBody);
            if (body is not null && body.Count != 0)
            {
                foreach (var e in body!)
                {
                    yield return e;
                    ++total;
                    if (total == first)
                        break;
                }
                if (total == first)
                    break;
            }
            else
                break;
        }
    }

    public IEnumerable<TmProject>? GetTmProjects()
    {
        return GetEnumerableTm<TmProject>("/testmanager_/api/v2/projects");
    }

    public void PutTmProject(TmProject project)
    {
        var body = HttpRequest(HttpMethod.Put, $"/testmanager_/api/v2/projects/{project.id}", null, project);
    }

    public void RemoveTmProject(string projectId)
    {
        // Returns nothing
        HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/projects/{projectId}");
    }

    public IEnumerable<TmRequirement> GetTmRequirements(string projectId)
    {
        return GetEnumerableTm2<TmRequirement>($"/testmanager_/api/v2/{projectId}/requirements");
    }

    public void RemoveTmRequirements(string projectId, string requirementId)
    {
        string body = HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/{projectId}/requirements/{requirementId}");
    }

    public IEnumerable<TmTestCase> GetTmTestCases(string projectId)
    {
        return GetEnumerableTm<TmTestCase>($"/testmanager_/api/v2/{projectId}/testcases");
    }

    public void RemoveTmTestCase(string projectId, string testCaseId)
    {
        // Returns empty
        HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/{projectId}/testcases/{testCaseId}");
    }

    public IEnumerable<TmTestSet> GetTmTestSets(string projectId)
    {
        return GetEnumerableTm<TmTestSet>($"/testmanager_/api/v2/{projectId}/testsets");
    }

    public void RemoveTmTestSet(string projectId, string testSetId)
    {
        HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/{projectId}/testsets/{testSetId}");
    }

    // This endpoint appears to return the same results as /testexecutions/filtered.
    // Are the parameters unusable?
    public IEnumerable<TmTestExecution> GetTmTestExecutions(string projectId)
    {
        return GetEnumerableTm<TmTestExecution>($"/testmanager_/api/v2/{projectId}/testexecutions");
    }

    public IEnumerable<TmTestExecution> GetTmTestExecutionsFiltered(string projectId)
    {
        return GetEnumerableTm<TmTestExecution>($"/testmanager_/api/v2/{projectId}/testexecutions/filtered");
    }

    public IEnumerable<TmTestExecutionResult> GetTmTestExecutionsResult(string projectId, string testExecutionId)
    {
        return GetEnumerableTm<TmTestExecutionResult>($"/testmanager_/api/v2/{projectId}/testcaselogs/testexecution/{testExecutionId}/paged");
    }

    public IEnumerable<TmRole>? GetTmRoles()
    {
        return GetEnumerableTm3<TmRole>("/testmanager_/api/v2/roles");
    }

    public TmServerInfo? GetTmServerInfo()
    {
        return HttpRequest<TmServerInfo>(HttpMethod.Get, "/testmanager_/api/serverinfo");
    }

    public TmConfig? GetTmConfiguration()
    {
        return HttpRequest<TmConfig>(HttpMethod.Get, "/testmanager_/api/configuration");
    }

    public TmProjectSettings? GetTmProjectSettings(string projectId)
    {
        return HttpRequest<TmProjectSettings>(HttpMethod.Get, $"/testmanager_/api/v2/{projectId}/projectsettings");
    }

    public IEnumerable<TmProjectPermission> GetTmProjectPermission(string projectId)
    {
        return GetEnumerableTm<TmProjectPermission>($"/testmanager_/api/v2/{projectId}/permissions/project");
    }

    public IEnumerable<TmProjectPermission> GetTmDefects(string projectId)
    {
        return GetEnumerableTm<TmProjectPermission>($"/testmanager_/api/v2/{projectId}/defects");
    }

    #endregion

    #region Context Grounding
    // Unfortunately, this does not work. No error is returned, but an empty entity is returned.
    //public string GetCgIndex(string partitionGlobalId, string tenantKey, string folderKey)
    //{
    //    Uri uri = new(_base_url);
    //    string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/{tenantKey}/ecs_/v2/indexes?$expand=datasource";
    //    return HttpRequestGcImpl(HttpMethod.Get, baseUrl, folderKey, null);
    //}
    #endregion
}

#pragma warning restore IDE1006 // Naming styles
