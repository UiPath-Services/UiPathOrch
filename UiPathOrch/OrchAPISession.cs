// -----------------------------------------------------------------------------
// This file is large (~3,700 lines, ~170 API methods) ON PURPOSE — do not split it.
//
// It is a flat REGISTRY of independent REST calls, not a "god class": each method
// is thin, wrapping one endpoint, with no shared mutable state or tangled control
// flow between methods, so the size carries none of the coupling/complexity that
// makes large files harmful. Splitting into per-domain partials would not lower
// coupling (there is none to lower); it would only cost what this layout gives
// for free — one grep / one scroll across the whole API surface, and a trivial
// place to add the next call. C# compiles per-assembly, so there is no build-time
// win. If a method here grows real logic, extract that logic to a helper rather
// than splitting the file (see Jwt.cs for the pattern).
// -----------------------------------------------------------------------------

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
    private readonly object refillLock = new();

    public RateLimiter(int maxRequestsPerSecond)
    {
        this.maxRequestsPerSecond = maxRequestsPerSecond;
        this.rateLimitSemaphore = new SemaphoreSlim(maxRequestsPerSecond, maxRequestsPerSecond);

        // Set up a timer to refill rate limit tokens every second
        refillTimer = new Timer(RefillRateLimitTokens!, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(1));
    }

    private void RefillRateLimitTokens(object state)
    {
        // Serialize refills. System.Threading.Timer can fire overlapping callbacks if one is delayed
        // (thread-pool starvation); without this lock two refills could each read CurrentCount and
        // Release up to max, pushing the semaphore past its cap and throwing SemaphoreFullException.
        // Consumers only Wait() (never Release), so the timer is the sole releaser — locking makes the
        // read-then-release atomic and keeps the count within [0, maxRequestsPerSecond].
        lock (refillLock)
        {
            int tokensToRelease = maxRequestsPerSecond - rateLimitSemaphore.CurrentCount;
            if (tokensToRelease > 0)
            {
                rateLimitSemaphore.Release(tokensToRelease);
            }
        }
    }

    public void Wait() => rateLimitSemaphore.Wait();

    public async Task WaitAsync(CancellationToken cancellationToken = default) => await rateLimitSemaphore.WaitAsync(cancellationToken);

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
    internal int NextCallId() => Interlocked.Increment(ref http_call_num);
    // Sticky "authentication is broken" latch (tripped when a freshly re-issued token
    // is still 401). Reset only by rebuilding the session (Import-OrchConfig).
    private readonly AuthCircuitBreaker _authBreaker = new();

    // The single send chokepoint. SendOnce does the actual send + per-attempt logging;
    // this wrapper layers the retry / recovery policy on top (HttpRetryPolicy):
    //   - 401: clear auth, re-authenticate and retry ONCE. A token that merely expired or
    //     rotated recovers; if the freshly issued token is still 401 the credential is
    //     genuinely broken, so trip the circuit breaker (later calls fail fast instead of
    //     re-auth+retry per item) and surface the 401 for this call.
    //   - 429 / 503 / 504: back off (honoring Retry-After) and retry up to MaxTransientRetries.
    // An explicitly supplied httpClient is the auth flow's own client: send once, no
    // retry/reauth, to avoid recursion.
    private HttpResponseMessage HttpClient_Send(HttpRequestMessage message, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        // Add the on-premises tenant header once, here (not per attempt in SendOnce), so
        // a retried clone carries it exactly once.
        if (!string.IsNullOrEmpty(_authManager.OnpremiseTenancy))
        {
            message.Headers.TryAddWithoutValidation("X-UIPATH-TenantName", _authManager.OnpremiseTenancy);
        }

        // Make the blocking send (and the backoff between retries) interruptible by Ctrl+C
        // even though no caller threads a token today. A fresh handler per call is the
        // right scope — a CancellationTokenSource is one-shot, so a session-wide one
        // couldn't be reused after the first cancellation. This is the single chokepoint,
        // so it covers every path (the commented-out intent in the GetEnumerable* helpers).
        using ConsoleCancelHandler? localCancel = cancellationToken.CanBeCanceled ? null : new ConsoleCancelHandler();
        CancellationToken ct = localCancel?.Token ?? cancellationToken;

        if (httpClient != null)
        {
            return SendOnce(message, httpClient, ct);
        }

        // Buffer the body once: an HttpRequestMessage can only be sent a single time, so
        // each retry must go on a fresh message rebuilt from these bytes.
        byte[]? bodyBytes = null;
        List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders = null;
        if (message.Content != null)
        {
            bodyBytes = message.Content.ReadAsByteArrayAsync(ct).GetAwaiter().GetResult();
            contentHeaders = message.Content.Headers.ToList();
        }

        bool reauthUsed = false;
        int transientAttempt = 0;
        bool firstAttempt = true;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            HttpRequestMessage attempt = firstAttempt
                ? message
                : CloneRequest(message, bodyBytes, contentHeaders);
            firstAttempt = false;

            HttpResponseMessage response = SendOnce(attempt, null, ct);

            // 503/504 may arrive after a write already committed server-side, so a non-idempotent
            // POST (create/add) must not be retried on them (429 stays retryable -- it means the
            // request was rejected unprocessed). PATCH/PUT/DELETE/GET re-apply safely.
            bool isIdempotent = message.Method != HttpMethod.Post;
            HttpRetryPolicy.Action action = HttpRetryPolicy.Decide(response.StatusCode, isIdempotent, reauthUsed, transientAttempt);

            if (action == HttpRetryPolicy.Action.Reauth)
            {
                response.Dispose();
                ClearAuthentication();   // next SendOnce re-acquires the token via the HttpClient getter
                reauthUsed = true;
                continue;
            }

            if (action == HttpRetryPolicy.Action.Backoff)
            {
                TimeSpan? retryAfter = HttpRetryPolicy.ResolveRetryAfter(response.Headers.RetryAfter, DateTimeOffset.Now);
                TimeSpan delay = HttpRetryPolicy.BackoffDelay(transientAttempt, retryAfter);
                response.Dispose();
                transientAttempt++;
                if (delay > TimeSpan.Zero && ct.WaitHandle.WaitOne(delay))
                {
                    ct.ThrowIfCancellationRequested();
                }
                continue;
            }

            // Returning. If a freshly re-authenticated token is still 401, the credential
            // is genuinely broken — latch the breaker so later calls fail fast.
            if (response.StatusCode == HttpStatusCode.Unauthorized && reauthUsed)
            {
                _authBreaker.Trip(new HttpResponseException(
                    "Authentication failed: a newly issued access token was still rejected with 401. " +
                    "Verify the application / PAT scopes and validity, then run Import-OrchConfig.",
                    new HttpResponseMessage(HttpStatusCode.Unauthorized)));
            }
            return response;
        }
    }

    // Clone a request so a retry can be sent (the original is already spent). Copies the
    // method, URI, version, request headers, and the buffered body + content headers.
    private static HttpRequestMessage CloneRequest(HttpRequestMessage src, byte[]? body, List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders)
    {
        var clone = new HttpRequestMessage(src.Method, src.RequestUri) { Version = src.Version };
        foreach (var header in src.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        if (body != null)
        {
            clone.Content = new ByteArrayContent(body);
            clone.Content.Headers.Clear();
            if (contentHeaders != null)
            {
                foreach (var header in contentHeaders)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }
        return clone;
    }

    private HttpResponseMessage SendOnce(HttpRequestMessage message, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // PAT mode never enters an auth flow, so trigger the one-shot
        // PSDrive settings dump here on the first HTTP call. The flag
        // inside AuthManager guards against duplicate writes for the
        // other auth modes (which already log via their own flow).
        _authManager.LogAuthSettings();

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
            reqTime = DateTime.Now;
            HttpClient hc = httpClient ?? HttpClient;
            ret = hc.Send(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resTime = DateTime.Now;

            // Primary ApiVersion discovery: read the `api-supported-versions`
            // response header (always present on Orchestrator responses, no
            // scope dependency). Set on the very first API response in the
            // session so subsequent per-version routing decisions see it.
            // The fallback path in EnsureAuthenticated (GetActivitySettings,
            // gated on OR.Settings scope) remains as defense-in-depth; the
            // `ApiVersion is null` guard there means it only runs when this
            // primary path didn't fire (e.g. on a hypothetical Orchestrator
            // version that omits the header). Concurrent first responses converge
            // on the same value and publish it through ApiVersion's volatile-flag
            // setter, so a reader never observes a torn value.
            if (ApiVersion is null && ret.Headers.TryGetValues("api-supported-versions", out var apiVersionHeaders))
            {
                double max = 0;
                foreach (var entry in apiVersionHeaders)
                {
                    foreach (var token in entry.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double n) && n > max)
                            max = n;
                    }
                }
                if (max > 0) ApiVersion = max;
            }

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

    // Token-expiry instant, stored as ticks so it can be published with
    // Volatile.Read/Write for the lock-free fast-path check in EnsureAuthenticated
    // (DateTime cannot be declared volatile). Writes happen under _authLock; the
    // outer fast-path read is lock-free and re-checked under the lock.
    private long _expiryTimeTicks; // ticks of the exact time when the token expires
    private readonly OrchestratorAuthManager _authManager;

    internal OrchestratorAuthManager AuthManager
    {
        get { return _authManager; }
    }

    internal readonly string _base_url;
    // Base for Orchestrator-specific API calls (/odata/..., /api/...).
    // Cloud's gateway accepts these directly off the tenant root, but Automation Suite
    // routes by service segment and refuses paths that don't start with /orchestrator_/.
    // For AS this is _base_url + "/orchestrator_"; for Cloud and on-prem it equals _base_url.
    internal readonly string _base_url_orchestrator;
    internal readonly string _base_url_identity;
    internal readonly string _base_url_portal;
    internal volatile bool _isAuthenticated = false;
    private bool _disposed = false;
    private readonly OrchDriveInfo _drive;
    // ApiVersion is discovered from the first API response (see SendOnce) and read
    // by many per-version routing gates, potentially from parallel request threads
    // (bucket / identity fan-out). A bare `double?` is two fields (hasValue + value)
    // that are neither written atomically nor safely published, so a reader could
    // observe hasValue=true with a stale value=0 and mis-route to a legacy branch.
    // Publish through a volatile flag: the volatile write of _apiVersionKnown orders
    // the preceding _apiVersion write before it, and a reader that sees the flag set
    // is guaranteed to see the value. Concurrent writers converge on the same number,
    // so the value write itself is not torn in practice.
    private double _apiVersion;
    private volatile bool _apiVersionKnown;
    public double? ApiVersion
    {
        get => _apiVersionKnown ? _apiVersion : null;
        set
        {
            if (value is double v) { _apiVersion = v; _apiVersionKnown = true; }
            else { _apiVersionKnown = false; _apiVersion = 0; }
        }
    }

    // Deferred warning message to be displayed when a cmdlet runs (not during tab completion)
    internal string? PendingWarning { get; set; }
    internal void ClearPendingWarning() => PendingWarning = null;

    // Append a paragraph to the deferred warning. Producers concatenate with
    // "\n\n"; the BeginProcessing drain splits on that and emits each paragraph
    // as its own WriteWarning. Appending (not overwriting) lets independent
    // advisories — e.g. the IgnoreSslErrors notice and the Entra-ID local-user
    // notice — coexist on the same drive instead of clobbering each other.
    internal void AppendPendingWarning(string warning)
        => PendingWarning = string.IsNullOrEmpty(PendingWarning)
            ? warning
            : PendingWarning + "\n\n" + warning;

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

        AppendPendingWarning(warning);
    }

    #region Authentication

    internal HttpClient InitializeHttpClient(OrchDriveInfo drive)
    {
        // SocketsHttpHandler with proxy config, an optional SSL-error override, and (for direct
        // connections) an RFC 8305 Happy Eyeballs dialer that survives NAT64/DNS64 networks
        // where the default in-order dialer hangs in a black-holed TLS handshake. See OrchHttp.
        var handler = OrchHttp.CreateHandler(
            drive._psDrive.Proxy,
            drive._psDrive.IgnoreSslErrors.GetValueOrDefault(false));

        var ret = new HttpClient(handler);

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

        _base_url_orchestrator = drive._psDrive.ResolvedEdition == OrchEdition.AutomationSuite
            ? _base_url + "/orchestrator_"
            : _base_url;

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

    // Token expiry derived from the IdP's reported `expires_in`; falls back to a
    // conservative 1 hour when the auth flow doesn't report a lifetime (PAT /
    // user-password), so behavior is unchanged for those modes. Using the real
    // lifetime avoids a too-late refresh (and the resulting 401 surfaced to the
    // caller) when the IdP issues tokens shorter than 1h, e.g. on Automation
    // Suite / on-premises Identity policies.
    private DateTime ComputeTokenExpiry(DateTime from) => ComputeTokenExpiry(from, _authManager.ExpiresInSeconds);

    // Pure expiry decision, separated for unit testing: use the IdP-reported
    // lifetime when positive, else the conservative 1h fallback.
    internal static DateTime ComputeTokenExpiry(DateTime from, int expiresInSeconds) => expiresInSeconds > 0 ? from.AddSeconds(expiresInSeconds) : from.AddHours(1);

    // Test-only: mark the session authenticated with a far-future expiry and suppress the one-time
    // Entra-ID warning probe, so provider operations that gate on EnsureAuthenticated (e.g. the
    // Get-ChildItem enumerate path) run against a seeded catalog without any network call.
    internal void MarkAuthenticatedForTest()
    {
        _isAuthenticated = true;
        Volatile.Write(ref _expiryTimeTicks, DateTime.Now.AddHours(1).Ticks);
        EntraIdWarningChecked = true;
    }

    internal void EnsureAuthenticated()
    {
        // Fail fast if a prior call proved the credential is broken (re-issued token still
        // 401). Avoids every call in a bulk operation re-authenticating + retrying anew.
        _authBreaker.ThrowIfTripped();

        if (!_isAuthenticated)
        {
            lock (_authLock)
            {
                if (!_isAuthenticated)
                {
                    // Set initial token
                    string token = _authManager.RequestToken();
                    if (!SetToken(token))
                    {
                        // A 200 token response with no access_token leaves the session
                        // with no Bearer header. Don't mark it authenticated behind a
                        // fresh expiry (every later call would 401 until the expiry
                        // lapsed); fail now so the caller surfaces / retries.
                        throw new Exception("Authentication returned an empty access token.");
                    }

                    _isAuthenticated = true;
                    Volatile.Write(ref _expiryTimeTicks, ComputeTokenExpiry(DateTime.Now).Ticks);

                    // The Scope gate is an optimization that skips a doomed call when
                    // the OAuth request is known not to include OR.Settings. A PAT is
                    // opaque — its scopes cannot be known client-side — so for PAT
                    // drives always attempt the fetch; the catch below already falls
                    // back gracefully when the token lacks the scope.
                    if (ApiVersion is null && _drive is not null &&
                        ((_drive._psDrive.Scope?.Contains("OR.Settings") ?? false)
                         || !string.IsNullOrEmpty(_drive._psDrive.AccessToken)))
                    {
                        try
                        {
                            var activitySettings = _drive.ActivitySettings.Get();
                            if (double.TryParse(activitySettings?.ApiVersion, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var version))
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
        if (now > new DateTime(Volatile.Read(ref _expiryTimeTicks)).AddMinutes(-5))
        {
            lock (_authLock)
            {
                if (now > new DateTime(Volatile.Read(ref _expiryTimeTicks)).AddMinutes(-5))
                {
                    if (SetToken(RenewAccessToken()))
                    {
                        Volatile.Write(ref _expiryTimeTicks, ComputeTokenExpiry(now).Ticks);
                    }
                    else
                    {
                        // Refresh returned an empty token. Keeping the old (expiring)
                        // Bearer header while advancing expiry would pin the stale
                        // token until a 401; force a full re-auth on the next call.
                        _isAuthenticated = false;
                        throw new Exception("Token refresh returned an empty access token; re-authentication required.");
                    }
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
            throw new HttpResponseException(CapErrorBody(GetBody(response)), response);
        }
    }

    // Bucket blob I/O targets pre-signed storage URLs (Azure / S3) sent via the auth-less
    // bucket-item client, so a non-success here is a STORAGE error, not an Orchestrator-auth
    // problem. Surface the storage response body for diagnostics (the framework's bare
    // EnsureSuccessStatusCode() throws an HttpRequestException with no body) but, unlike
    // EnsureSuccessStatusCode above, do NOT clear the Orchestrator session auth on a 401 —
    // that 401 comes from storage and is unrelated to the Orchestrator token.
    private void EnsureBlobSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpResponseException(CapErrorBody(GetBody(response)), response);
        }
    }

    // Upper bound on the response-body text that becomes the exception MESSAGE.
    // The full body stays on HttpResponseException.Response for programmatic use;
    // this only bounds what OrchException.ExtractMessage can echo into
    // Start-Transcript / CI logs when the body is non-JSON or lacks known error
    // fields. 8 KB is generous enough that real Orchestrator JSON error envelopes
    // still parse, while a multi-MB HTML/gateway dump can't flood the transcript.
    // Mirrors the cap the Invoke-OrchApi error path already applies.
    internal const int MaxErrorBodyChars = 8192;

    internal static string CapErrorBody(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Length <= MaxErrorBodyChars)
        {
            return body;
        }
        return body.Substring(0, MaxErrorBodyChars) + "… [truncated]";
    }

    private void EnsureVersionSupport(float requiredVersion)
    {
        if (ApiVersion < requiredVersion)
        {
            // DeterministicApiException so the cache layer remembers the
            // failure (ApiVersion is fixed for the connection's lifetime).
            //throw new DeterministicApiException($"Orchestrator API Version {ApiVersion:F1} does not support this operation. Required version: {requiredVersion:F1}.");
            throw new DeterministicApiException($"Orchestrator API Version {ApiVersion:F1} does not support this operation.");
        }
    }

    // Named, null-safe wrappers over OrchApiFloor for this session's discovered ApiVersion.
    // Supports == `ApiVersion >= floor`, Below == `ApiVersion < floor`; both are false when the
    // version is unknown (null), matching the historic inline comparisons. See OrchApiFloor.
    private bool Supports(double floor) => OrchApiFloor.Supports(ApiVersion, floor);

    private bool Below(double floor) => OrchApiFloor.Below(ApiVersion, floor);

    // Returns true when a usable (non-empty) token was applied to the client.
    // The bool lets the (re)auth flow avoid advancing expiry / marking the
    // session authenticated when a token endpoint returns a 200 with no
    // access_token (GetAccessToken yields "" in that case) — otherwise the
    // stale Bearer header would be pinned behind a fresh expiry until a 401.
    private bool SetToken(string? access_token)
    {
        if (!IsTokenApplied(access_token))
        {
            return false;
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
        return true;
    }

    // Pure guard, separated for unit testing: a token is usable only when non-empty.
    internal static bool IsTokenApplied(string? access_token) => !string.IsNullOrEmpty(access_token);

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

    // Single offset-paging loop shared by the OData / identity / portal enumerators.
    // `fetchPage(top, skip)` performs one request and returns that page's items (null/empty
    // = no more); `first` caps the total yielded; `startSkip` is the initial offset.
    //
    // stopOnPartialPage:true relies on the Orchestrator Web API returning up to the requested
    // `top` (page size 1000) — a short page is then the last one, so the loop ends without an
    // extra empty request. If that assumption ever breaks (an endpoint caps a page below the
    // requested top while still holding more), pass stopOnPartialPage:false for that endpoint:
    // it stops only on an empty page (robust, at the cost of one extra request per enumeration).
    internal static IEnumerable<T> Paginate<T>(Func<ulong, ulong, T[]?> fetchPage, ulong startSkip, ulong first, bool stopOnPartialPage)
    {
        const ulong PageSize = 1000;
        ulong total = 0;
        ulong skip = startSkip;

        while (true)
        {
            ulong top = Math.Min(first - total, PageSize);
            if (top == 0) yield break;

            T[]? page = fetchPage(top, skip);
            if (page is null || page.Length == 0) yield break;

            foreach (var item in page)
            {
                yield return item;
                if (++total == first) yield break;
            }

            if (stopOnPartialPage && (ulong)page.Length < top) yield break;
            skip += (ulong)page.Length;
        }
    }

    // Pure: composes one page's request URL. `query` is the caller-supplied OData/identity tail
    // (already begins with '&', e.g. "&$filter=..."); it is concatenated verbatim. odataStyle
    // selects the OData '$top'/'$skip' spelling vs the identity/portal 'top'/'skip' spelling.
    // Extracted from the GetEnumerable* helpers so the paging-URL composition is unit-testable
    // without a live session (the rest of those helpers is the un-mockable HttpClient send).
    internal static string BuildPagedUrl(string baseUrl, string endPoint, ulong top, ulong skip, string? query, bool odataStyle)
        => odataStyle
            ? $"{baseUrl}{endPoint}?$top={top}&$skip={skip}{query}"
            : $"{baseUrl}{endPoint}?top={top}&skip={skip}{query}";

    private IEnumerable<T> GetEnumerable<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        => Paginate<T>((top, pageSkip) =>
        {
            string url = BuildPagedUrl(_base_url_orchestrator, endPoint, top, pageSkip, query, odataStyle: true);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }
            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);
            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<HttpBodyValues<T>>(strBody)?.value;
        }, skip, first, stopOnPartialPage: true);

    // identity uses non-$ "top"/"skip" (see Paginate for the page-size assumption).
    private IEnumerable<T> GetEnumerableIdentity<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        => Paginate<T>((top, pageSkip) =>
        {
            string url = BuildPagedUrl(_base_url_identity, endPoint, top, pageSkip, query, odataStyle: false);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }
            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);
            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<HttpBodyResults<T>>(strBody)?.results;
        }, skip, first, stopOnPartialPage: true);

    private static StringContent CreateEmptyContent() => new("", Encoding.UTF8, @"application/json");

    // portal uses non-$ "top"/"skip" and requires a (possibly empty) request body.
    private IEnumerable<T> GetEnumerablePortal<T>(string endPoint, Int64? folderId = null, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        => Paginate<T>((top, pageSkip) =>
        {
            string url = BuildPagedUrl(_base_url_portal, endPoint, top, pageSkip, query, odataStyle: false);
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                // The body is required even when empty: without it the Get-OrchPmUser
                // endpoint returns an error (most other endpoints work without it).
                Content = CreateEmptyContent()
            };
            if (folderId.HasValue)
            {
                request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
            }
            using var response = HttpClient_Send(request);
            EnsureSuccessStatusCode(response);
            string strBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<HttpBodyResults<T>>(strBody)?.results;
        }, skip, first, stopOnPartialPage: true);

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
        var body = GetEnumerableWithoutPagingImpl<HttpBodyValues<T>>(_base_url_orchestrator, endPoint, folderId, query);
        return body?.value;
    }

    private T[]? GetEnumerableWithoutPagingIdentity<T>(string endPoint, Int64? folderId = null, string? query = null) => GetEnumerableWithoutPagingImpl<T[]>(_base_url_identity, endPoint, folderId, query);

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

    public string HttpRequest(HttpMethod method, string endPoint, Int64? folderId, string payload) => HttpRequestImpl(method, _base_url_orchestrator, endPoint, folderId, payload);

    public string HttpRequestIdentity(HttpMethod method, string endPoint, Int64? folderId, string payload) => HttpRequestImpl(method, _base_url_identity, endPoint, folderId, payload);

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

    public string HttpRequest(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null) => HttpRequestImpl(method, _base_url_orchestrator, endPoint, folderId, payload);

    public string HttpRequestIdentity(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null) => HttpRequestImpl(method, _base_url_identity, endPoint, folderId, payload);

    // For calling undocumented/private APIs
    public string HttpRequestPortal(HttpMethod method, string endPoint, Int64? folderId = null, object? payload = null) => HttpRequestImpl(method, _base_url_portal, endPoint, folderId, payload);

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
    public HttpResponseMessage SendApiRequest(HttpRequestMessage request, CancellationToken cancellationToken = default) => HttpClient_Send(request, cancellationToken: cancellationToken);

    #endregion

    #region Settings
    public IEnumerable<Settings> GetSettings() => GetEnumerable<Settings>("/odata/Settings");

    public ActivitySettings? GetActivitySettings() => HttpRequest<ActivitySettings>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetActivitySettings");

    // /api/Status/Version returns the deployed product build version
    // (e.g. "26.3.0-s192.2662"), distinct from the API contract version
    // exposed as ApiVersion above. Cloud also returns deployment topology
    // (primary/primary etc.); on-prem leaves deployment / configsVersion null.
    public OrchProductVersion? GetProductVersion(string? partitionGlobalId)
    {
        // partitionGlobalId satisfies the per-org cache getter contract; the
        // endpoint itself is org-global (no tenant/partition in the URL).
        if (string.IsNullOrEmpty(partitionGlobalId)) return null;
        return HttpRequest<OrchProductVersion>(HttpMethod.Get, "/api/Status/Version");
    }

    public UpdateSettings? GetUpdateSettings() => HttpRequest<UpdateSettings>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetUpdateSettings");

    public ExecutionSettingsConfiguration? GetExecutionSettings(int scope) => HttpRequest<ExecutionSettingsConfiguration>(HttpMethod.Get, $"/odata/Settings/UiPath.Server.Configuration.OData.GetExecutionSettingsConfiguration(scope={scope})");

    public void UpdateSettingsBulk(IEnumerable<Settings> settings)
        => HttpRequest(HttpMethod.Post, "/odata/Settings/UiPath.Server.Configuration.OData.UpdateBulk", null, new { settings = settings.Select(s => new { s.Name, s.Value }).ToArray() });

    public ResponseDictionary? GetWebSettings() => HttpRequest<ResponseDictionary>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings");

    public ResponseDictionary? GetAuthenticationSettings() => HttpRequest<ResponseDictionary>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings");

    public ODataValueOfString? GetConnectionString() => HttpRequest<ODataValueOfString>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetConnectionString");

    public License? GetLicenseSettings() => HttpRequest<License>(HttpMethod.Get, "/odata/Settings/UiPath.Server.Configuration.OData.GetLicense");

    #endregion

    #region Alert
    public IEnumerable<Alert> GetAlerts(string? query, ulong skip, ulong first)
    {
        // /odata/Alerts was removed in Orchestrator API v18+. Throw a
        // DeterministicApiException so the cache layer remembers the failure
        // and skips the API call on subsequent invocations.
        if (Supports(OrchApiFloor.AlertsRemoved))
        {
            throw new DeterministicApiException(
                "The Alerts API has been deprecated since Orchestrator API version 18.0.");
        }
        return GetEnumerable<Alert>("/odata/Alerts", null, query, skip, first);
    }
    #endregion

    #region Queues

    public IEnumerable<QueueDefinition> GetQueues(Int64 folderId) => GetEnumerable<QueueDefinition>("/odata/QueueDefinitions", folderId);

    public QueueDefinition? GetQueue(Int64 folderId, Int64 queueId)
    {
        // TODO: Which one should be called when ApiVersion is 17 or 18?
        if (Supports(OrchApiFloor.QueueGetAction))
        {
            return HttpRequest<QueueDefinition>(HttpMethod.Get, $"/odata/QueueDefinitions/UiPath.Server.Configuration.OData.GetQueue(id={queueId})", folderId);
        }
        else
        {
            var queue = HttpRequest<QueueDefinition>(HttpMethod.Get, $"/odata/QueueDefinitions({queueId})", folderId);
            if (queue is null) return null;

            if (Supports(OrchApiFloor.QueueRetentionMerge)) // && ApiVersion < 19)
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

    // Strips the QueueDefinitionDto fields that the given ApiVersion does not have (System.Text.Json
    // is configured WhenWritingNull, so nulling a field excludes it from the POST/PUT body).
    //
    // Mutates `queue` in place: callers pass a freshly-built or DeepCopy'd object (the module-wide
    // convention for the Post*/Put* helpers — see CopyRole/UpdateQueue/NewQueue). Shared by
    // Create / Edit / Put.
    //
    //   < v19: StaleRetention* — confirmed absent from the web interface on v17.
    //   < v18: RetryAbandonedItems — added to QueueDefinitionDto in WebApi v18.0 (swagger
    //          v15-v17 vs v18-v20). MSI 25.10 reports v17.0 and lacks the field; sending it
    //          triggers strict-deserialization failure ("command/queueDef must not be null", 400).
    //   < v14: Tags / Encrypted / IsProcessInCurrentFolder / FoldersCount — absent from the
    //          v11/v13 DTO (swagger v11; live-confirmed: Update-OrchQueue -Tags 400'd with
    //          "queueDefinitionDto must not be null" on 21.10.4 / v13 before this strip). The
    //          legacy < v16 CreateQueue branch nulls these too; doing it here makes Edit / Put
    //          (which only call this helper) survive on v11-v13.
    internal static void StripQueueFieldsForApiVersion(QueueDefinition queue, double? apiVersion)
    {
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.QueueStaleRetention))
        {
            queue.StaleRetentionAction = null;
            queue.StaleRetentionPeriod = null;
            queue.StaleRetentionBucketId = null;
            queue.StaleRetentionBucketName = null;
        }
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.QueueRetryAbandonedItems))
        {
            queue.RetryAbandonedItems = null;
        }
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.QueueV14Fields))
        {
            queue.Tags = null;
            queue.Encrypted = null;
            queue.IsProcessInCurrentFolder = null;
            queue.FoldersCount = null;
        }
    }

    // Canonical version-aware retention defaults for a queue being created. Called from
    // CreateQueue() (just before StripQueueFieldsForApiVersion) so every create path —
    // New-OrchQueue and Copy-OrchQueue (CopyQueues) — gets identical retention behaviour and
    // the "Delete"/30/"Delete"/180 constants live in exactly one place. Mutates `queue` in
    // place (same convention as StripQueueFieldsForApiVersion).
    //
    // Ordering matters: it runs BEFORE the strip, so below v19 the strip still nulls the
    // StaleRetention* fields and the stale defaults set here are correctly dropped.
    //
    // Behaviour notes (live-verified against the OData.CreateQueue action endpoint, the one
    // the module POSTs to at >= v16):
    //   - Automation Cloud (v20, svc1): RetentionPeriod is REQUIRED (a bare body 400s with
    //     "Invalid argument 'Period'"); Final + Stale body retention are honoured; Action
    //     "None" is accepted on THIS version.
    //   - On-prem 25.10.2 (API v17): Period not required, Final honoured, Action "None"
    //     accepted. On-prem tops out at v17, so the >= v19 / Stale path is Cloud-only.
    // The Delete/30 default is therefore load-bearing (Cloud requires a Period).
    //
    // The "None" -> "Delete" coercion below is RETAINED on purpose: an older Orchestrator was
    // observed to fail the POST when RetentionAction was "None", and that platform (older
    // Cloud snapshot / Automation Suite) is no longer reproducible to re-confirm. Current
    // Cloud v20 happens to accept "None", but the two platforms tested here are too small a
    // sample to drop a guard that fixed a real failure. Gated >= v19 (Cloud / Automation
    // Suite); on-prem never reaches it.
    internal static void ApplyQueueRetentionDefaults(QueueDefinition queue, double? apiVersion)
    {
        // Queue retention is a v16+ concept (QueueRetentionMerge floor); below that the
        // legacy CreateQueue path strips retention entirely, so there is nothing to default.
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.QueueRetentionMerge))
        {
            return;
        }

        if (string.IsNullOrEmpty(queue.RetentionAction))
        {
            queue.RetentionAction = "Delete";
        }
        if (queue.RetentionPeriod is null || queue.RetentionPeriod == 0)
        {
            queue.RetentionPeriod = 30;
        }

        // StaleRetention* only exist >= v19 (QueueStaleRetention); below that they are
        // stripped from the body. The server defaults omitted stale to Delete/180, but set
        // it explicitly so a copied queue keeps parity with its source. "None" (Keep) is
        // coerced to "Delete" here — see the note above.
        if (!OrchApiFloor.Below(apiVersion, OrchApiFloor.QueueStaleRetention))
        {
            if (string.IsNullOrEmpty(queue.StaleRetentionAction))
            {
                queue.StaleRetentionAction = "Delete";
            }
            if (queue.StaleRetentionPeriod is null || queue.StaleRetentionPeriod == 0)
            {
                queue.StaleRetentionPeriod = 180;
            }

            if (queue.RetentionAction == "None")
            {
                queue.RetentionAction = "Delete";
            }
            if (queue.StaleRetentionAction == "None")
            {
                queue.StaleRetentionAction = "Delete";
            }
        }
    }

    public QueueDefinition? CreateQueue(Int64 folderId, QueueDefinition queue)
    {
        ApplyQueueRetentionDefaults(queue, ApiVersion);
        StripQueueFieldsForApiVersion(queue, ApiVersion);

        // Verified on OC 22.10.1 (15.0) POST /odata/QueueDefinitions
        // Verified on OC 23.4.0 (16.0) POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue
        // Verified on OC 23.10.0 (17.0) POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue
        if (Supports(OrchApiFloor.QueueCreateAction))
        {
            return HttpRequest<QueueDefinition>(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue", folderId, queue);
        }
        else
        {
            // < v16 legacy POST /odata/QueueDefinitions. Retention is not a create-body field
            // below v16 (separate /odata/QueueRetention resource from v16), so null it. The
            // other fields below are dropped to preserve long-standing legacy-create behaviour;
            // StripQueueFieldsForApiVersion (run above) already nulls Tags / Encrypted /
            // IsProcessInCurrentFolder / FoldersCount < v14 and RetryAbandonedItems < v18 — so
            // EditQueue / PutQueueDefinition, which only call the strip, now survive on v11-v13
            // too (was the open TODO here; verified: Update-OrchQueue -Tags 400'd on v13 before).
            queue.RetentionAction = null;
            queue.RetentionPeriod = null;
            queue.RetentionBucketId = null;
            queue.RetentionBucketName = null;
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
        StripQueueFieldsForApiVersion(queue, ApiVersion);
        // Returns nothing
        HttpRequest(HttpMethod.Post, "/odata/QueueDefinitions/UiPath.Server.Configuration.OData.EditQueue", folderId, queue);
    }

    public void PutQueueDefinition(Int64 folderId, QueueDefinition queue)
    {
        StripQueueFieldsForApiVersion(queue, ApiVersion);
        HttpRequest(HttpMethod.Put, $"/odata/QueueDefinitions({queue.Id!.Value})", folderId, queue);
    }

    public void PutQueueRetention(Int64 folderId, Int64 queueId, QueueRetentionSetting setting) => HttpRequest(HttpMethod.Put, $"/odata/QueueRetention({queueId})", folderId, setting);

    public void RemoveQueue(Int64 folderId, Int64 queueId) => HttpRequest(HttpMethod.Delete, $"/odata/QueueDefinitions({queueId})", folderId);

    public AccessibleFoldersDto? GetFoldersForQueue(Int64 folderId, Int64 queueId) => HttpRequest<AccessibleFoldersDto>(HttpMethod.Get, $"/odata/QueueDefinitions/UiPath.Server.Configuration.OData.GetFoldersForQueue(id={queueId})", folderId);

    public void ShareQueuesToFolders(Int64 folderId, List<Int64> queueIds, List<Int64> toAddFolderIds, List<Int64> toRemoveFolderIds)
    {
        QueueFoldersShare foldersShare = new()
        {
            QueueIds = queueIds,
            ToAddFolderIds = toAddFolderIds,
            ToRemoveFolderIds = toRemoveFolderIds
        };

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
        // Spaces in OData query values must be percent-encoded; some Orchestrator
        // builds reject the raw space.
        string direction = orderAscending ? "asc" : "desc";
        string order = $"&$orderby={orderBy} {direction}";

        // Trailing "&orderby=Id desc" was a non-$-prefixed duplicate that strict
        // Orchestrator builds reject as "Invalid OData query options". The
        // $orderby above is the canonical form.
        return GetEnumerable<QueueItem>("/odata/QueueItems", folderId, $"{filter}{order}&$expand=Robot,ReviewerUser", skip, first);
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

    public BulkOperationResponseOfInt64? DeleteBulkQueueItem(Int64 folderId, QueueItemDeleteBulkRequest payload) => HttpRequest<BulkOperationResponseOfInt64>(HttpMethod.Post, "/odata/QueueItems/UiPathODataSvc.DeleteBulk", folderId, payload);

    public IEnumerable<RobotsFromFolderModel> GetRobotsFromFolder(Int64 folderId) => GetEnumerable<RobotsFromFolderModel>($"/odata/Robots/UiPath.Server.Configuration.OData.GetRobotsFromFolder(folderId={folderId})");

    // Used when creating/updating machines. Note the filter is set. This filter text might be better held on the cache side.
    public IEnumerable<ExtendedRobot> FindAllRobotsAcrossFolders()
        => GetEnumerable<ExtendedRobot>($"/odata/Robots/UiPath.Server.Configuration.OData.FindAllAcrossFolders", null,
            "&$filter=Type eq '2' and ProvisionType eq '1'&$expand=User");

    public IEnumerable<SimpleUser> GetReviewers(Int64 folderId) => GetEnumerable<SimpleUser>("/odata/QueueItems/UiPath.Server.Configuration.OData.GetReviewers()", folderId, "&$filter=(Type eq 'DirectoryUser')");

    public BulkOperationResponseDtoOfFailedQueueItem? BulkAddQueueItem(Int64 folderId, BulkAddQueueItemsRequest payload) => HttpRequest<BulkOperationResponseDtoOfFailedQueueItem>(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.BulkAddQueueItems", folderId, payload);

    public BulkOperationResponseDtoOfFailedQueueItem? BulkAddQueueItem(Int64 folderId, string payload) => HttpRequest<BulkOperationResponseDtoOfFailedQueueItem>(HttpMethod.Post, "/odata/Queues/UiPathODataSvc.BulkAddQueueItems", folderId, payload);

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
    public IEnumerable<CredentialStore> GetCredentialStores() => GetEnumerable<CredentialStore>("/odata/CredentialStores");

    public CredentialStore? GetCredentialStore(Int64 credentialStoreId) => HttpRequest<CredentialStore>(HttpMethod.Get, $"/odata/CredentialStores({credentialStoreId})");

    public CredentialStore? CreateCredentialStore(CredentialStore credentialStore) => HttpRequest<CredentialStore>(HttpMethod.Post, "/odata/CredentialStores", null, credentialStore);

    public void PutCredentialStore(CredentialStore credentialStore) => HttpRequest(HttpMethod.Put, $"/odata/CredentialStores({credentialStore.Id!.Value})", null, credentialStore);

    public void RemoveCredentialStore(Int64 credentialStoreId) => HttpRequest(HttpMethod.Delete, $"/odata/CredentialStores({credentialStoreId})?forceDelete=true");
    #endregion

    #region Webhooks
    public IEnumerable<Webhook> GetWebhooks() => GetEnumerable<Webhook>("/odata/Webhooks");

    // Returns empty
    public void RemoveWebhooks(Int64 webhookId) => HttpRequest(HttpMethod.Delete, $"/odata/Webhooks({webhookId})");

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

    public Webhook? PatchWebhook(Int64 webhookId, Webhook webhook) => HttpRequest<Webhook>(HttpMethod.Patch, $"/odata/Webhooks({webhookId})", null, webhook);

    public IEnumerable<WebhookEventType> GetWebhookEventTypes() => GetEnumerable<WebhookEventType>("/odata/Webhooks/UiPath.Server.Configuration.OData.GetEventTypes");

    public WebhookPingResult? PingWebhook(Int64 webhookId) => HttpRequest<WebhookPingResult>(HttpMethod.Post, $"/odata/Webhooks({webhookId})/UiPath.Server.Configuration.OData.Ping");
    #endregion

    #region BusinessRules

    public IEnumerable<BusinessRule> GetBusinessRules(Int64 folderId) => GetEnumerable<BusinessRule>("/odata/BusinessRules", folderId);

    public BusinessRule? GetBusinessRule(Int64 folderId, string businessRuleKey) => HttpRequest<BusinessRule>(HttpMethod.Get, $"/odata/BusinessRules({businessRuleKey})", folderId);

    public void RemoveBusinessRule(Int64 folderId, string businessRuleKey) => HttpRequest(HttpMethod.Delete, $"/odata/BusinessRules({businessRuleKey})", folderId);

    public BusinessRule? CreateBusinessRule(Int64 folderId, BusinessRule businessRule, string fileName, byte[] file) =>
        // Browser HAR shows POST /odata/BusinessRules with multipart/form-data:
        //   businessRule (text)  : JSON-serialized BusinessRuleDto (PascalCase fields)
        //   file (binary)        : the rule definition file (.dmn), Content-Type application/octet-stream
        // X-UIPATH-OrganizationUnitId carries the folder context.
        PostMultipartBusinessRule(HttpMethod.Post, "/odata/BusinessRules", folderId, businessRule, fileName, file);

    public void UpdateBusinessRule(Int64 folderId, string businessRuleId, BusinessRule businessRule, string? fileName = null, byte[]? file = null) =>
        // PUT /odata/BusinessRules({id}). Same multipart shape as POST, but `file` is optional —
        // omitting it preserves the existing rule definition while updating metadata only.
        PostMultipartBusinessRule(HttpMethod.Put, $"/odata/BusinessRules({businessRuleId})", folderId, businessRule, fileName, file);

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

        var request = new HttpRequestMessage(method, _base_url_orchestrator + endpoint) { Content = content };
        request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());

        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(body)) return null;
        return JsonSerializer.Deserialize<BusinessRule>(body);
    }

    #endregion

    #region Environent
    public IEnumerable<PowerShell.Entities.Environment> GetEnvironments(Int64 folderId) => GetEnumerable<PowerShell.Entities.Environment>("/odata/Environments", folderId, "&$expand=Robots");
    #endregion

    #region Folders

    public IEnumerable<Folder> GetFolders() => GetEnumerable<Folder>("/odata/Folders");

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

    public Folder? GetFolderById(Int64 Id) => HttpRequest<Folder>(HttpMethod.Get, $"/odata/Folders({Id})");

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

    public void RemoveFolder(Int64 folderId) => HttpRequest(HttpMethod.Delete, $"/odata/Folders({folderId})");

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

    public LibraryFeed[]? GetLibraryFeeds() => HttpRequest<LibraryFeed[]>(HttpMethod.Get, "/api/PackageFeeds/GetLibraryFeeds");

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

    public IEnumerable<UserRoles> GetUsersForFolder(Int64 folderId, bool includeInherited = false) => GetEnumerable<UserRoles>($"/odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={folderId},includeInherited={includeInherited.ToString().ToLower()})", null, "&includeAlertsEnabled=true");

    public void AssignDomainUser(DomainUserAssignment user) => HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.AssignDomainUser", null, new { assignment = user });

    public void AssignDirectoryUser(DomainUserAssignment user) => HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.AssignDirectoryUser", null, new { assignment = user });

    // Routes folder-user assignment based on the Orchestrator edition: Cloud
    // takes /AssignDirectoryUser; OnPrem takes /AssignDomainUser. Calling the
    // wrong one surfaces as either "An unknown failure has occurred" or
    // 400 "Invalid OData query options." on the affected environment.
    //
    // The choice depends purely on IsCloud, not on ApiVersion. Verified via
    // web devtools captures on OnPrem 22.10.1 (API 15.0), 23.4.0 (API 16.0)
    // and 25.10.2 (API 17.0) — all three versions use /AssignDomainUser
    // (web payload byte-identical to what this cmdlet sends). If a future
    // OnPrem release ever drops /AssignDomainUser in favor of
    // /AssignDirectoryUser, add a version-aware branch here at that time.
    public void AssignFolderUser(DomainUserAssignment user)
    {
        if (_drive._psDrive.IsCloud)
        {
            AssignDirectoryUser(user);
        }
        else
        {
            AssignDomainUser(user);
        }
    }

    public void AssignUser(Int64 folderId, Int64 userId, IEnumerable<Int64>? roleIds)
    {
        var payload = new
        {
            assignments = new
            {
                UserIds = new Int64[] { userId },
                RolesPerFolder = new[] { new { FolderId = folderId, RoleIds = roleIds } },
            },
        };

        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.AssignUsers", null, payload);
    }

    public IEnumerable<MachineFolder> GetMachinesAssignedTo(Int64 folderId, string? query = null) => GetEnumerable<MachineFolder>($"/odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder(key={folderId})", null, query);

    // Verified on ApiVersion = 15
    public void SetFolderMachineInherit(Int64 folderId, Int64 machineId, bool enabled)
        => HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.ToggleFolderMachineInherit", folderId, new { MachineId = machineId, FolderId = folderId, InheritEnabled = enabled });

    // TODO: Specifying the filter here, but that should be fine... It might be better to implement on the cache side later.
    public IEnumerable<ExtendedRobot> GetFolderRobots(Int64 folderId, MachineFolder machine) => GetEnumerable<ExtendedRobot>($"/odata/Robots/UiPath.Server.Configuration.OData.GetFolderRobots(folderId={folderId},machineId={machine.Id})",
            null,
            "&$filter=Type eq '2' and ProvisionType eq 'Automatic'&$expand=User");

    public IEnumerable<RobotUser> GetMachineRobots(Int64 folderId, MachineFolder machine) => GetEnumerable<RobotUser>($"/odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machine.Id})");

    public void SetMachineRobots(SetMachineRobotsCmd cmd) =>
        // Returns nothing
        HttpRequest(HttpMethod.Post, "/odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots", null, cmd);

    public IEnumerable<UserRobots> GetUserRobots(Int64 folderId) => GetEnumerable<UserRobots>("/odata/Sessions/UiPath.Server.Configuration.OData.GetUserRobots", folderId, "&$filter=startswith(Robot/Username,'autogen\\') eq false and Robot/Username ne ''");

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

    public AvailableVersions? GetAvailableVersions() => HttpRequest<AvailableVersions>(HttpMethod.Get, "/api/UpdateServer/GetAvailableVersions?onlyLatestPatchVersions=true");

    #region PersonalWorkspaces

    public PersonalWorkspace? GetPersonalWorkspace() => HttpRequest<PersonalWorkspace>(HttpMethod.Get, "/odata/PersonalWorkspaces/UiPath.Server.Configuration.OData.GetPersonalWorkspace");

    public IEnumerable<PersonalWorkspace> GetPersonalWorkspaces() => GetEnumerable<PersonalWorkspace>("/odata/PersonalWorkspaces");

    // Currently, this API cannot be called because it does not support OAuth.
    //public void StartExploringPersonalWorkspace(Int64? folderId)
    //{
    //    string body = HttpRequest(HttpMethod.Get, $"/odata/PersonalWorkspaces({folderId})/UiPath.Server.Configuration.OData.StartExploring");
    //}

    public EntitiesSummary? GetEntitiesSummary(Int64 folderId) => HttpRequest<EntitiesSummary>(HttpMethod.Get, $"/odata/Folders/UiPath.Server.Configuration.OData.GetEntitiesSummary(folderId={folderId})?includeShared=true");

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
        // Confirmed that Machine must not be included when ApiVersion == 11
        // Confirmed that Machine can be included when ApiVersion == 13 (21.10.4)
        // No obtainable on-prem build serves API v12, so the boundary itself is
        // unverifiable; the gate stays at 12, bracketed by the two measurements above.
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

    public Job? GetJob(Int64 folderId, Int64 jobId) => HttpRequest<Job>(HttpMethod.Get, $"/odata/Jobs({jobId})?$expand=Robot,Machine,Release", folderId);

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

    public void StopJobs(Int64 folderId, IEnumerable<Int64> jobIds, bool force = false) => HttpRequest(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.StopJobs", folderId, new { strategy = force ? "2" : "1", jobIds });

    public Job? RestartJob(Int64 folderId, Int64 jobId) => HttpRequest<Job>(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.RestartJob", folderId, new { jobId });

    public Job? ResumeJob(Int64 folderId, string jobKey) => HttpRequest<Job>(HttpMethod.Post, "/odata/Jobs/UiPath.Server.Configuration.OData.ResumeJob", folderId, new { jobKey });

    //public string? StartRemoteControl(Int64 folderId, string jobKey)
    //{
    //    var payload = new Dictionary<string, string>();
    //    payload["JobKey"] = jobKey;

    //    var res = HttpRequest<RemoteControlStart>(HttpMethod.Post, "/api/RemoteControl/Start", folderId, payload);
    //    return res!.uri;
    //}

    #endregion

    #region Machines

    public IEnumerable<ExtendedMachine> GetMachines(string? query = null) => GetEnumerable<ExtendedMachine>("/odata/Machines", null, query);

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

    public MachineClientSecretResponse[]? GetMachineClientSecret(string licenseKey) => HttpRequest<MachineClientSecretResponse[]>(HttpMethod.Get, $"/api/clientsecrets/{licenseKey}");

    public MachineClientSecretResponse? AddMachineClientSecret(string licenseKey) => HttpRequest<MachineClientSecretResponse>(HttpMethod.Post, $"/api/clientsecrets/{licenseKey}");

    public void DeleteMachineClientSecret(string secretId) => HttpRequest(HttpMethod.Delete, $"/api/clientsecrets/{secretId}");

    // Returns an empty string
    public void PatchMachine(ExtendedMachine machine) => HttpRequest(HttpMethod.Patch, $"/odata/Machines({machine.Id!.Value})", null, machine);

    public void RemoveMachine(Int64 machineId) => HttpRequest(HttpMethod.Delete, $"/odata/Machines({machineId})");

    // POST is correct for some reason.
    public void RemoveMachines(IEnumerable<Int64> machineIds) => HttpRequest(HttpMethod.Post, $"/odata/Machines/UiPath.Server.Configuration.OData.DeleteBulk", null, new { machineIds });

    #endregion

    #region Packages

    public IEnumerable<Library> GetLibraries(string? feedId = null) =>
        //return GetEnumerable<Library>("/odata/Libraries?$orderby=Id%20desc"); // doesn't work for some reason?
        GetEnumerable<Library>("/odata/Libraries", null, feedId is null ? null : $"&feedId={feedId}");

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
        string endPoint = $"/odata/Libraries/UiPath.Server.Configuration.OData.GetVersions(packageId='{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(libraryId))}')";
        return GetEnumerable<LibraryVersion>(endPoint, null, feedId is null ? null : $"&feedId={feedId}");
    }

    public IEnumerable<Package> GetPackageVersions(string? feedId, string processId)
    {
        string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions(processId='{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(processId))}')";
        string query = null;
        if (feedId is not null)
        {
            query = $"&feedId={feedId}";
        }
        return GetEnumerable<Package>(endPoint, null, query);
    }

    public PackageEntryPoint? GetPackageMainEntryPoint(string? feedId, string packageId, string packageVersion)
    {
        if (Below(OrchApiFloor.PackageEntryPointMetadata)) return null; // Confirmed it returns Not Found on 11.1. TODO: What about 12+? Verify with New-OrchProcess.

        //string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetPackageMainEntryPoint(key='{HttpUtility.UrlEncode(packageId)}:{packageVersion}')";

        string key = $"{PathTools.EscapeODataLiteral(packageId)}:{packageVersion}";
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
        // Mirror the GetPackageMainEntryPoint guard - callers degrade to "no entry
        // point metadata available" instead of a hard failure.
        if (Below(OrchApiFloor.PackageEntryPointMetadata)) return [];

        string endPoint = $"/odata/Processes/UiPath.Server.Configuration.OData.GetPackageEntryPoints(key='{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(packageId))}:{packageVersion}')";
        if (!string.IsNullOrEmpty(feedId))
        {
            return GetEnumerable<PackageEntryPoint>(endPoint, null, $"&feedId={feedId}");
        }
        else
        {
            return GetEnumerable<PackageEntryPoint>(endPoint);
        }
    }

    public void RemoveLibrary(string libraryId, string libraryVersion) => HttpRequest(HttpMethod.Delete, $"/odata/Libraries('{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(libraryId))}:{libraryVersion}')");

    public void RemovePackage(string processId, string processVersion, string? feedId = null)
    {
        if (!string.IsNullOrEmpty(feedId))
        {
            HttpRequest(HttpMethod.Delete, $"/odata/Processes('{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(processId))}:{processVersion}')?feedId={feedId}");
        }
        else
        {
            HttpRequest(HttpMethod.Delete, $"/odata/Processes('{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(processId))}:{processVersion}')");
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
        var request = new HttpRequestMessage(HttpMethod.Post, _base_url_orchestrator + "/odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage")
        {
            Content = content
        };

        // Send the HTTP POST request and get the response
        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var objBody = JsonSerializer.Deserialize<HttpBodyValues<BulkItemDtoOfString>>(body);
        return objBody?.value?.FirstOrDefault();
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

        var request = new HttpRequestMessage(HttpMethod.Post, _base_url_orchestrator + $"/odata/Processes/UiPath.Server.Configuration.OData.UploadPackage()?feedId={feedId}")
        {
            Content = content
        };

        // Send the HTTP POST request and get the response
        using var response = HttpClient_Send(request);
        EnsureSuccessStatusCode(response);

        // Read the response content
        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var objBody = JsonSerializer.Deserialize<HttpBodyValues<BulkItemDtoOfString>>(body);
        return objBody?.value?.FirstOrDefault();
    }

    public BulkItemDtoOfString? UploadPackage(string? feedId, string packageFilePath)
    {
        var fileName = System.IO.Path.GetFileName(packageFilePath);
        var fileContent = File.ReadAllBytes(packageFilePath);
        return UploadPackage(feedId, fileName, fileContent);
    }

    public (string? FileName, byte[] FileContent) DownloadLibrary(string libraryId, string libraryVersion)
    {
        string url = _base_url_orchestrator + $"/odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(libraryId))}:{libraryVersion}')";
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
        // Make the body transfer (the actual download, since the send used ResponseHeadersRead)
        // interruptible by Ctrl+C, not just the header phase.
        using var cancel = new ConsoleCancelHandler();
        var responseBytes = response.Content.ReadAsByteArrayAsync(cancel.Token).GetAwaiter().GetResult();
        return (ret, responseBytes);
    }

    public (string? FileName, byte[] FileContent) DownloadPackage(string feedId, string packageId, string packageVersion)
    {
        string url = _base_url_orchestrator + $"/odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(packageId))}:{packageVersion}')";
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

        // Make the body transfer (the actual download, since the send used ResponseHeadersRead)
        // interruptible by Ctrl+C, not just the header phase.
        using var cancel = new ConsoleCancelHandler();
        var responseBytes = response.Content.ReadAsByteArrayAsync(cancel.Token).GetAwaiter().GetResult();
        return (ret, responseBytes);
    }

    #endregion

    #region Process

    public IEnumerable<Release> ListReleases(Int64 folderId) => GetEnumerable<Release>("/odata/Releases/UiPath.Server.Configuration.OData.ListReleases", folderId, "&$filter=ProcessType eq '1'");

    public IEnumerable<Release> GetReleases(Int64 folderId)
    {
        // The list endpoint accepts an OData $expand string. v12+ adds EntryPoint. Environment is a
        // classic-folder navigation dropped from ReleaseDto in v20 (swagger v20 removes Environment/
        // EnvironmentName/EnvironmentId; 26.3/staging rejects the $expand: "Could not find a property
        // named 'Environment'"). Nothing here reads the nested object and modern folders always
        // returned it null, so drop it at v20+ while keeping it below where classic on-prem may still
        // populate it (unknown version keeps it, staying safe).
        string env = ApiVersion >= 20 ? "" : "Environment,";
        string query = ApiVersion >= 12
            ? $"&$expand={env}CurrentVersion,ReleaseVersions,EntryPoint"
            : $"&$expand={env}CurrentVersion,ReleaseVersions";
        return GetEnumerable<Release>("/odata/Releases", folderId, query);
    }

    public Release? GetReleaseById(Int64 folderId, Int64 releaseId, string? query = null)
    {
        if (Supports(OrchApiFloor.ReleaseGetAction))
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
        var result = HttpRequest<HttpBodyValues<SubtypedPackageResource>>(HttpMethod.Get, $"/odata/Releases({release.Id})/UiPath.Server.Configuration.OData.GetResources(processKey='{HttpUtility.UrlEncode(PathTools.EscapeODataLiteral(release.ProcessKey))}:{release.ProcessVersion}')", folderId);
        return result?.value ?? [];
    }

    // Strip ReleaseDto fields that the target ApiVersion does not have. Sending unknown
    // fields to /odata/Releases (or its CreateRelease action) triggers strict body
    // deserialization and HTTP 400 ("release/command must not be null"). System.Text.Json
    // is configured WhenWritingNull, so nulling a field excludes it from the JSON entirely.
    //
    // Mutates `release` (and its nested ProcessSettings) in place: callers pass a freshly-built
    // or DeepCopy'd object (the module-wide Post*/Put* convention).
    //
    // Empirical findings (OData $metadata + POST probe per OC build) drive these thresholds;
    // ApiVersion alone is not a reliable schema indicator (22.10.1 reports v15 but its
    // ReleaseDto already has RobotSize that the v15.0 swagger snapshot lacks).
    internal static void StripReleaseFieldsForApiVersion(Release release, double? apiVersion)
    {
        // Fields added in v19.0
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.ReleaseV19Fields))
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
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.ReleaseV17Fields))
        {
            release.HiddenForAttendedUser = null;
            release.EntryPointPath = null;
        }
        // Fields added in v16.0 (verified rejected by 22.10.1 / ApiVersion 15).
        // RobotSize: same v15-era split as the ProcessSchedule v16 fields - 22.10.1
        // has it, 22.4.4 does not, both report ApiVersion 15. Bundled here under < 16.
        if (OrchApiFloor.Below(apiVersion, OrchApiFloor.ReleaseV16Fields))
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

        StripReleaseFieldsForApiVersion(release, ApiVersion);

        // Verified on OC 22.10.1 (15.0) POST /odata/Releases
        // Verified on OC 23.4.0 (16.0) POST /odata/Releases
        // Verified on OC 23.10.6 (17.0) POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease
        // Verified on Automation Cloud (19.0) POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease
        if (Supports(OrchApiFloor.ReleaseCloudRetentionDefault))
        {
            // Coerce RetentionAction "None" (Keep) to "Delete". The "None cannot be used on
            // Automation Cloud" observation that motivated this no longer reproduces: current
            // Cloud (v20) ACCEPTS "None" (verified live via PUT /odata/ReleaseRetention). The
            // guard is kept for safety anyway, matching the queue path
            // (ApplyQueueRetentionDefaults): the platform that rejected it (older Cloud snapshot
            // / Automation Suite) is no longer reproducible to confirm.
            // TODO: revisit given an Automation Suite instance.
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
        else if (Supports(OrchApiFloor.ReleaseCreateAction))
        {
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.CreateRelease", folderId, release);
        }
        else
        {
            // Confirmed that non-null SpecificPriorityValue causes an error on 11.1
            // Confirmed that non-null SpecificPriorityValue causes an error on 13.0
            // Confirmed on 15.0 (22.4.4) that this legacy POST /odata/Releases accepts
            // SpecificPriorityValue (47 round-trips intact), matching the
            // ReleaseSpecificPriority = 14 floor. API v14 has no obtainable on-prem build;
            // the floor stays at 14, bracketed by the 13.0 (rejected) / 15.0 (accepted)
            // measurements.
            if (Below(OrchApiFloor.ReleaseSpecificPriority) && release.SpecificPriorityValue is not null)
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
            if (Below(OrchApiFloor.ReleaseEntryPointId)) release.EntryPointId = null;
            return HttpRequest<Release>(HttpMethod.Post, "/odata/Releases", folderId, release);
        }
    }

    public void PatchRelease(Int64 folderId, Release release)
    {
        StripReleaseFieldsForApiVersion(release, ApiVersion);
        HttpRequest(HttpMethod.Patch, $"/odata/Releases({release.Id!.Value})", folderId, release);
    }

    #region ReleaseRetention
    public ReleaseRetentionSetting? GetReleaseRetention(Int64 folderId, Int64 releaseId)
    {
        // Could not read the retention policy with API ver 16.0.
        // Could read the retention policy with API ver 17.0.
        if (Below(OrchApiFloor.ReleaseRetentionReadable)) return null;
        return HttpRequest<ReleaseRetentionSetting>(HttpMethod.Get, $"/odata/ReleaseRetention({releaseId})", folderId);
    }

    public void PutReleaseRetention(Int64 folderId, Int64 releaseId, ReleaseRetentionSetting setting) => HttpRequest(HttpMethod.Put, $"/odata/ReleaseRetention({releaseId})", folderId, setting);

    #endregion ReleaseRetention

    public void RemoveRelease(Int64 folderId, Int64 processId) => HttpRequest(HttpMethod.Delete, $"/odata/Releases({processId})", folderId);

    public void UpdateReleaseToLatestVersionBulk(Int64 folderId, IEnumerable<Int64> processIds)
        => HttpRequest(HttpMethod.Post, "/odata/Releases/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersionBulk", folderId, new { releaseIds = processIds, mergePackageTags = false });

    public void UpdateReleaseToLatestVersion(Int64 folderId, Int64 processId) => HttpRequest(HttpMethod.Post, $"/odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersion?mergePackageTags=false", folderId, (object?)null);

    public void UpdateReleaseToSpecificVersion(Int64 folderId, Int64 processId, string version) => HttpRequest(HttpMethod.Post, $"/odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToSpecificPackageVersion", folderId, new { packageVersion = version });

    public void RollbackReleaseVersion(Int64 folderId, Int64 processIds) => HttpRequest(HttpMethod.Post, $"/odata/Releases({processIds})/UiPath.Server.Configuration.OData.RollbackToPreviousReleaseVersion?mergePackageTags=false", folderId, (object?)null);

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

    public IEnumerable<ProcessSchedule> GetProcessSchedules(Int64 folderId) => GetEnumerable<ProcessSchedule>("/odata/ProcessSchedules", folderId);

    public ProcessSchedule? GetProcessSchedule(Int64 folderId, Int64 processScheduleId) => HttpRequest<ProcessSchedule>(HttpMethod.Get, $"/odata/ProcessSchedules({processScheduleId})", folderId);

    // Get the ExecutorRobots of the trigger
    public Int64[] GetRobotIdsForSchedule(Int64 folderId, Int64 processScheduleId) => HttpRequest<HttpBodyValue<Int64[]>>(HttpMethod.Get, $"/odata/ProcessSchedules/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule(key={processScheduleId})", folderId)?.value ?? [];

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

    public void DeleteProcessSchedule(Int64 folderId, Int64 processScheduleId) => HttpRequest(HttpMethod.Delete, $"/odata/ProcessSchedules({processScheduleId})", folderId);

    public bool? EnableProcessSchedule(Int64 folderId, IEnumerable<Int64> processScheduleIds, bool enabled = true)
    {
        var ret = HttpRequest<HttpBodyValue<bool>>(HttpMethod.Post, "/odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled", folderId, new { scheduleIds = processScheduleIds, enabled });
        return ret?.value;
    }

    // Server-side validation of a ProcessSchedule's executability (robot/template/license checks)
    // without committing changes. Returns IsValid + Errors + ErrorCodes.
    public ValidationResult? ValidateProcessSchedule(Int64 folderId, ProcessSchedule schedule) =>
        HttpRequest<ValidationResult>(HttpMethod.Post, "/odata/ProcessSchedules/UiPath.Server.Configuration.OData.ValidateProcessSchedule", folderId, new { processSchedule = schedule });

    #endregion

    #region Buckets

    public IEnumerable<Bucket> GetBuckets(Int64 folderId) => GetEnumerable<Bucket>("/odata/Buckets", folderId);

    public Bucket? PostBucket(Int64 folderId, Bucket bucket)
    {
        if (ApiVersion < 15)
        {
            bucket.Tags = null;
        }
        return HttpRequest<Bucket>(HttpMethod.Post, "/odata/Buckets", folderId, bucket);
    }

    public void PutBucket(Int64 folderId, Bucket bucket)
    {
        if (ApiVersion < 15)
        {
            bucket.Tags = null;
        }
        HttpRequest(HttpMethod.Put, $"/odata/Buckets({bucket.Id!.Value})", folderId, bucket);
    }

    public void DeleteBucket(Int64 folderId, Int64 bucketId) => HttpRequest(HttpMethod.Delete, $"/odata/Buckets({bucketId})", folderId);

    // Returns nothing
    public void DeleteBucketItem(Int64 folderId, Int64 bucketId, string fullPath) => HttpRequest(HttpMethod.Delete, $"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.DeleteFile?path={Uri.EscapeDataString(fullPath)}", folderId);

    public IEnumerable<BlobFile> GetBucketDirectories(Int64 folderId, Int64 bucketId) => GetEnumerable<BlobFile>($"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetDirectories", folderId, "&directory=%2F&recursive=true");

    public IEnumerable<BlobFile> GetBucketFiles(Int64 folderId, Bucket bucket) => GetEnumerable<BlobFile>($"/odata/Buckets({bucket.Id})/UiPath.Server.Configuration.OData.GetFiles", folderId, "&directory=%2F&recursive=true");

    public BlobFileAccess? GetBucketReadUri(Int64 folderId, Int64 bucketId, string fullPath) => HttpRequest<BlobFileAccess>(HttpMethod.Get, $"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetReadUri?path={Uri.EscapeDataString(fullPath)}", folderId);

    public BlobFileAccess? GetBucketWriteUri(Int64 folderId, Int64 bucketId, string fullPath) => HttpRequest<BlobFileAccess>(HttpMethod.Get, $"/odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetWriteUri?path={Uri.EscapeDataString(fullPath)}", folderId);

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

    // Buckets are read/written on multiple threads (see callers below), so this
    // lazily-created dedicated client must be published atomically. A bare `??=`
    // is not thread-safe: two threads could each construct a client and one would
    // leak (only the last-assigned is disposed). volatile + double-checked lock
    // guarantees the factory runs once and readers see a fully-constructed client.
    private volatile HttpClient? _httpClientForBucketItem = null;
    private readonly object _httpClientForBucketItemLock = new();

    private void EnsureBucketItemHttpClient()
    {
        if (_httpClientForBucketItem is null)
        {
            lock (_httpClientForBucketItemLock)
            {
                _httpClientForBucketItem ??= InitializeHttpClient(_drive);
            }
        }
    }

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
        EnsureBucketItemHttpClient();

        // When access.RequiresAuth is true, should the request be sent with the Authorization header retained?
        using var res = HttpClient_Send(req, _httpClientForBucketItem, cancellationToken);

        EnsureBlobSuccess(res);

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
    public void ReadBucketItem(BlobFileAccess? access, string destinationPath, CancellationToken cancellationToken = default) => ReadBucketItemAsync(access, destinationPath, cancellationToken).GetAwaiter().GetResult();

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
        EnsureBucketItemHttpClient();

        // When access.RequiresAuth is true, should the request be sent with the Authorization header retained?
        using var res = HttpClient_Send(req, _httpClientForBucketItem, cancelToken);

        EnsureBlobSuccess(res);
    }

    // Holds an open streaming bucket-item read: keeps the HTTP response alive so its
    // body Stream stays readable until the caller has finished piping it into a write.
    // Dispose releases the stream and the underlying response/connection.
    public sealed class BucketItemReadStream : IDisposable
    {
        private readonly HttpResponseMessage _response;
        public Stream Stream { get; }
        public long? Length { get; }
        public string? ContentType { get; }

        internal BucketItemReadStream(HttpResponseMessage response, Stream stream, long? length, string? contentType)
        {
            _response = response;
            Stream = stream;
            Length = length;
            ContentType = contentType;
        }

        public void Dispose()
        {
            Stream?.Dispose();
            _response?.Dispose();
        }
    }

    // Open a bucket item for streaming read. The response (and its body Stream) stay open
    // inside the returned holder so the caller can pipe the bytes STRAIGHT into a destination
    // write without staging to local disk. The GET runs through THIS session's bucket-item
    // client, so the SOURCE drive's proxy / SSL config applies — which is why a cross-drive
    // copy must open the read on the source session and the write on the destination session
    // (their proxy/SSL settings can differ). Returns null on empty access (mirrors
    // ReadBucketItem's no-op). Used by Copy-OrchBucketItem.
    public BucketItemReadStream? OpenBucketItemRead(BlobFileAccess? access, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();

        if (access == null || string.IsNullOrWhiteSpace(access.Verb) || string.IsNullOrWhiteSpace(access.Uri))
            return null;

        HttpMethod method = access.Verb.Equals("POST", StringComparison.OrdinalIgnoreCase)
            ? HttpMethod.Post
            : HttpMethod.Get;

        if (!Uri.TryCreate(access.Uri, UriKind.Absolute, out var _))
            throw new ArgumentException($"Invalid Uri: {access.Uri}", nameof(access));

        using var req = new HttpRequestMessage(method, access.Uri);
        AddHeaders(req, access);

        // Dedicated bucket-item client (no Authorization default header) — same rationale
        // as ReadBucketItem: the presigned blob URI must not carry the session bearer.
        EnsureBucketItemHttpClient();

        // HttpClient_Send returns on ResponseHeadersRead, so the body is still streaming here.
        var res = HttpClient_Send(req, _httpClientForBucketItem, cancelToken);
        try
        {
            EnsureBlobSuccess(res);
            var stream = res.Content.ReadAsStream();
            return new BucketItemReadStream(res, stream, res.Content.Headers.ContentLength, res.Content.Headers.ContentType?.MediaType);
        }
        catch
        {
            res.Dispose();
            throw;
        }
    }

    // Upload a bucket item from an already-open stream (e.g. the body of a source read), so a
    // cross-drive copy never lands the bytes on local disk. The PUT runs through THIS session's
    // bucket-item client, so the DESTINATION drive's proxy / SSL config applies. A presigned PUT
    // (S3 / Azure Blob) generally needs Content-Length and rejects chunked transfer, so we send
    // the known length; when the source length is unknown we buffer once to a temp file to obtain
    // it rather than risk a chunked PUT the backend refuses. Used by Copy-OrchBucketItem.
    public void WriteBucketItemFromStream(BlobFileAccess access, Stream body, long? length, string? contentType, string fullPath, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        // For now, only PUT is supported (mirrors WriteBucketItem).
        if (access.Verb!.ToUpperInvariant() != "PUT") throw new NotImplementedException();

        if (!Uri.TryCreate(access.Uri, UriKind.Absolute, out var _))
            throw new ArgumentException($"Invalid Uri: {access.Uri}", nameof(access));

        if (length.HasValue)
        {
            PutBucketStream(access, body, length.Value, contentType, fullPath, cancelToken);
            return;
        }

        // Unknown source length: buffer once to a temp file to obtain a Content-Length.
        string temp = Path.GetTempFileName();
        try
        {
            using (var tmp = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1024 * 128, options: FileOptions.SequentialScan))
            {
                body.CopyTo(tmp);
            }
            using var readBack = File.OpenRead(temp);
            PutBucketStream(access, readBack, readBack.Length, contentType, fullPath, cancelToken);
        }
        finally
        {
            TryDeleteFileQuiet(temp);
        }
    }

    private void PutBucketStream(BlobFileAccess access, Stream body, long length, string? contentType, string fullPath, CancellationToken cancelToken)
    {
        using var content = new StreamContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? MimeTypeHelper.GetMimeType(fullPath) : contentType);
        content.Headers.ContentLength = length;

        using var req = new HttpRequestMessage(HttpMethod.Put, access.Uri) { Content = content };
        AddHeaders(req, access);

        EnsureBucketItemHttpClient();

        using var res = HttpClient_Send(req, _httpClientForBucketItem, cancelToken);
        EnsureBlobSuccess(res);
    }


    // TODO
    //public void GetBucketsAcrossFolders(Int64 folderId)
    //{
    //    string body = HttpRequest(HttpMethod.Get, "/odata/Buckets/UiPath.Server.Configuration.OData.GetBucketsAcrossFolders", folderId);
    //}

    public AccessibleFoldersDto? GetFoldersForBucket(Int64 folderId, Int64 bucketId) => HttpRequest<AccessibleFoldersDto>(HttpMethod.Get, $"/odata/Buckets/UiPath.Server.Configuration.OData.GetFoldersForBucket(id={bucketId})", folderId);

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
        // NB: the triggers endpoint rejects $expand=Release ("Invalid OData query options"), so the
        // Release nav prop comes back null -- Get-OrchApiTrigger fills Release.Name client-side from
        // ReleaseKey instead (see ResolveReleaseName).
        return GetEnumerable<HttpTrigger>("/odata/HttpTriggers", folderId);
    }

    public HttpTrigger? GetHttpTrigger(Int64 folderId, string triggerId) => HttpRequest<HttpTrigger>(HttpMethod.Get, $"/odata/HttpTriggers({triggerId})", folderId);

    public HttpTrigger? CreateHttpTrigger(Int64 folderId, HttpTrigger trigger)
    {
        StripHttpTriggerFieldsForApiVersion(trigger);
        return HttpRequest<HttpTrigger>(HttpMethod.Post, "/odata/HttpTriggers", folderId, trigger);
    }

    // HttpTrigger create/update is strict: it replies "httpTrigger must not be null"
    // (HTTP 400 — the body fails to deserialize, so the bound parameter is null) when the
    // payload carries a field the server's version does not recognise. This is the same
    // failure mode as ProcessSchedule's "model must not be null"; see
    // StripProcessScheduleFieldsForApiVersion. RunAsCaller is a recent Cloud-only field —
    // absent from the on-prem swagger through v20 — that ApiVersion 17 (on-prem 25.10.2)
    // rejects while ApiVersion 20 (Cloud) accepts (verified live). Strip it below 20.
    private void StripHttpTriggerFieldsForApiVersion(HttpTrigger trigger)
    {
        if (ApiVersion < 20)
        {
            trigger.RunAsCaller = null;
        }
    }

    // Confirmed against browser dev-tools capture (yotsuda tenant 2026-05-21):
    //   PUT /odata/HttpTriggers({id})   id is the string GUID HttpTrigger.Id
    // The server returns no entity body on PUT, so callers should re-fetch
    // via GetHttpTrigger if they need the updated state.
    public void UpdateHttpTrigger(Int64 folderId, HttpTrigger trigger)
    {
        if (string.IsNullOrEmpty(trigger.Id))
        {
            throw new ArgumentException("HttpTrigger.Id (GUID string) must be set for PUT.", nameof(trigger));
        }
        StripHttpTriggerFieldsForApiVersion(trigger);
        HttpRequest(HttpMethod.Put, $"/odata/HttpTriggers({trigger.Id})", folderId, trigger);
    }

    // nothing returns
    public void RemoveHttpTrigger(Int64 folderId, string triggerId) => HttpRequest(HttpMethod.Delete, $"/odata/HttpTriggers({triggerId})", folderId);

    public bool? EnableHttpTriggers(Int64 folderId, string[] triggerIds, bool enabled = true)
    {
        var ret = HttpRequest<HttpBodyValue<bool>>(HttpMethod.Post, "/odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled", folderId, new { enabled, triggerIds });
        return ret?.value;
    }

    #endregion

    #region EventTrigger
    public IEnumerable<ApiTrigger> GetEventTriggers(Int64 folderId)
    {
        EnsureVersionSupport(14); // The exact number has not been confirmed.
        // NB: $expand=Release is rejected here too; Get-OrchEventTrigger fills Release.Name client-side.
        return GetEnumerable<ApiTrigger>("/odata/ApiTriggers", folderId);
    }

    // nothing returns
    public void RemoveEventTrigger(Int64 folderId, string triggerId) => HttpRequest(HttpMethod.Delete, $"/odata/ApiTriggers({triggerId})", folderId);

    public bool? EnableEventTriggers(Int64 folderId, string triggerId, bool enabled = true)
    {
        var ret = HttpRequest<HttpBodyValue<bool>>(HttpMethod.Patch, $"/odata/ApiTriggers({triggerId})", folderId, new { Enabled = enabled });
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

    public IEnumerable<AuditLog> GetAuditLogs(string? query, ulong skip, ulong first) => GetEnumerable<AuditLog>("/odata/AuditLogs", null, query, skip, first);

    public IEnumerable<AuditLogEntity> GetAuditLogDetails(Int64 auditLogId) => GetEnumerable<AuditLogEntity>($"/odata/AuditLogs/UiPath.Server.Configuration.OData.GetAuditLogDetails(auditLogId={auditLogId})");

    #endregion

    #region Robot
    public IEnumerable<Robot> GetRobots() => GetEnumerable<Robot>("/odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots", null, "&$expand=User");
    #endregion

    #region Role

    public IEnumerable<Role> GetRoles() => GetEnumerable<Role>("/odata/Roles", null, "&$expand=Permissions");

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

    public void DeleteRole(Int64 roleId) => HttpRequest(HttpMethod.Delete, $"/odata/Roles({roleId})");

    #endregion

    #region User

    public IEnumerable<User> GetUsers() => GetEnumerable<User>("/odata/Users", null, "&$expand=OrganizationUnits,UserRoles");//return GetEnumerable<User>("/odata/Users", null, "&$expand=OrganizationUnits,UserRoles,UnattendedRobot"); // This causes an error

    public User? GetUser(Int64 userId) => HttpRequest<User>(HttpMethod.Get, $"/odata/Users({userId})?$expand=OrganizationUnits,UserRoles");

    public UserPrivilege? GetUserPrivilege(Int64 userId) => HttpRequest<UserPrivilege>(HttpMethod.Get, $"/api/Users/GetPrivileges?userId={userId}");

    public User? PostUser(User user)
    {
        if (user.NotificationSubscription is not null)
        {
            // Drop notification flags the target server's schema predates, or it
            // rejects POST /odata/Users with "The input was not valid." (older
            // Orchestrator strict-binds and refuses unknown fields). Introduction
            // versions per the UserNotificationSubscription swagger snapshots
            // v15-v20.
            if (ApiVersion < 16)
            {
                // Export (and Serverless) added in ApiVersion 16; 22.10 == v15.
                user.NotificationSubscription.Export = null;
            }
            if (ApiVersion < 18)
            {
                // RateLimitsDaily / RateLimitsRealTime added in ApiVersion 18.
                user.NotificationSubscription.RateLimitsDaily = null;
                user.NotificationSubscription.RateLimitsRealTime = null;
            }
        }
        if (ApiVersion >= 18)
        {
            user.BypassBasicAuthRestriction = null; // Deprecated in ApiVersion 18.
        }

        //if (user.UnattendedRobot is not null)
        //{
        //    user.UnattendedRobot.ExecutionSettings ??= new();
        //}

        // UpdatePolicy is on UserDto from ApiVersion 15 (absent on v11). Older
        // Orchestrator (e.g. 22.10 == v15) rejects POST /odata/Users with a bare
        // "The input was not valid." when it's absent, and the web UI always sends
        // it — but pre-15 has no such field and would reject it as unknown. Default
        // it (matching the web UI) only where the field exists. (??= leaves an
        // explicit -UpdatePolicyType / -UpdatePolicyVersion untouched.)
        if (ApiVersion >= 15)
        {
            user.UpdatePolicy ??= new() { Type = "None" };
        }

        return HttpRequest<User>(HttpMethod.Post, "/odata/Users", null, user);
    }

    public void PutUser(User user) => HttpRequest(HttpMethod.Put, $"/odata/Users({user.Id ?? 0})", null, user);

    public void PatchUser(User user) => HttpRequest(HttpMethod.Patch, $"/odata/Users({user.Id ?? 0})", null, user);

    public void DeleteUser(Int64 userId) => HttpRequest(HttpMethod.Delete, $"/odata/Users({userId})");

    public User? GetCurrentUser() =>
        // No client-side IsConfidentialApp guard: let the server be the source
        // of truth. On a Confidential app the server returns an authentication
        // error (typically 401), which the caller's exception cache catches
        // via the standard HttpResponseException whitelist.
        HttpRequest<User>(HttpMethod.Get, "/odata/Users/UiPath.Server.Configuration.OData.GetCurrentUser");

    public ExtendedUser? GetCurrentUserExtended() => HttpRequest<ExtendedUser>(HttpMethod.Get, "/odata/Users/UiPath.Server.Configuration.OData.GetCurrentUserExtended?$expand=PersonalWorkspace");

    public void UpdateCurrentUserURPassword(Int64 userId, string password)
        => HttpRequest(HttpMethod.Patch, $"/odata/Users({userId})", null, new { UnattendedRobot = new { Password = password } });

    public void UnassignUserFromFolder(Int64 folderId, Int64 userId)
        => HttpRequest(HttpMethod.Post, $"/odata/Folders({folderId})/UiPath.Server.Configuration.OData.RemoveUserFromFolder", null, new { userId });

    #endregion

    #region DirectoryService

    // The quota for this API is 300 calls per 5 minutes.
    // https://uipath-japan.slack.com/archives/C0175DZP4PQ/p1751336407409919?thread_ts=1751275792.210139&cid=C0175DZP4PQ
    private DateTime _lastSearchDirectory = DateTime.MinValue;
    private readonly object _lockSearchDirectory = new object();
    public DirectoryObject[] SearchDirectory(string prefix, string? domain = null)
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
        // `domain` defaults to "autogen", which works for Automation Cloud and
        // for non-federated OnPrem (the server's default partition). EntraID-
        // federated OnPrem tenants reject "autogen" with a generic 500 ("An
        // unknown failure has occurred") and require the actual tenant domain
        // (e.g. "frc"). The caller passes the user-supplied -Domain in those
        // cases; we don't currently auto-discover the domain list.
        var effectiveDomain = string.IsNullOrEmpty(domain) ? "autogen" : domain;
        return HttpRequest<DirectoryObject[]>(HttpMethod.Get, $"/api/DirectoryService/SearchForUsersAndGroups?domain={HttpUtility.UrlEncode(effectiveDomain)}&prefix={HttpUtility.UrlEncode(prefix)}&searchContext=All") ?? [];
    }

    // Lists the directory partition domains the tenant can search/assign against
    // (EntraID-federated OnPrem returns e.g. "frc"/"root" with one isDefault; non-
    // federated tenants and Automation Cloud typically return an empty list). Feeds
    // the -Domain completer. Bare JSON array, not OData, so deserialize directly.
    public IEnumerable<DirectoryDomain> GetDomains()
        => HttpRequest<DirectoryDomain[]>(HttpMethod.Get, "/api/DirectoryService/GetDomains") ?? [];

    #endregion

    #region License
    public IEnumerable<LicenseNamedUser> GetLicensesNamedUser(string robotType) => GetEnumerable<LicenseNamedUser>($"/odata/LicensesNamedUser/UiPath.Server.Configuration.OData.GetLicensesNamedUser(robotType='{PathTools.EscapeODataLiteral(robotType)}')");

    public IEnumerable<LicenseRuntime> GetLicensesRuntime(string robotType) =>
        //return GetEnumerable<LicenseRuntime>($"/odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime(robotType='{robotType}')", null, "&$expand='LastLoginDate'");
        GetEnumerable<LicenseRuntime>($"/odata/LicensesRuntime/UiPath.Server.Configuration.OData.GetLicensesRuntime(robotType='{PathTools.EscapeODataLiteral(robotType)}')");

    public void ToggleLicenseRuntime(string robotType, string key, string machineName, bool enabled) =>
        HttpRequest(HttpMethod.Post, $"/odata/LicensesRuntime('{Uri.EscapeDataString(PathTools.EscapeODataLiteral(machineName))}')/UiPath.Server.Configuration.OData.ToggleEnabled", null, new { key, robotType, enabled });
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

    public IEnumerable<LicenseStatsModel> GetLicenseStats(int tenantId, int days) => HttpRequest<LicenseStatsModel[]>(HttpMethod.Get, $"/api/Stats/GetLicenseStats?tenantId={tenantId}&days={days}") ?? [];

    public IEnumerable<CountStats> GetJobStats() => HttpRequest<CountStats[]>(HttpMethod.Get, "/api/Stats/GetJobsStats") ?? [];

    // Returns Not Found...
    public IEnumerable<CountStats> GetSessionStats() => HttpRequest<CountStats[]>(HttpMethod.Get, "/api/Stats/GetSessionsStats") ?? [];

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

    public Asset? GetAsset(Int64 folderId, Int64 assetId) => HttpRequest<Asset>(HttpMethod.Get, $"/odata/Assets({assetId})?$expand=UserValues", folderId);

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

    public void PutAsset(Int64 folderId, Asset asset) => HttpRequest(HttpMethod.Put, $"/odata/Assets({asset.Id})", folderId, asset);

    public void RemoveAsset(Int64 folderId, Int64 assetId) => HttpRequest(HttpMethod.Delete, $"/odata/Assets({assetId})", folderId);

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
    public IEnumerable<ExecutionMedia> GetExecutionMedia(Int64 folderId, ulong skip = 0, ulong first = ulong.MaxValue) => GetEnumerable<ExecutionMedia>("/odata/ExecutionMedia", folderId, null, skip, first);

    public void RemoveExecutionMedia(Int64 folderId, Int64 jobId)
        => HttpRequest(HttpMethod.Post, "/odata/ExecutionMedia/UiPath.Server.Configuration.OData.DeleteMediaByJobId", folderId, new { jobId });

    // Routes through the shared send chokepoint (SendApiRequest -> HttpClient_Send) so this
    // download gets the same auth-refresh-on-401, transient (429/503/504) retry, and Ctrl+C
    // cancellation as every other call. (The endpoint may not strictly require the Authorization
    // header, but sending it is harmless and keeps this on the single retry/cancel path.)
    public async Task<(string? FileName, byte[] FileContent)> DownloadMediaByJobId(Int64 folderId, Int64 jobId)
    {
        string endPoint = _base_url_orchestrator + $"/odata/ExecutionMedia/UiPath.Server.Configuration.OData.DownloadMediaByJobId(jobId={jobId})";
        var request = new HttpRequestMessage(HttpMethod.Get, endPoint);
        request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());

        using var response = SendApiRequest(request);
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

        // Make the body transfer (the actual download) interruptible by Ctrl+C, not just the header phase.
        using var cancel = new ConsoleCancelHandler();
        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancel.Token);
        return (ret, responseBytes);
    }

    #endregion

    #region Session

    // Get classic folder robots
    #region Tasks

    public IEnumerable<OrchTask> GetTasks(Int64 folderId) => GetEnumerable<OrchTask>("/odata/Tasks", folderId);

    public IEnumerable<OrchTask> GetTasksAcrossFolders() => GetEnumerable<OrchTask>("/odata/Tasks/UiPath.Server.Configuration.OData.GetTasksAcrossFolders");

    public OrchTask? GetTask(Int64 folderId, Int64 taskId) => HttpRequest<OrchTask>(HttpMethod.Get, $"/odata/Tasks({taskId})", folderId);

    public void RemoveTask(Int64 folderId, Int64 taskId) => HttpRequest(HttpMethod.Delete, $"/odata/Tasks({taskId})", folderId);

    public void EditTaskMetadata(Int64 folderId, EditTaskMetadataRequest request) => HttpRequest(HttpMethod.Post, "/odata/Tasks/UiPath.Server.Configuration.OData.EditTaskMetadata", folderId, request);

    #endregion

    public IEnumerable<Session> GetSessions(Int64 folderId) => GetEnumerable<Session>("/odata/Sessions", folderId, "&$expand=Robot($expand=License)");

    // Bulk delete inactive (disconnected / unresponsive) unattended sessions by id.
    // Returns 204 No Content. Tenant-level operation (no X-UIPATH-OrganizationUnitId
    // per the v20 swagger: parameters block only carries the body).
    public void DeleteInactiveSessions(IEnumerable<Int64> sessionIds)
        => HttpRequest(HttpMethod.Post, "/odata/Sessions/UiPath.Server.Configuration.OData.DeleteInactiveUnattendedSessions", null, new { sessionIds });

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

    public IEnumerable<Session> GetGlobalSessions(string? query = null, ulong skip = 0, ulong first = ulong.MaxValue) => GetEnumerable<Session>("/odata/Sessions/UiPath.Server.Configuration.OData.GetGlobalSessions", null, query, skip, first);

    //public IEnumerable<MachineSessionRuntime> GetMachineSessionRuntimes(string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    public IEnumerable<MachineSessionRuntime> GetMachineSessionRuntimes() => GetEnumerable<MachineSessionRuntime>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes");

    public IEnumerable<MachineSessionRuntime> GetMachineSessionRuntimesByFolderId(Int64 folderId, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue) => GetEnumerable<MachineSessionRuntime>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId(folderId={folderId})", folderId, query, skip, first);

    // Test
    //public void GetMachineSessions(Int64 machineId)
    //{
    //    HttpRequest(HttpMethod.Get, $"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessions({machineId})");
    //    //return GetEnumerable<Session>($"/odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessions({machineId})");
    //}

    #endregion

    #region TestCase

    public IEnumerable<TestCaseDefinition> GetTestCases(Int64 folderId) => GetEnumerable<TestCaseDefinition>("/odata/TestCaseDefinitions", folderId);

    public IEnumerable<TestCaseExecution> GetTestCaseExecutions(Int64 folderId, string? filter, ulong skip, ulong first) => GetEnumerable<TestCaseExecution>("/odata/TestCaseExecutions", folderId, filter, skip, first);

    public TestCaseExecution? GetTestCaseExecutionWithAssertions(Int64 folderId, Int64 testCaseExecutionId) => HttpRequest<TestCaseExecution>(HttpMethod.Get, $"/odata/TestCaseExecutions({testCaseExecutionId})?$expand=TestCaseAssertions", folderId);

    public void DownloadAssertionScreenshot(Int64 folderId, Int64 assertionId, string destinationPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string url = _base_url_orchestrator + $"/api/TestAutomation/GetAssertionScreenshot?testCaseAssertionId={assertionId}&organizationUnitId={folderId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Route through the shared chokepoint (not the raw _httpClient) so the download gets
        // auth-refresh-on-401, transient retry, and Ctrl+C cancellation like every other call.
        using var response = SendApiRequest(request, cancellationToken);
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

    public void RemoveTestCases(Int64 folderId, IEnumerable<Int64> testCaseIds)
        => HttpRequest(HttpMethod.Post, "/odata/TestCaseDefinitions/UiPath.Server.Configuration.OData.BulkDelete", folderId, new { testCaseDefinitionIds = testCaseIds.ToList() });

    public IEnumerable<TestSet> GetTestSets(Int64 folderId) => GetEnumerable<TestSet>("/odata/TestSets", folderId, "&$filter=(SourceType eq 'User')&$expand=Environment");

    public TestSet? GetTestSetForEdit(Int64 folderId, Int64 testSetId) => HttpRequest<TestSet>(HttpMethod.Get, $"/odata/TestSets({testSetId})/UiPath.Server.Configuration.OData.GetForEdit()", folderId);

    public TestSet? CreateTestSet(Int64 folderId, TestSet testSet)
    {
        string body = HttpRequest(HttpMethod.Post, "/odata/TestSets", folderId, testSet);
        return JsonSerializer.Deserialize<TestSet>(body);
    }

    public void RemoveTestSet(Int64 folderId, Int64 testSetId) => HttpRequest(HttpMethod.Delete, $"/odata/TestSets({testSetId})", folderId);

    public Int64? StartTestSets(Int64 folderId, Int64 testSetId)
    {
        string body = HttpRequest(HttpMethod.Post, $"/api/TestAutomation/StartTestSetExecution?testSetId={testSetId}&triggerType=0", folderId);
        if (Int64.TryParse(body, out Int64 ret))
        {
            return ret;
        }
        return null;
    }

    public IEnumerable<TestSetExecution> GetTestSetExecutions(Int64 folderId, string? filter, ulong skip, ulong first) => GetEnumerable<TestSetExecution>("/odata/TestSetExecutions", folderId, "&$expand=TestSet" + filter, skip, first);

    // Appears to return "null"
    public void CancelTestSetExecutions(Int64 folderId, Int64 testSetExecutionId) => HttpRequest(HttpMethod.Post, $"/api/TestAutomation/CancelTestSetExecution?testSetExecutionId={testSetExecutionId}", folderId);

    public string CancelTestCaseExecutions(Int64 folderId, Int64 testSetExecutionId) => HttpRequest(HttpMethod.Post, $"/api/TestAutomation/CancelTestSetExecution?testCaseExecutionId={testSetExecutionId}", folderId);

    public IEnumerable<TestSetSchedule> GetTestSetSchedules(Int64 folderId) => GetEnumerable<TestSetSchedule>("/odata/TestSetSchedules", folderId);

    public TestSetSchedule? CreateTestSetSchedule(Int64 folderId, TestSetSchedule testSetSchedule)
    {
        string body = HttpRequest(HttpMethod.Post, "/odata/TestSetSchedules", folderId, testSetSchedule);
        return JsonSerializer.Deserialize<TestSetSchedule>(body)!;
    }

    // PUT URL follows standard OData convention; tenant capability for
    // TestSetSchedule modification is gated by the same feature flag as
    // creation, so test-tenant verification is blocked until a tenant
    // with the flag enabled is available (yotsuda has it off as of
    // 2026-05-22). The endpoint URL itself matches what /odata/$metadata
    // advertises; no PATCH variant is exposed by the OrchAPI surface.
    public void UpdateTestSetSchedule(Int64 folderId, TestSetSchedule testSetSchedule)
    {
        if (testSetSchedule.Id is null)
        {
            throw new ArgumentException("TestSetSchedule.Id must be set for PUT.", nameof(testSetSchedule));
        }
        HttpRequest(HttpMethod.Put, $"/odata/TestSetSchedules({testSetSchedule.Id})", folderId, testSetSchedule);
    }

    public void RemoveTestSetSchedules(Int64 folderId, Int64 testSetScheduleId) => HttpRequest(HttpMethod.Delete, $"/odata/TestSetSchedules({testSetScheduleId})", folderId);

    public void EnableTestSetSchedules(Int64 folderId, bool enabled, IEnumerable<Int64> testSetScheduleIds)
        => HttpRequest(HttpMethod.Post, "/odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled", folderId, new { enabled, testSetScheduleIds });

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
        return GetEnumerable<TestDataQueueItem>("/odata/TestDataQueueItems", folderId, $"&$filter=(TestDataQueueId eq {testDataQueue.Id})");
    }

    public void AddTestDataQueueItems(Int64 folderId, string testDataQueueName, string itemJsonArray)
    {
        EnsureVersionSupport(14);
        // itemJsonArray is a pre-serialized JSON array supplied by the caller, so embed it raw;
        // testDataQueueName is escaped via JsonSerializer to survive quotes/backslashes.
        var payload = $"{{\"queueName\":{JsonSerializer.Serialize(testDataQueueName ?? "")},\"items\":{itemJsonArray}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/BulkAddItems", folderId, payload);
    }

    // Single-item add — the per-item fallback when a BulkAddItems batch is rejected,
    // so one bad row doesn't lose the whole batch. contentJson is a pre-serialized
    // JSON object (the item's ContentJson), embedded raw.
    public void AddTestDataQueueItem(Int64 folderId, string testDataQueueName, string contentJson)
    {
        EnsureVersionSupport(14);
        var payload = $"{{\"queueName\":{JsonSerializer.Serialize(testDataQueueName ?? "")},\"content\":{contentJson}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/AddItem", folderId, payload);
    }

    // Full-replace update of a Test Data Queue (PUT). Used to restore a queue's
    // original ContentJsonSchema after a migration that temporarily relaxed it.
    // PATCH is not supported by this OData entity (returns 404); PUT is.
    public TestDataQueue? UpdateTestDataQueue(Int64 folderId, Int64 testDataQueueId, TestDataQueue testDataQueue)
    {
        EnsureVersionSupport(14);
        return HttpRequest<TestDataQueue>(HttpMethod.Put, $"/odata/TestDataQueues({testDataQueueId})", folderId, testDataQueue);
    }

    public void SetAllTestDataQueueItemsConsumed(Int64 folderId, string testDataQueueName, bool isConsumed)
    {
        EnsureVersionSupport(14);
        var payload = $"{{\"queueName\":{JsonSerializer.Serialize(testDataQueueName ?? "")},\"isConsumed\":{(isConsumed ? "true" : "false")}}}";
        HttpRequest(HttpMethod.Post, "/api/TestDataQueueActions/SetAllItemsConsumed", folderId, payload);
    }

    #endregion

    #region Calendar

    public IEnumerable<ExtendedCalendar> GetCalendars() => GetEnumerable<ExtendedCalendar>($"/odata/Calendars");

    // To get ExcludedDates, GetCalendars(id) must be called.
    public ExtendedCalendar? GetCalendar(Int64 calendarId) => HttpRequest<ExtendedCalendar>(HttpMethod.Get, $"/odata/Calendars({calendarId})");

    public ExtendedCalendar? PostCalendar(ExtendedCalendar calendar) => HttpRequest<ExtendedCalendar>(HttpMethod.Post, "/odata/Calendars", null, calendar);

    public ExtendedCalendar? PutCalendar(ExtendedCalendar calendar) => HttpRequest<ExtendedCalendar>(HttpMethod.Put, $"/odata/Calendars({calendar.Id!.Value})", null, calendar);

    public void RemoveCalendar(Int64 calendarId) => HttpRequest(HttpMethod.Delete, $"/odata/Calendars({calendarId})");

    #endregion

    #region Maintenance

    // Host only
    //    public MaintenanceSetting? GetMaintenance(int? tenantId) => HttpRequest<MaintenanceSetting>(HttpMethod.Get, $"/api/Maintenance/Get?tenantId={tenantId}");

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

    public TaskCatalog? CreateTaskCatalog(Int64 folderId, TaskCatalog taskCatalog) => HttpRequest<TaskCatalog>(HttpMethod.Post, "/odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog", folderId, taskCatalog);

    public IEnumerable<TaskCatalog> GetTaskCatalogs(Int64 folderId) => GetEnumerable<TaskCatalog>("/odata/TaskCatalogs", folderId);

    public void RemoveTaskCatalog(Int64 folderId, Int64 catalogId) => HttpRequest(HttpMethod.Delete, $"/odata/TaskCatalogs({catalogId})", folderId);

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
    //public UserProfile? GetPmUserProfile() => HttpRequestIdentity<UserProfile>(HttpMethod.Get, "/api/Account/Profile");

    // This endpoint only works with host admin privileges
    //public void GetPmSetting()
    //{
    //    string body = HttpRequestIdentity(HttpMethod.Get, "/api/Setting");
    //}

    public DirectoryScope[]? GetPmDirectoryScope(string partitionGlobalId) => HttpRequestIdentity<DirectoryScope[]>(HttpMethod.Get, $"/api/Directory/Scopes/{partitionGlobalId}");

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
    // No "PM.xxx" OAuth scope needed for this endpoint.
    // The request body intentionally omits the "scope" field (does not apply to aad).
    // Resolve returns a bare GUID = UiPath's internal id, the id 1st parties should consume.
    public Dictionary<string, PmGroupMember> PmBulkResolveByName(string partitionGlobalId, string entityType, IEnumerable<string> names)
    {
        var postData = new BulkResolveByNameCommand()
        {
            entityNames = names as string[] ?? names.ToArray(),
            entityType = entityType
        };

        string body = HttpRequestIdentity(HttpMethod.Post, $"/api/Directory/BulkResolveByName/{partitionGlobalId}", null, postData);
        return JsonSerializer.Deserialize<Dictionary<string, PmGroupMember>>(body, jsoMemberConverter) ?? [];
    }

    public PmDirectoryEntityInfo[]? SearchPmDirectory(string partitionGlobalId, string userName) => GetEnumerableWithoutPagingIdentity<PmDirectoryEntityInfo>($"/api/Directory/Search/{partitionGlobalId}", null, $"&startsWith={Uri.EscapeDataString(userName)}");//+ "&sourceFilter=localUsers"//+ "&sourceFilter=localGroups"//+ "&sourceFilter=directoryUsers"//+ "&sourceFilter=directoryGroups"//+ "&sourceFilter=robotAccounts"//+ "&sourceFilter=applications");

    // undocumented API
    //public IEnumerable<PmUser> GetPmDirectoryUsers(string partitionGlobalId)
    //{
    //    return GetEnumerablePm2<PmUser>("/api/UserPartition/licenses", null, $"&partitionGlobalId={partitionGlobalId}");
    //}

    public BulkCreateResponse? CreatePmUserBulk(CreateUsersCommand createUsersCommand) => HttpRequestIdentity<BulkCreateResponse>(HttpMethod.Post, $"/api/User/BulkCreate", null, createUsersCommand);

    // This API is disabled and cannot be used
    public void GetPmUserLoginAttempts(string userId) => HttpRequestIdentity(HttpMethod.Get, $"/api/User/{userId}/loginAttempts");

    public void PutPmUser(string userId, PowerShell.Entities.UpdateUserCommand command) =>
        // Returns something like {"succeeded":true,"errors":[]}, but we can ignore it.
        // Errors are handled via exceptions anyway.
        HttpRequestIdentity(HttpMethod.Put, $"/api/User/{userId}", null, command);

    public void RemovePmUserDeprecated(string userId) => HttpRequestIdentity(HttpMethod.Delete, $"/api/User/{userId}");

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

    public void PutPmUserSetting(UpdatePmUserSettingPayload payload) =>
        //HttpRequestPortal(HttpMethod.Put, $"/portal_/api/identity/Setting", null, payload);
        HttpRequestIdentity(HttpMethod.Put, $"/api/Setting", null, payload);

    public PmGroup[] GetPmGroups(string partitionGlobalId) => GetEnumerableWithoutPagingIdentity<PmGroup>($"/api/Group/{partitionGlobalId}") ?? [];

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
    public IEnumerable<AvailableUserBundle> GetPmLicenses(string? partitionGlobalId) => HttpRequestPortal<AvailableUserBundle[]>(HttpMethod.Get, "/api/license/accountant/UserLicense") ?? [];

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
    public IEnumerable<NuLicensedGroup> GetPmLicensedGroups(string? partitionGlobalId) => GetEnumerablePortal<NuLicensedGroup>("/api/license/accountant/UserLicense/group/page");

    // This is an undocumented API.
    public void RemovePmLicensedGroup(string? groupId)
    {
        if (groupId is null) return;
        var removeGroup = new { id = groupId };
        // nothing returns
        HttpRequestPortal(HttpMethod.Delete, "/api/license/accountant/UserLicense/group", null, removeGroup);
    }

    // This is an undocumented API.
    public IEnumerable<NuLicensedGroupMember> GetPmLicenseGroupAllocations(string? groupId) => GetEnumerablePortal<NuLicensedGroupMember>($"/api/license/accountant/UserLicense/group/{groupId}/allocations");

    // This is an undocumented API.
    public UpdateLicensedGroupResponse? PutPmLicenseGroup(UpdateLicensedGroupCommand command) => HttpRequestPortal<UpdateLicensedGroupResponse>(HttpMethod.Put, "/api/license/accountant/UserLicense/group", null, command);

    // This is an undocumented API.
    public void DeletePmLicenseGroupAllocations(string? groupId, string userId) => HttpRequestPortal(HttpMethod.Delete, $"/api/license/accountant/UserLicense/group/{groupId}/user/{userId}");

    // This is an undocumented API.
    // partitionGlobalId is not needed for some reason, but the parameter is added to integrate with the cache.
    public IEnumerable<NuLicensedUser> GetPmLicensedUsers(string? partitionGlobalId) => GetEnumerablePortal<NuLicensedUser>("/portal_/api/license/accountant/UserLicense/user/page");

    public void PutLicensedUser(AddLicensedUserCommand payload) => HttpRequestPortal(HttpMethod.Post, "/portal_/api/license/accountant/UserLicense/users", null, payload);

    // This is an undocumented API.
    // Sets the listed users' allocated license bundles atomically (replace, not
    // append). Captured from the Portal "Licenses > Users" UI; the bare endpoint
    // (no /users suffix) and PUT verb are distinct from PutLicensedUser's
    // POST .../users. Also makes a user a licensed user when they aren't yet,
    // so a single PUT covers both add-user and bundle-assignment.
    public void PutPmLicenseUser(UpdateLicensedUserCommand command) => HttpRequestPortal(HttpMethod.Put, "/portal_/api/license/accountant/UserLicense", null, command);

    // This is an undocumented API.
    // Drops the listed users from the licensed-users set entirely (cleans up the
    // "No license" rows left behind by an empty-licenseCodes PutPmLicenseUser).
    // Captured from the Portal "License Allocations to Users" UI delete action;
    // analog of RemovePmLicensedGroup, but the body is {userIds:[]} (batched)
    // rather than {id:string} (single). Batching one DELETE per drive is the
    // natural fit for this shape and avoids N round trips for N users.
    public void RemovePmLicensedUser(string[] userIds)
    {
        if (userIds is null || userIds.Length == 0) return;
        var cmd = new { userIds };
        HttpRequestPortal(HttpMethod.Delete, "/portal_/api/license/accountant/UserLicense", null, cmd);
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

    public void RemovePmGroup(string partitionGlobalId, string? groupId)
    {
        if (groupId is null) return;
        var removeGroup = new { groupIDs = new[] { groupId } };
        // nothing returns
        HttpRequestIdentity(HttpMethod.Delete, $"/api/Group/{partitionGlobalId}", null, removeGroup);
    }

    public IEnumerable<PmRobotAccount> GetPmRobotAccounts(string partitionGlobalId) => GetEnumerableIdentity<PmRobotAccount>($"/api/RobotAccount/{partitionGlobalId}");

    public PmRobotAccount? CreatePmRobot(CreateRobotAccountCommand cmd) => HttpRequestIdentity<PmRobotAccount>(HttpMethod.Post, $"/api/RobotAccount", null, cmd);

    // partitionGlobalId is not needed for some reason.
    public PmRobotAccount? UpdatePmRobot(string robotId, UpdateRobotAccountCommand cmd) => HttpRequestIdentity<PmRobotAccount>(HttpMethod.Put, $"/api/RobotAccount/{robotId}", null, cmd);

    public void RemovePmRobot(string partitionGlobalId, string robotId)
        => HttpRequestIdentity(HttpMethod.Delete, $"/api/RobotAccount/{partitionGlobalId}", null, new { robotAccountIDs = new[] { robotId } });

    // partitionGlobalId is not needed for some reason, but the parameter is added to integrate with the cache.
    public IEnumerable<ExternalResource> GetPmExternalApiResource(string partitionGlobalId) => GetEnumerableWithoutPagingIdentity<ExternalResource>("/api/ExternalApiResource") ?? [];

    public IEnumerable<ExternalClient> GetPmExternalClients(string partitionGlobalId) => GetEnumerableWithoutPagingIdentity<ExternalClient>($"/api/ExternalClient/{partitionGlobalId}") ?? [];

    public ExternalClient? GetPmExternalClient(string? partitionGlobalId, string id) => HttpRequestIdentity<ExternalClient>(HttpMethod.Get, $"/api/ExternalClient/{partitionGlobalId}/{id}");

    // This probably does not need a wrapper method in OrchDriveInfo.
    public ExternalClientCreated? PostPmExternalClient(CreateExternalClientCommand app) => HttpRequestIdentity<ExternalClientCreated>(HttpMethod.Post, "/api/ExternalClient", null, app);

    // Returns nothing
    public void DeletePmExternalClient(string partitionGlobalId, string id) => HttpRequestIdentity(HttpMethod.Delete, $"/api/ExternalClient/{partitionGlobalId}/{id}");

    // Forbidden for external app
    //public IEnumerable<int> GetIdentityClient()
    //{
    //    return GetEnumerableIdentity<int>($"/Client");
    //}

    // The completer could use the return value of GetIdentityAvailableDirectoryTypes().
    // But what would it be useful for...
    // identifier needs to be a directory adapter.
    // e.g., "aad", "Saml2", "scim", etc.
    //public void GetPmDirectoryConfiguration(string identifier) => HttpRequestIdentity(HttpMethod.Get, $"/api/DirectoryConnection/DirectoryConfiguration?identifier={identifier}");

    // Returns something like ["aad","Saml2","scim"].
    //public string[]? GetPmAvailableDirectoryTypes() => HttpRequestIdentity<string[]>(HttpMethod.Get, "/api/DirectoryConnection/AvailableDirectoryTypes");

    // Seems to return an empty array. Hmm.
    //public void GetPmExternalIdentityProvider(string partitionGlobalId) => HttpRequestIdentity(HttpMethod.Get, $"/api/ExternalIdentityProvider?partitionGlobalId={partitionGlobalId}");

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

    public void GetIdentitySetting(string partitionGlobalId, string userId) => HttpRequestIdentity(HttpMethod.Get, $"/api/Setting", null, (object)$"&partitionGlobalId={partitionGlobalId}&userId={userId}");

    // Typed read of a user's identity settings (theme, language, ...). The GET
    // returns nothing unless explicit key filters are supplied, so the caller
    // passes the keys to fetch; they go on the URL as repeated key= params
    // alongside partitionGlobalId and userId (omitting userId makes it a
    // partition-level read that requires host admin).
    public PmUserSettingDto[]? GetUserSettings(string partitionGlobalId, string userId, IEnumerable<string> keys)
    {
        var keyQuery = string.Concat(keys.Select(k => $"key={Uri.EscapeDataString(k)}&"));
        return HttpRequestIdentity<PmUserSettingDto[]>(HttpMethod.Get,
            $"/api/Setting?{keyQuery}partitionGlobalId={partitionGlobalId}&userId={userId}");
    }

    // The notification (usersubscriptionservice) endpoints are account-scoped and
    // addressed by the account GUID (partitionGlobalId), e.g.
    // https://cloud.uipath.com/{accountId}/notificationservice_/usersubscriptionservice/api/v1/...
    private string NotificationServiceBase(string partitionGlobalId) =>
        $"{new Uri(_base_url).GetLeftPart(UriPartial.Authority)}/{partitionGlobalId}/notificationservice_/usersubscriptionservice/api/v1";

    // Reads the connected user's notification subscriptions (publishers -> topics ->
    // per-mode subscription state). No userId is taken; the service uses the token's user.
    public PmNotificationSubscriptionResponse? GetUserSubscriptions(string partitionGlobalId)
    {
        string body = HttpRequestImpl(HttpMethod.Get, NotificationServiceBase(partitionGlobalId), "/UserSubscription/", null, (string?)null);
        return string.IsNullOrEmpty(body) ? null : JsonSerializer.Deserialize<PmNotificationSubscriptionResponse>(body);
    }

    // Updates one or more (topicId, mode) subscription toggles for the connected user.
    public void UpdateUserSubscriptions(string partitionGlobalId, UpdateUserSubscriptionPayload payload) => HttpRequestImpl(HttpMethod.Post, NotificationServiceBase(partitionGlobalId), "/UserSubscription/", null, payload);

    // Seems to return an empty array. Hmm.
    //public string GetPmRule(string partitionGlobalId) => HttpRequestIdentity(HttpMethod.Get, $"/api/Rule/{partitionGlobalId}");

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

    public AccessAllowedMember[] GetPmPartitionAccessPolicy(string partitionGlobalId) => HttpRequestPortal<AccessAllowedMember[]>(HttpMethod.Get, $"/api/identity/PartitionAccessPolicy/{partitionGlobalId}") ?? [];

    #endregion

    #region Document Understanding

    public DuProject[] GetDuProjects()
    {
        var body = HttpRequest<DuGetProjectsResponse>(HttpMethod.Get, "/du_/api/framework/projects?api-version=1");
        return body?.projects ?? [];
    }

    // Does not seem to work... This is problematic.
    public void CreateDuProjects(CreateDuProjectCmd cmd)
    {
        var body = HttpRequest(HttpMethod.Post, "/du_/api/app/web/projects?api-version=1.4", null, cmd);
    }

    // Undocumented (not in the public DU swagger): the same internal app/web API the DU web
    // app uses to delete a project. projectId is the DuProject GUID.
    public void RemoveDuProject(string projectId) => HttpRequest(HttpMethod.Delete, $"/du_/api/app/web/projects/{projectId}?api-version=1.4");

    public DuDocumentType[] GetDuDocumentTypes(string? projectId)
    {
        var body = HttpRequest<DuGetDocumentTypesResponse>(HttpMethod.Get, $"/du_/api/framework/projects/{projectId}/document-types?api-version=1");
        return body?.documentTypes ?? [];
    }

    public DuClassifier[] GetDuClassifiers(string? projectId)
    {
        var body = HttpRequest<DuGetClassifiersResponse>(HttpMethod.Get, $"/du_/api/framework/projects/{projectId}/classifiers?api-version=1");
        return body?.classifiers ?? [];
    }

    public DuExtractor[] GetDuExtractors(string? projectId)
    {
        var body = HttpRequest<DuGetExtractorsResponse>(HttpMethod.Get, $"/du_/api/framework/projects/{projectId}/extractors?api-version=1");
        return body?.extractors ?? [];
    }

    // TODO: Should pagination be supported?
    public DuUser[] GetDuUsers(string? partitionGlobalId, string? tenantKey, string? projectId)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/userroleassignments?scope=/tenant/{tenantKey}/DocumentUnderstanding/projects/{projectId}&serviceName=DocumentUnderstanding";

        string body = HttpRequestImpl(HttpMethod.Get, baseUrl, "");
        var results = JsonSerializer.Deserialize<HttpBodyResults<DuUser>>(body);
        return results?.results ?? [];
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
    public DuRole[] GetDuRoles(string? partitionGlobalId)
    {
        Uri uri = new(_base_url);
        string baseUrl = $"{uri.Scheme}://{uri.Host}/{partitionGlobalId}/pap_/api/roles?scopeType=project&serviceName=DocumentUnderstanding";

        string body = HttpRequestImpl(HttpMethod.Get, baseUrl, "");
        var results = JsonSerializer.Deserialize<HttpBodyResults<DuRole>>(body);
        return results?.results ?? [];
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
                skip += (ulong)body.data.Count;
                if (total == first || !(body.paging?.nextPage.GetValueOrDefault() ?? false))
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
                skip += (ulong)body.data.Count;
                if (total == first || !body.hasNextPage.GetValueOrDefault())
                    break;
            }
            else
                break;
        }
    }

    // No pagination
    private IEnumerable<T> GetEnumerableTm3<T>(string endPoint, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        const ulong PageSize = 1000;
        ulong total = 0;
        while (true)
        {
            ulong top = Math.Min(first - total, PageSize);
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
                skip += (ulong)body.Count;
                // No paging envelope on this endpoint: stop on a short page (or once the
                // requested cap is reached) so a non-paginated response ends after one fetch
                // instead of re-fetching the same window forever.
                if (total == first || body.Count < (int)top)
                    break;
            }
            else
                break;
        }
    }

    public IEnumerable<TmProject> GetTmProjects() => GetEnumerableTm<TmProject>("/testmanager_/api/v2/projects");

    public string PutTmProject(TmProject project) => HttpRequest(HttpMethod.Put, $"/testmanager_/api/v2/projects/{project.id}", null, project);

    // Returns nothing
    public void RemoveTmProject(string projectId) => HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/projects/{projectId}");

    public IEnumerable<TmRequirement> GetTmRequirements(string projectId) => GetEnumerableTm2<TmRequirement>($"/testmanager_/api/v2/{projectId}/requirements");

    public string RemoveTmRequirements(string projectId, string requirementId) => HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/{projectId}/requirements/{requirementId}");

    public IEnumerable<TmTestCase> GetTmTestCases(string projectId) => GetEnumerableTm<TmTestCase>($"/testmanager_/api/v2/{projectId}/testcases");

    // Returns empty
    public string RemoveTmTestCase(string projectId, string testCaseId) => HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/{projectId}/testcases/{testCaseId}");

    public IEnumerable<TmTestSet> GetTmTestSets(string projectId) => GetEnumerableTm<TmTestSet>($"/testmanager_/api/v2/{projectId}/testsets");

    public string RemoveTmTestSet(string projectId, string testSetId) => HttpRequest(HttpMethod.Delete, $"/testmanager_/api/v2/{projectId}/testsets/{testSetId}");

    // This endpoint appears to return the same results as /testexecutions/filtered.
    // Are the parameters unusable?
    public IEnumerable<TmTestExecution> GetTmTestExecutions(string projectId) => GetEnumerableTm<TmTestExecution>($"/testmanager_/api/v2/{projectId}/testexecutions");

    public IEnumerable<TmTestExecution> GetTmTestExecutionsFiltered(string projectId) => GetEnumerableTm<TmTestExecution>($"/testmanager_/api/v2/{projectId}/testexecutions/filtered");

    public IEnumerable<TmTestExecutionResult> GetTmTestExecutionsResult(string projectId, string testExecutionId) => GetEnumerableTm<TmTestExecutionResult>($"/testmanager_/api/v2/{projectId}/testcaselogs/testexecution/{testExecutionId}/paged");

    public IEnumerable<TmRole> GetTmRoles() => GetEnumerableTm3<TmRole>("/testmanager_/api/v2/roles");

    public TmServerInfo? GetTmServerInfo() => HttpRequest<TmServerInfo>(HttpMethod.Get, "/testmanager_/api/serverinfo");

    public TmConfig? GetTmConfiguration() => HttpRequest<TmConfig>(HttpMethod.Get, "/testmanager_/api/configuration");

    public TmProjectSettings? GetTmProjectSettings(string projectId) => HttpRequest<TmProjectSettings>(HttpMethod.Get, $"/testmanager_/api/v2/{projectId}/projectsettings");

    public IEnumerable<TmProjectPermission> GetTmProjectPermission(string projectId) => GetEnumerableTm<TmProjectPermission>($"/testmanager_/api/v2/{projectId}/permissions/project");

    public IEnumerable<TmProjectPermission> GetTmDefects(string projectId) => GetEnumerableTm<TmProjectPermission>($"/testmanager_/api/v2/{projectId}/defects");

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
