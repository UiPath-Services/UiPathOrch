#pragma warning disable IDE1006 // 命名スタイル

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

// リソースへのアクセスを制限
// 同時にアクセスできる回数: maxConcurrentRequests
// 1秒間の間にアクセスできる回数: maxRequestsPerSecond
// スレッドの上限は OrchThreadPoolImpl class で cap するため、このクラスからは除外した
public class RateLimiter : IDisposable
{
    //private readonly SemaphoreSlim concurrentSemaphore;
    private readonly SemaphoreSlim rateLimitSemaphore;
    private readonly int maxRequestsPerSecond;
    private readonly Timer refillTimer; // タイマーが GC で回収されないように、タイマーの参照を保持しておく

    public RateLimiter(int maxRequestsPerSecond)
    {
        //this.concurrentSemaphore = new SemaphoreSlim(maxConcurrentRequests);
        this.maxRequestsPerSecond = maxRequestsPerSecond;
        this.rateLimitSemaphore = new SemaphoreSlim(maxRequestsPerSecond, maxRequestsPerSecond);

        // Set up a timer to refill rate limit tokens every second
        refillTimer = new Timer(RefillRateLimitTokens!, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(1));
    }

    private void RefillRateLimitTokens(object state)
    {
        //lock (rateLimitSemaphore) // SemaphoreSlim はスレッドセーフなので lock の必要はなかった
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
        //concurrentSemaphore.Wait();
        rateLimitSemaphore.Wait();
    }

    //public async Task WaitAsync(CancellationToken cancellationToken = default)
    //{
    //    await rateLimitSemaphore.WaitAsync(cancellationToken);
    //}

    public void Release()
    {
        //concurrentSemaphore.Release();
    }

    public void Dispose()
    {
        refillTimer.Dispose();
        rateLimitSemaphore.Dispose();
    }
}

public partial class OrchAPISession : IDisposable
{
    private readonly HttpClient _httpClient;
    private HttpClient HttpClient
    {
        get
        {
            EnsureAuthenticated();
            return _httpClient;
        }
    }

    // 1秒間の間に送出できるリクエスト数を15に制限
    private readonly RateLimiter limitter = new(15);

    private int http_call_num = 0;
    private HttpResponseMessage HttpClient_Send(HttpRequestMessage message, CancellationToken cancellationToken = default)
    {
        //limitter.WaitAsync(cancellationToken).GetAwaiter().GetResult();
        limitter.Wait();
        HttpResponseMessage ret = null;
        DateTime reqTime = DateTime.Now;
        DateTime resTime = reqTime;
        int callId = Interlocked.Increment(ref http_call_num);
        try
        {
            reqTime = DateTime.Now;
            ret = HttpClient!.Send(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resTime = DateTime.Now;
            return ret;
        }
        finally
        {
            limitter.Release();

            var logging = _drive._psDrive.Logging;
            bool logEnabled = logging?.Enabled.GetValueOrDefault() ?? false;
            if (logEnabled)
            {
                string? combinedLogBlock = BuildCombinedLogBlock(reqTime, message, resTime, ret, callId, logging?.InternalLogLevel);
                WriteLogBlock(combinedLogBlock);
            }
        }
    }

    private DateTime _expiryTime; // トークンが expire する正確な時刻を保持
    private readonly OrchestratorAuthManager _authManager;

    internal OrchestratorAuthManager AuthManager
    {
        get { return _authManager; }
    }

    internal readonly string _base_url;
    internal readonly string _base_url_identity;
    internal readonly string _base_url_portal;
    internal bool _isAuthenticated = false;
    private bool _disposed = false;
    private readonly OrchDriveInfo _drive;
    public double? ApiVersion;

    #region Authentication

    public OrchAPISession(OrchDriveInfo drive)
    {
        _drive = drive;

        if (drive._psDrive.Proxy?.Enabled ?? false)
        {
            HttpClientHandler handler;
            try
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

                handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };

                // SSL 証明書がない場合の例外を無視する
                if (drive._psDrive.IgnoreSslErrors.GetValueOrDefault(false))
                {
                    handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Proxy: {ex.Message}", ex);
            }

            _httpClient = new HttpClient(handler);
        }
        else
        {
            // SSL 証明書がない場合の例外を無視する
            if (drive._psDrive.IgnoreSslErrors.GetValueOrDefault(false))
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true,
                };
                _httpClient = new HttpClient(handler);
            }
            else
            {
                _httpClient = new HttpClient();
            }
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string userAgent = $"UiPathOrch/{version}";
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        _authManager = new OrchestratorAuthManager(drive, _httpClient);
        if (_authManager._isUserPassword)
        {
            _base_url = _authManager._baseUrl;
        }
        else
        {
            _base_url = drive._psDrive.Root!;
        }

        if (drive._psDrive.IsCloud)
        {
            int slashIndex = _base_url.LastIndexOf('/');
            _base_url_identity = string.Concat(_base_url.AsSpan(0, slashIndex), "/identity_");
            _base_url_portal = string.Concat(_base_url.AsSpan(0, slashIndex), "/portal_");
        }
        else
        {
            _base_url_identity = _base_url + "/identity";
            _base_url_portal = _base_url + "/portal";
        }

        if (!string.IsNullOrEmpty(drive._psDrive.IdentityUrl))
        {
            _base_url_identity = drive._psDrive.IdentityUrl;
        }

        _drive = drive;
    }

    private static readonly object _authLock = new();
    internal void EnsureAuthenticated()
    {
        if (!_isAuthenticated)
        {
            lock (_authLock)
            {
                if (!_isAuthenticated)
                {
                    // Set initial token
                    SetToken(_authManager.RequestToken());
                    _isAuthenticated = true;
                    _expiryTime = DateTime.Now.AddHours(1);

                    if (ApiVersion is null && _drive is not null)
                    {
                        try
                        {
                            var activitySettings = _drive.ActivitySettings.Get();
                            if (double.TryParse(activitySettings?.ApiVersion, out var version))
                            {
                                ApiVersion = version;
                            }
                        }
                        catch {} // この例外は握りつぶす
                    }
                }
            }
        }

        // トークンが切れる5分前を過ぎていたら、トークンをリフレッシュする
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
            Console.WriteLine($"renew token failed on {_drive.NameColonSeparator}.");
            Console.WriteLine(ex.Message);
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

            var response = HttpClient_Send(request);
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

            // パラメータ名に $ がつかない。$top とか $skip でないので要注意。
            string url = $"{_base_url_identity}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            //{
            //    Content = new StringContent("", Encoding.UTF8, @"application/json")
            //};
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }

            var response = HttpClient_Send(request);
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

    private static readonly StringContent EmptyContent = new("", Encoding.UTF8, @"application/json");

    private IEnumerable<T> GetEnumerablePortal<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;

        //using var cancelHandler = new ConsoleCancelHandler();
        while (true)
        {
            //cancelHandler.Token.ThrowIfCancellationRequested();

            ulong top = Math.Min(first - total, 1000);
            if (top == 0) break;

            // パラメータ名に $ がつかない。$top とか $skip でないので要注意。
            string url = $"{_base_url_portal}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                // content が空っぽの場合でも、次の行は必要。
                // これがないと、Get-OrchPmUser のエンドポイントがエラーを返してしまう。
                // ほとんどすべてのエンドポイントでは、次の行がなくても大丈夫なのに、、
                Content = EmptyContent
            };
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }

            var response = HttpClient_Send(request);
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

    // ページングのサポートがない
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

        var response = HttpClient_Send(request);
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

    private T[]? GetEnumerableWithoutPagingPortal<T>(string endPoint, Int64? folderId = null, string? query = null)
    {
        return GetEnumerableWithoutPagingImpl<T[]>(_base_url_portal, endPoint, folderId, query);
    }

    public string HttpRequestImpl(HttpMethod method, string baseUrl, string endPoint, Int64? folderId, string payload)
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

        var response = HttpClient_Send(request);
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
            return HttpRequestImpl(method, baseUrl, endPoint, folderId, "");
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

    // 非公開の API を呼び出す用
    public string HttpRequestPortal(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null)
    {
        return HttpRequestImpl(method, _base_url_portal, endPoint, folderId, payload);
    }

    public T? HttpRequest<T>(HttpMethod method, string endPoint, Int64? folderId = null, object? query = null)
    {
        string body = HttpRequest(method, endPoint, folderId, query);
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
        return GetEnumerable<Alert>("/odata/Alerts", null, query, skip, first);
    }
    #endregion

    #region Queues

    public IEnumerable<QueueDefinition> GetQueues(Int64 folderId)
    {
        return GetEnumerable<QueueDefinition>("/odata/QueueDefinitions", folderId);
    }

    public QueueDefinition? GetQueue(Int64 folderId, Int64 queueId)
    {
        return HttpRequest<QueueDefinition>(HttpMethod.Get, $"/odata/QueueDefinitions({queueId})", folderId);
    }

    public QueueRetentionSetting? GetQueueRetention(Int64 folderId, Int64 queueId)
    {
        EnsureVersionSupport(14);
        return HttpRequest<QueueRetentionSetting>(HttpMethod.Get, $"/odata/QueueRetention({queueId})", folderId);
    }

    public QueueDefinition? CreateQueue(Int64 folderId, QueueDefinitionPosting queue)
    {
        // OC 22.10.1 (15.0) で動作確認済み POST /odata/QueueDefinitions
        // OC 23.4.0 (16.0) で動作確認済み POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue
        if (ApiVersion >= 16)
        {
            return HttpRequest<QueueDefinition>(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue", folderId, queue);
        }
        else
        {
            queue.RetentionAction = null;
            queue.RetentionPeriod = null;
            queue.RetentionBucketId = null;
            return HttpRequest<QueueDefinition>(HttpMethod.Post, "/odata/QueueDefinitions", folderId, queue);
        }
    }

    public void EditQueue(Int64 folderId, QueueDefinitionPosting queue)
    {
        // 何も返さない
        HttpRequest(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.EditQueue", folderId, queue);
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

    // TODO: きれいに書き直す
    public void AddQueueItem(Int64 folderId, string queueName, Dictionary<string, object> specificContent, QueuePriority priority = QueuePriority.Normal)
    {
        var itemData = new Dictionary<string, object>()
        {
            {
                "itemData",
                new Dictionary<string, object>() {
                    { "Name", $"{queueName}" },
                    { "Priority", priority.ToString() },
                    { "SpecificContent", specificContent },
                }
            }
        };

        HttpRequest(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.AddQueueItem", folderId, itemData);
    }

    // filter には "&$filter=()" のようなのを渡す必要がある
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

    public IEnumerable<RobotsFromFolderModel> GetRobotsFromFolder(Int64 folderId)
    {
        return GetEnumerable<RobotsFromFolderModel>($"/odata/Robots/UiPath.Server.Configuration.OData.GetRobotsFromFolder(folderId={folderId})");
    }

    // マシン作成・更新時に使う。フィルターを設定していることに注意。このフィルタテキストはキャッシュ側でもつべきかもしれない。
    public IEnumerable<ExtendedRobot> FindAllRobotsAcrossFolders()
    {
        return GetEnumerable<ExtendedRobot>($"/odata/Robots/UiPath.Server.Configuration.OData.FindAllAcrossFolders", null,
            "&$filter=Type%20eq%20%272%27%20and%20ProvisionType%20eq%20%271%27&$expand=User");
    }

    public IEnumerable<SimpleUser> GetReviewers(Int64 folderId)
    {
        return GetEnumerable<SimpleUser>("/odata/QueueItems/UiPath.Server.Configuration.OData.GetReviewers()", folderId, "&$filter=(Type%20eq%20%27DirectoryUser%27)");
    }

    public BulkOperationResponseDtoOfFailedQueueItem? BulkAddQueueItem(Int64 folderId, string payload)
    {
        return HttpRequest<BulkOperationResponseDtoOfFailedQueueItem>(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.BulkAddQueueItems", folderId, payload);
    }

    #endregion

    #region CredentialStores

    // Type を取得できない。Post するときに必要なのに。。
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

    // 空が返る
    public void RemoveWebhooks(Int64 webhookId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Webhooks({webhookId})");
    }

    public Webhook? CreateWebhook(Webhook webhook)
    {
        return HttpRequest<Webhook>(HttpMethod.Post, "/odata/Webhooks", null, webhook);
    }

    public Webhook? PatchWebhook(Int64 webhookId, Webhook webhook)
    {
        return HttpRequest<Webhook>(HttpMethod.Patch, $"/odata/Webhooks({webhookId})", null, webhook);
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
    //public void GetAllRolesForUser()
    //{
    //    HttpRequest(HttpMethod.Get, "/api/FoldersNavigation/GetAllRolesForUser?username=wenqi.li%40uipath.com&type=User");
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
            newFolder.FeedType = feedType; // OC Issue: なぜか FeedType が必ず Process になってしまうので直す
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

        // 何も返らない
        HttpRequest(HttpMethod.Put, endPoint);
    }

    public LibraryFeed[]? GetLibraryFeeds()
    {
        return HttpRequest<LibraryFeed[]>(HttpMethod.Get, "/api/PackageFeeds/GetLibraryFeeds");
    }

    // このメソッドは、"" を返すべきではない
    // このメソッドを呼ぶ側が、必要に応じて null を "" に変換する必要がある
    // (Dictionary のキーとして使う場合など)
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

    // ApiVersion = 15 で動作確認済み
    public void SetFolderMachineInherit(Int64 folderId, Int64 machineId, bool enabled)
    {
        FolderMachineInherit payload = new(machineId, folderId, enabled);
        // "" が返る
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.ToggleFolderMachineInherit", folderId, payload);
    }

    // TODO: ここでフィルター指定してるけど、まあいいか。。キャッシュ側で実装した方があとあと嬉しいかもしれないけど、
    public IEnumerable<ExtendedRobot> GetFolderRobots(Int64 folderId, Int64 machineId)
    {
        return GetEnumerable<ExtendedRobot>($"/odata/Robots/UiPath.Server.Configuration.OData.GetFolderRobots(folderId={folderId},machineId={machineId})",
            null,
            "&$filter=Type eq '2' and ProvisionType eq 'Automatic'&$expand=User");
    }

    public IEnumerable<RobotUser> GetMachineRobots(Int64 folderId, Int64 machineId)
    {
        return GetEnumerable<RobotUser>($"/odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId})");
    }

    public void SetMachineRobots(SetMachineRobotsCmd cmd)
    {
        // 何も返らない
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots", null, cmd);
    }

    public IEnumerable<UserRobots> GetUserRobots(Int64 folderId)
    {
        return GetEnumerable<UserRobots>("/odata/Sessions/UiPath.Server.Configuration.OData.GetUserRobots", folderId, "&$filter=startswith(Robot/Username,'autogen\\') eq false and Robot/Username ne ''");
    }

    // TODO: きれいに書き直す。StartJob() とか参考に。
    public void AddMachinesToFolder(Int64 folderId, IEnumerable<Int64> machineIds)
    {
        var payload = new Dictionary<string, object?>();
        payload["associations"] = new Dictionary<string, object?>()
        {
            { "FolderId", folderId },
            { "AddedMachineIds", machineIds },
            { "RemovedMachineIds", Array.Empty<int>() }
        };

        // 何も返らない
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations", folderId, payload);
    }

    // TODO: きれいに書き直す。StartJob() とか参考に。
    public void UnassignMachinesFromFolder(Int64 folderId, IEnumerable<Int64> machineIds)
    {
        var payload = new Dictionary<string, object?>();
        payload["associations"] = new Dictionary<string, object?>()
        {
            { "FolderId", folderId },
            { "AddedMachineIds", Array.Empty<int>() },
            { "RemovedMachineIds", machineIds }
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
        return  HttpRequest<PersonalWorkspace>(HttpMethod.Get, "/odata/PersonalWorkspaces/UiPath.Server.Configuration.OData.GetPersonalWorkspace");
    }

    public IEnumerable<PersonalWorkspace> GetPersonalWorkspaces()
    {
        return GetEnumerable<PersonalWorkspace>("/odata/PersonalWorkspaces");
    }

    // 現在、この API は OAuth をサポートしていないため呼び出せない。
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
        return GetEnumerable<Job>("/odata/Jobs", folderId, $"{filter}&$expand=Robot,Machine,Release{order}", skip, first);
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
        var payload = new Dictionary<string, object>
        {
            ["strategy"] = force ? "2" : "1",
            ["jobIds"] = jobIds
        };

        HttpRequest(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.StopJobs", folderId, payload);
    }

    // TODO: これ使ってないけど、実装をきれいにできたい。
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
        return HttpRequest<CreatedMachine>(HttpMethod.Post, "/odata/Machines", null, machine);
    }

    public MachineClientSecretResponse[]? GetMachineClientSecret(Guid licenseKey)
    {
        return HttpRequest<MachineClientSecretResponse[]>(HttpMethod.Get, $"/api/clientsecrets/{licenseKey}");
    }

    public MachineClientSecretResponse? AddMachineClientSecret(Guid licenseKey)
    {
        return HttpRequest<MachineClientSecretResponse>(HttpMethod.Post, $"/api/clientsecrets/{licenseKey}");
    }

    public void DeleteMachineClientSecret(string secretId)
    {
        HttpRequest(HttpMethod.Delete, $"/api/clientsecrets/{secretId}");
    }

    public void PatchMachine(ExtendedMachine machine)
    {
        // 空文字列が返る
        HttpRequest(HttpMethod.Patch, $"/odata/Machines({machine.Id!.Value})", null, machine);
    }

    public void RemoveMachine(Int64 machineId)
    {
        HttpRequest(HttpMethod.Delete, $"/odata/Machines({machineId})");
    }

    public void RemoveMachines(IEnumerable<Int64> machineIds)
    {
        var payload = new Dictionary<string, object>
        {
            ["machineIds"] = machineIds
        };
        // なぜか Post で正しい。
        HttpRequest(HttpMethod.Post, $"/odata/Machines/UiPath.Server.Configuration.OData.DeleteBulk", null, payload);
    }

    #endregion

    #region Packages

    public IEnumerable<Library> GetLibraries(string? feedId = null)
    {
        //return GetEnumerable<Library>("/odata/Libraries?$orderby=Id%20desc"); // なぜか動かない？
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
        if (ApiVersion < 12) return null; // 11.1 では Not found になることを確認済み TODO: 12 以降では？ New-OrchProcess で確認。
        string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetPackageMainEntryPoint(key='{HttpUtility.UrlEncode(packageId)}:{packageVersion}')";
        if (!string.IsNullOrEmpty(feedId))
        {
            return HttpRequest<PackageEntryPoint>(HttpMethod.Get, endPoint, null, $"&feedId={feedId}");
        }
        else
        {
            return HttpRequest<PackageEntryPoint>(HttpMethod.Get, endPoint);
        }
    }

    public IEnumerable<PackageEntryPoint> GetPackageEntryPoints(string? feedId, string packageId, string packageVersion)
    {
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
        // MultipartFormDataContentを作成
        using var content = new MultipartFormDataContent("----UiPathOrchBoundary");

        // ファイル内容をByteArrayContentとして読み込む
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // contentにファイル内容を追加
        content.Add(fileContent, "uploads[]", fileName);

        //var request = new HttpRequestMessage(HttpMethod.Post, $"/odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage()?feedId={feedId}");
        var request = new HttpRequestMessage(HttpMethod.Post, _base_url + "/odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage")
        {
            Content = content
        };

        // HTTP POSTリクエストを送信し、レスポンスを取得
        var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var objBody = JsonSerializer.Deserialize<HttpBodyValues<BulkItemDtoOfString>>(body)!;
        return objBody.value?[0];
    }


    public BulkItemDtoOfString? UploadLibrary(string libraryFilePath)
    {
        var fileName = System.IO.Path.GetFileName(libraryFilePath);
        var fileContent = File.ReadAllBytes(libraryFilePath);
        return UploadLibrary(fileName, fileContent);
    }

    public BulkItemDtoOfString? UploadPackage(string? feedId, string fileName, byte[] file)
    {
        // MultipartFormDataContentを作成
        using var content = new MultipartFormDataContent("----UiPathOrchBoundary");
        // 送信するファイルの情報を追加

        // ファイル内容をByteArrayContentとして読み込む
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // contentにファイル内容を追加
        content.Add(fileContent, "uploads[]", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, _base_url + $"/odata/Processes/UiPath.Server.Configuration.OData.UploadPackage()?feedId={feedId}")
        {
            Content = content
        };

        // HTTP POSTリクエストを送信し、レスポンスを取得
        var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        // レスポンス内容を読み取る
        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var objBody = JsonSerializer.Deserialize<HttpBodyValues<BulkItemDtoOfString>>(body)!;
        return objBody.value?[0];
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

        var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        var contentDisposition = response.Content.Headers.ContentDisposition;

        string? ret = null;
        if (contentDisposition is not null)
        {
            // "filename*" を優先して使用する
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

        // ReadAsByteArrayAsync の結果を非同期で待機
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

        var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        var contentDisposition = response.Content.Headers.ContentDisposition;

        string? ret = null;
        if (contentDisposition is not null)
        {
            // "filename*" を優先して使用する
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
        return HttpRequest<Release>(HttpMethod.Get, $"/odata/Releases({releaseId})", folderId, query);
    }

    public Release? PostRelease(Int64 folderId, Release release)
    {
        // OC 22.10.1 (15.0) で動作確認済み POST /odata/Releases
        // OC 23.4.0 (16.0) で動作確認済み POST /odata/Releases
        // OC 23.10.6 (17.0) で動作確認済み POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease
        if (ApiVersion >= 17)
        {
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.CreateRelease", folderId, release);
        }
        else
        {
            // 11.1 では SpecificPriorityValue が not null だとエラーになることを確認済み
            // TODO: 12 以降ではどうか？
            if (ApiVersion < 12 && release.SpecificPriorityValue is not null)
            {
                if      (release.SpecificPriorityValue >= 61) release.JobPriority = "High";
                else if (release.SpecificPriorityValue <= 30) release.JobPriority = "Low";
                else                                          release.JobPriority = "Normal";
            }
            release.VideoRecordingSettings = null;
            release.RetentionAction = null;
            release.RetentionPeriod = null;
            release.RetentionBucketId = null;
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases", folderId, release);
        }
    }

    // 非公開の API だな。
    public void EditRelease(Int64 folderId, Release release)
    {
        // 何も返さない
        HttpRequest(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.EditRelease", folderId, release);
    }

    #region ReleaseRetention
    public ReleaseRetentionSetting? GetReleaseRetention(Int64 folderId, Int64 releaseId)
    {
        // API ver が 16.0 の場合には、リテンションポリシーを読み取れなかった。
        // API ver が 17.0 の場合には、リテンションポリシーを読み取れた。
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

    public void UpdateReleaseToLatstVersionBulk(Int64 folderId, IEnumerable<Int64> processIds)
    {
        var payload = new Dictionary<string, object>()
        {
            { "releaseIds", processIds },
            { "mergePackageTags", false }
        };

        HttpRequest(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersionBulk", folderId, payload);
    }

    public void UpdateReleaseToLatestVersion(Int64 folderId, Int64 processId)
    {
        HttpRequest(HttpMethod.Post, $"/odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersion?mergePackageTags=false", folderId, (object?)null);
    }

    public void UpdateReleaseToSpecificVersion(Int64 folderId, Int64 processId, string version)
    {
        var payload = new Dictionary<string, object>()
        {
            { "packageVersion", version },
        };

        HttpRequest(HttpMethod.Post, $"/odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToSpecificPackageVersion", folderId, payload);
    }

    public void RollbackReleaseVersion(Int64 folderId, Int64 processIds)
    {
        HttpRequest(HttpMethod.Post, $"/odata/Releases({processIds})/UiPath.Server.Configuration.OData.RollbackToPreviousReleaseVersion?mergePackageTags=false", folderId, (object?)null);
    }

    public void CreateRelease(Int64 folderId, Package package, Int64? entryPointId = null)
    {
        var payload = new Dictionary<string, object>()
        {
            { "Name", package.Id! },
            { "Description", package.Description! },
            { "ProcessKey", package.Id! },
            { "ProcessVersion", package.Version! },
        };
        if (entryPointId.HasValue)
        {
            payload["EntryPointId"] = entryPointId;
        }
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

    // トリガーの ExecutorRobots を取得
    public Int64[]? GetRobotIdsForSchedule(Int64 folderId, Int64 processScheduleId)
    {
        return HttpRequest<HttpBodyValue<Int64[]>>(HttpMethod.Get, $"/odata/ProcessSchedules/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule(key={processScheduleId})", folderId)?.value;
    }

    public ProcessSchedule? PostProcessSchedule(Int64 folderId, ProcessSchedule schedule)
    {
        return HttpRequest<ProcessSchedule>(HttpMethod.Post, "/odata/ProcessSchedules", folderId, schedule);
    }

    public void PutProcessSchedule(Int64 folderId, ProcessSchedule schedule)
    {
        // 何も返さない
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

    #endregion

    #region Buckets

    public IEnumerable<Bucket> GetBuckets(Int64 folderId)
    {
        return GetEnumerable<Bucket>("/odata/Buckets", folderId);
    }

    public Bucket? PostBucket(Int64 folderId, Bucket bucket)
    {
        return HttpRequest<Bucket>(HttpMethod.Post, "/odata/Buckets", folderId, bucket);
    }

    public void DeleteBucket(Int64 folderId, Int64 bucketId)
    {
        string body = HttpRequest(HttpMethod.Delete, $"/odata/Buckets({bucketId})", folderId);
    }

    public IEnumerable<BlobFile> GetBucketDirectories(Int64 folderId, Int64 bucketId)
    {
        return GetEnumerable<BlobFile>($"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetDirectories", folderId, "&directory=%2F&recursive=true");
    }

    public IEnumerable<BlobFile> GetBucketFiles(Int64 folderId, Int64 bucketId)
    {
        return GetEnumerable<BlobFile>($"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetFiles", folderId, "&directory=%2F&recursive=true");
    }

    public void GetBucketsAcrossFolders(Int64 folderId)
    {
        string body = HttpRequest(HttpMethod.Get, "/odata/Buckets/UiPath.Server.Configuration.OData.GetBucketsAcrossFolders", folderId);
    }

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

    // TODO: これどこからも使っていない。良くないのでは？
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

        // 何も返らない
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

        //return GetEnumerable<User>("/odata/Users", null, "&$expand=OrganizationUnits,UserRoles,UnattendedRobot"); // エラーになってしまう
    }

    public User? GetUser(Int64 userId)
    {
        return HttpRequest<User>(HttpMethod.Get, $"/odata/Users({userId})?$expand=OrganizationUnits,UserRoles");
    }

    public User? PostUser(User user)
    {
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
        throw new Exception("User not found");
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

    public DirectoryObject[]? SearchDirectory(string prefix)
    {
        return HttpRequest<DirectoryObject[]>(HttpMethod.Get, $"/api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All");
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

    // 空っぽが返ってしまう。。
    //public IEnumerable<ConsumptionLicenseStatsModel> GetStatsLicenseConsumption(int tenantId, int days)
    //{
    //    string body = HttpRequest(HttpMethod.Get, $"/api/Stats/GetConsumptionLicenseStats?tenantId={tenantId}&days={days}");
    //    yield break;
    //}

    // これも動作しない。すべて -1 が返ってきてしまう。。
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

    // Not Found が返ってしまう。。
    public IEnumerable<CountStats> GetSessionStats()
    {
        var ret = HttpRequest<CountStats[]>(HttpMethod.Get, "/api/Stats/GetSessionsStats'");
        return ret ?? [];
    }

    #endregion

    #region Asset

    public IEnumerable<Asset> GetAssets(Int64 folderId)
    {
        // なぜか UserValues.CredentialUsername が空で返ってきてしまう。不具合なのか？
        //return GetEnumerable<Asset>("/odata/Assets/UiPath.Server.Configuration.OData.GetFiltered", folderId, "&$expand=UserValues");
        return GetEnumerable<Asset>("/odata/Assets", folderId, "&$expand=UserValues");
    }

    public Asset? GetAsset(Int64 folderId, Int64 assetId)
    {
        return HttpRequest<Asset>(HttpMethod.Get, $"/odata/Assets({assetId})?$expand=UserValues", folderId);
    }

    public Asset? AddAsset(Int64 folderId, Asset asset)
    {
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

    //    // 引数で指定されない場合は、元の UserValues を保持する
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
        EnsureVersionSupport(12); // 多分もっと後ろのバージョンだと思う
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

    // TODO: このエンドポイントは、Authorization ヘッダがなくても動いているような気がする？
    public async Task<(string? FileName, byte[] FileContent)> DownloadMediaByJobId(Int64 folderId, Int64 jobId)
    {
        string endPoint = _base_url + $"/odata/ExecutionMedia/UiPath.Server.Configuration.OData.DownloadMediaByJobId(jobId={jobId})";
        var request = new HttpRequestMessage(HttpMethod.Get, endPoint);
        request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());

        var response = await _httpClient.SendAsync(request);
        EnsureSuccessStatusCode(response);

        var contentDisposition = response.Content.Headers.ContentDisposition;

        string? ret = null;
        if (contentDisposition is not null)
        {
            // "filename*" を優先して使用する
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

        // ReadAsByteArrayAsync の結果を非同期で待機
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        return (ret, responseBytes);
    }

    #endregion

    #region Session

    // クラシックフォルダのロボットを取得
    public IEnumerable<Session> GetSessions(Int64 folderId)
    {
        return GetEnumerable<Session>("/odata/Sessions", folderId, "&$expand=Robot($expand=License)");
    }

    // クラシックフォルダのロボットを有効化/無効化
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

    // テスト
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

    public IEnumerable<TestCaseExecution> GetTestCaseExecutions(Int64 folderId)
    {
        return GetEnumerable<TestCaseExecution>("/odata/TestCaseExecutions", folderId);
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
        // "null" が返るようだ
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

    public IEnumerable<TestDataQueueItem> GetTestDataQueueItems(Int64 folderId, Int64 testDataQueueId)
    {
        EnsureVersionSupport(14);
        return GetEnumerable<TestDataQueueItem>("/odata/TestDataQueueItems", folderId, $"&$filter=(TestDataQueueId%20eq%20{testDataQueueId})");
    }

    public void AddTestDataQueueItems(Int64 folderId, string testDataQueueName, string itemJsonArray)
    {
        EnsureVersionSupport(14);
        var payload = $"{{\"queueName\": \"{testDataQueueName}\",\"items\": {itemJsonArray}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/BulkAddItems", folderId, payload);
    }

    public void SetAllTestDataQueueItemsConsumed(Int64 folderId, string testDataQueueName, bool isConsumed)
    {
        EnsureVersionSupport(14);
        var payload = $"{{\"queueName\": \"{testDataQueueName}\",\"isConsumed\": {isConsumed.ToString().ToLower()}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/SetAllItemsConsumed", folderId, payload);
    }

    #endregion

    #region Calendar

    public IEnumerable<ExtendedCalendar>? GetCalendars()
    {
        return GetEnumerable<ExtendedCalendar>($"/odata/Calendars");
    }

    // ExcludedDates を取得するには、GetCalendars(id) を呼ぶ必要がある。
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
        // 何も返らない
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

    #region Platform Management

    //private static string TrimUrl(string url)
    //{
    //    // URLをスラッシュで分割
    //    string[] parts = url.Split('/');

    //    // 後ろから3番目のスラッシュまでの部分を再結合
    //    if (parts.Length > 3)
    //    {
    //        return string.Join("/", parts, 0, parts.Length - 2) + "/portal_/api/identity";
    //    }
    //    else
    //    {
    //        // 分割した結果が3パート以下の場合、そのまま返す
    //        return url;
    //    }
    //}

    // AD 連携している Platform Management の環境用
    //private IEnumerable<T> GetEnumerablePmDirectory<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    //{
    //    //string anotherBaseUrl = TrimUrl(_base_url);
    //    ////int slashIndex = _base_url.LastIndexOf('/');
    //    ////string __base_url = _base_url.Substring(0, slashIndex);

    //    ulong total = 0;
    //    while (true)
    //    {
    //        ulong top = Math.Min(first - total, 50);
    //        // パラメータ名に $ がつかない。$top とか $skip でないので要注意。
    //        //string url = $"{_base_url_identity}{endPoint}?top={top}&skip={skip}{query}";
    //        string url = $"{_base_url_identity}{endPoint}?top={top}&skip={skip}{query}";

    //        var request = new HttpRequestMessage(HttpMethod.Get, url);
    //        request.Content = new StringContent("", Encoding.UTF8, @"application/json");

    //        if (folderId.HasValue)
    //        {
    //            request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
    //        }

    //        var response = HttpClient_Send(request);
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


    // 200 で空っぽが返ってしまう。。
    public UserProfile? GetPmUserProfile()
    {
        return HttpRequestIdentity<UserProfile>(HttpMethod.Get, "/api/Account/Profile");
    }

    // このエンドポイントは、host admin でないと動かない
    public void GetPmSetting()
    {
        string body = HttpRequestIdentity(HttpMethod.Get, "/api/Setting");
    }

    public IEnumerable<PmUser> GetPmUsers(string partitionGlobalId)
    {
        if (_drive._psDrive.IsCloud)
        {
            //return GetEnumerableIdentity<PmUser>($"/api/User/users/{partitionGlobalId}");

            // 現在の Automation Cloud は次を実行しているようだ。
            return GetEnumerablePortal<PmUser>($"/api/identity/UserPartition/licenses", null, $"&partitionGlobalId={partitionGlobalId}");
        }
        else
        {
            // obsoleted なんだけど、MSI OC ではこっちを呼び出さないといけないんだな。。
            return GetEnumerableIdentity<PmUser>($"/api/UserPartition/users/{partitionGlobalId}");
        }
    }

    // entityType: "user", "group", or "application"
    // "robot" を渡すとエラーになる。
    public Dictionary<string, PmGroupMember>? PmBulkResolveByName(string partitionGlobalId, string entityType, IEnumerable<string> names)
    {
        var postdata = new BulkResolveByNameCommand()
        {
            entityNames = names as string[] ?? names.ToArray(),
            entityType = entityType
        };

        string body = HttpRequestIdentity(HttpMethod.Post, $"/api/Directory/BulkResolveByName/{partitionGlobalId}", null, postdata);
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

    // この API は無効化されているため使えない
    public void GetPmUserLoginAttempts(string userId)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/User/{userId}/loginAttempts");
    }

    public void PutPmUser(string userId, PowerShell.Entities.UpdateUserCommand command)
    {
        // {"succeeded":true,"errors":[]} みたいのが返るけど、無視で良いか。
        // エラーのときには例外で処理しているしな。。
        HttpRequestIdentity(HttpMethod.Put, $"/api/User/{userId}", null, command);
    }

    public void RemovePmUser(string userId)
    {
        HttpRequestIdentity(HttpMethod.Delete, $"/api/User/{userId}");
    }

    //
    public void PutPmUserSetting(UpdatePmUserSettingPayload payload)
    {
        //HttpRequestPortal(HttpMethod.Put, $"/portal_/api/identity/Setting", null, payload);
        HttpRequestIdentity(HttpMethod.Put, $"/api/Setting", null, payload);
    }

    public PmGroup[]? GetPmGroups(string partitionGlobalId)
    {
        return GetEnumerableWithoutPagingIdentity<PmGroup>($"/api/Group/{partitionGlobalId}");
    }

    public PmGroup[]? GetPmGroups2(string partitionGlobalId)
    {
        return GetEnumerableWithoutPagingPortal<PmGroup>($"/api/identity/Group/{partitionGlobalId}/licenses");
    }

    // 非公開の API だな。。なんじゃこりゃ簡単に URL を構築できない。
    // "/portal_/api/orchestrator/tags/yotsuda/svc3?skip=0&take=10&startsWith=&type=Label"
    //public void GetTags()
    //{
    //    string body = HttpRequestPortal(HttpMethod.Get, "/api/orchestrator/tags");
    //}

    // 非公開の API だな。。
    public IEnumerable<PmAuditLog> GetPmAuditLog(string? partitionGlobalId, string? query, ulong skip, ulong first)
    {
        if (string.IsNullOrEmpty(partitionGlobalId)) return [];
        return GetEnumerablePortal<PmAuditLog>($"/api/auditLog/{partitionGlobalId}", null, query, skip, first);
    }

    // 非公開の API だな。。
    public AvailableUserBundles? GetPmLicensedGroupsAvailableLicenses(string? groupId)
    {
        if (groupId is null) return null;
        return HttpRequestPortal<AvailableUserBundles>(HttpMethod.Get, $"/api/license/accountant/UserLicense/group/?id={groupId}");
    }

    // 非公開の API だな。。
    public IEnumerable<NuLicensedGroup> GetPmLicensedGroups()
    {
        return GetEnumerablePortal<NuLicensedGroup>("/api/license/accountant/UserLicense/group/page");
    }

    private class RemovePmLicensedGroupCommand
    {
        public string? id { get; set; }
    }

    // 非公開の API だな。。
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

    // 非公開の API だな。。
    public IEnumerable<NuLicensedGroupMember> GetPmLicenseGroupAllocations(string? groupId)
    {
        return GetEnumerablePortal<NuLicensedGroupMember>($"/api/license/accountant/UserLicense/group/{groupId}/allocations");
    }

    // 非公開の API だな。。
    public UpdateLicensedGroupResponse? PutPmLicenseGroup(UpdateLicensedGroupCommand command)
    {
        return HttpRequestPortal<UpdateLicensedGroupResponse>(HttpMethod.Put, "/api/license/accountant/UserLicense/group", null, command);
    }

    // 非公開の API だな。。
    public void DeletePmLicenseGroupAllocations(string? groupId, string userId)
    {
        HttpRequestPortal(HttpMethod.Delete, $"/api/license/accountant/UserLicense/group/{groupId}/user/{userId}");
    }

    // 非公開の API だな。。
    public IEnumerable<NuLicensedUser> GetPmLicensedUsers()
    {
        return GetEnumerablePortal<NuLicensedUser>("/portal_/api/license/accountant/UserLicense/user/page");
    }

    private static readonly JsonSerializerOptions jsoMemberConverter = new()
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

    // なぜか partitionGlobalId は不要。
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

    // なぜかpartitionGlobalId は不要
    public IEnumerable<ExternalResource> GetPmExternalApiResource()
    {
        return GetEnumerableWithoutPagingIdentity<ExternalResource>("/api/ExternalApiResource") ?? [];
    }

    public IEnumerable<ExternalClient> GetPmExternalClient(string partitionGlobalId)
    {
        return GetEnumerableWithoutPagingIdentity<ExternalClient>($"/api/ExternalClient/{partitionGlobalId}") ?? [];
    }

    // Forbidden for external app
    //public IEnumerable<int> GetIdentityClient()
    //{
    //    return GetEnumerableIdentity<int>($"/Client");
    //}

    // TODO: Get-OrchIdDirectoryConfiguration cmdlet とか作れそう。
    // completer には、GetIdentityAvailableDirectoryTypes() の戻りを使えば良い。
    // でも何の役に立つのか、、
    // identifier には directory adapter を渡す必要がある。
    // "aad" とか "Saml2" とか "scim" とか。
    public void GetPmDirectoryConfiguration(string identifier)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/DirectoryConnection/DirectoryConfiguration?identifier={identifier}");
    }

    // ["aad","Saml2","scim"] みたいなものが返る。
    public string[]? GetPmAvailableDirectoryTypes()
    {
        return HttpRequestIdentity<string[]>(HttpMethod.Get, "/api/DirectoryConnection/AvailableDirectoryTypes");
    }

    // 空の配列が返るようだ。うーん。
    public void GetPmExternalIdentityProvider(string partitionGlobalId)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/ExternalIdentityProvider?partitionGlobalId={partitionGlobalId}");
    }

    // Forbidden for external app
    //public void GetIdentityResource()
    //{
    //    string body = HttpRequestIdentity(HttpMethod.Get, "/IdentityResource");
    //}

    // うまく動く。
    // TODO: Get-OrchIdLanguage cmdlet を実装する。
    public void GetPmLanguage()
    {
        string body = HttpRequestIdentity(HttpMethod.Get, "/api/Language");
    }

    public void GetIdentitySetting(string partitionGlobalId, string userId)
    {
        HttpRequestIdentity(HttpMethod.Get, $"/api/Setting", null, (object)$"&partitionGlobalId={partitionGlobalId}&userId={userId}");
    }

    // 空の配列が返るようだ。うーん。
    public void GetPmRule(string partitionGlobalId)
    {
        string body = HttpRequestIdentity(HttpMethod.Get, $"/api/Rule/{partitionGlobalId}");
    }

    // 残念、機密アプリでは動作しない。空が返る。
    // 非機密アプリで呼び出した場合でも、GetCurrentUser の方がリッチな情報を得られる。使えない。。
    //public void GetPmUserOrgsInfo()
    //{
    //    string body = HttpRequestPm(HttpMethod.Get, "/api/UserOrgs/userOrgsLocalByAuth0Token");
    //}

    // Forbidden for external app
    //public void GetPmUserOrgsInfo(string email)
    //{
    //    string body = HttpRequestIdentity(HttpMethod.Get, $"/api/UserOrgs/userOrgs?email={email}");
    //}

    public PmAuthenticationRoot? GetPmAuthenticationSettings(string partitionGlobalId)
    {
        var body = HttpRequestIdentity<Dictionary<string, PmAuthenticationRoot>>(HttpMethod.Get, $"/api/AuthenticationSetting/getAll/{partitionGlobalId}");
        return body?.FirstOrDefault().Value;
    }

    #endregion

    #region Document Understanding

    public DuProject[]? GetDuProjects()
    {
        var body = HttpRequest<DuGetProjectsResponse>(HttpMethod.Get, "/du_/api/framework/projects?api-version=1");
        return body?.projects;
    }

    // どうも動かない。。困ったな
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

    // TODO: paging をサポートしないといけないのではないか？
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

    // TODO: paging をサポートしないといけないのではないか？
    public DuRole[]? GetDuRoles(string? partitionGlobalId)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/roles?scopeType=project&serviceName=DocumentUnderstanding";

        string body = HttpRequestImpl(HttpMethod.Get, baseUrl, "");
        var results = JsonSerializer.Deserialize<HttpBodyResults<DuRole>>(body);
        return results?.results;
    }

    // 何も返さない
    public void SetDuRoleToDuUser(string? partitionGlobalId, UserRoleAssignmentsCmd payload)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/userroleassignments";
        HttpRequestImpl(HttpMethod.Patch, baseUrl, "", null, payload);
    }
    #endregion

    #region TestManager

    // PagingModel でページング
    private IEnumerable<T> GetEnumerableTm<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, 100);
            string url = $"{_base_url}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = HttpClient_Send(request);
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

    // PagingModel2 でページング
    private IEnumerable<T> GetEnumerableTm2<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, 100);
            string url = $"{_base_url}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = HttpClient_Send(request);
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

    // paging なし
    private IEnumerable<T> GetEnumerableTm3<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, 1000);
            string url = $"{_base_url}{endPoint}?top={top}&skip={skip}{query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = HttpClient_Send(request);
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
                if (total == first) // TODO: 正しい？
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
        // 何も返らない
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
        // 空が返る
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
}

#pragma warning restore IDE1006 // 命名スタイル
