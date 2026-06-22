using System;
using System.Net;
using System.Net.Http;
using UiPath.PowerShell.Commands;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins ExceptionCachePolicy: how long the per-key exception caches keep a failure (so the same
// failing API call isn't re-made) — Permanent (deterministic), a short TTL (500/502, which clear
// over minutes), or not at all (transient/recoverable). Cross-cutting invariants pinned here:
//   * 401 must NOT be cached, or a token that merely expired/rotated gets a stale 401 re-thrown
//     before the session's re-auth can run, wedging the slot.
//   * The cache stays consistent with the retry layer: anything HttpRetryPolicy retries as
//     transient (429/503/504) is never cached here.
//   * 500/502 are cached briefly, then self-heal once the TTL passes (no Clear-OrchCache needed).
public class ExceptionCachePolicyTests
{
    private static HttpResponseException Http(HttpStatusCode code) =>
        new("err", new HttpResponseMessage(code));

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]          // 400
    [InlineData(HttpStatusCode.Forbidden)]           // 403
    [InlineData(HttpStatusCode.NotFound)]            // 404
    [InlineData(HttpStatusCode.Gone)]                // 410
    [InlineData(HttpStatusCode.NotImplemented)]      // 501
    public void Deterministic_statuses_cache_permanently(HttpStatusCode code)
    {
        Assert.Equal(ExceptionCachePolicy.Permanent, ExceptionCachePolicy.CacheDuration(Http(code)));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.BadGateway)]          // 502
    public void Server_errors_cache_with_a_finite_ttl(HttpStatusCode code)
    {
        var duration = ExceptionCachePolicy.CacheDuration(Http(code));
        Assert.Equal(ExceptionCachePolicy.TransientServerTtl, duration);
        Assert.True(duration > TimeSpan.Zero && duration < ExceptionCachePolicy.Permanent);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]        // 401 — recoverable via re-auth, must NOT cache
    [InlineData(HttpStatusCode.RequestTimeout)]      // 408
    [InlineData(HttpStatusCode.TooManyRequests)]     // 429
    [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]      // 504
    public void Transient_or_recoverable_statuses_are_not_cached(HttpStatusCode code)
    {
        Assert.Null(ExceptionCachePolicy.CacheDuration(Http(code)));
    }

    // Consistency with the retry layer: every status HttpRetryPolicy retries as transient must be
    // non-cacheable here, so the two layers can't disagree about what "transient" means.
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]     // 429
    [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]      // 504
    public void Retry_transient_statuses_are_never_cached(HttpStatusCode code)
    {
        Assert.True(HttpRetryPolicy.IsTransient(code));   // guard: these ARE the retry-transient set
        Assert.Null(ExceptionCachePolicy.CacheDuration(Http(code)));
    }

    [Fact]
    public void DeterministicApiException_caches_permanently()
    {
        Assert.Equal(ExceptionCachePolicy.Permanent,
            ExceptionCachePolicy.CacheDuration(new DeterministicApiException("version not supported")));
    }

    [Fact]
    public void Non_http_exceptions_are_not_cached()
    {
        Assert.Null(ExceptionCachePolicy.CacheDuration(new InvalidOperationException("x")));
        Assert.Null(ExceptionCachePolicy.CacheDuration(new HttpRequestException("network down")));
        Assert.Null(ExceptionCachePolicy.CacheDuration(new OperationCanceledException()));
    }

    // Behavioral guard on the actual cache: a 401 must not wedge the slot (so the next Get()
    // reaches the API and re-auths), while a deterministic 404 stays wedged.
    [Fact]
    public void Cache_does_not_wedge_on_401_but_does_on_404()
    {
        var afterUnauthorized = new ExceptionCachePerTenant();
        afterUnauthorized.CacheException(Http(HttpStatusCode.Unauthorized));
        afterUnauthorized.ThrowCachedExceptionIfAny();   // no throw == not wedged

        var afterNotFound = new ExceptionCachePerTenant();
        afterNotFound.CacheException(Http(HttpStatusCode.NotFound));
        Assert.Throws<HttpResponseException>(() => afterNotFound.ThrowCachedExceptionIfAny());
    }

    // A cached 500 short-circuits calls during the outage (fast no-API throw — what keeps
    // completers responsive), then self-heals once the TTL passes.
    [Fact]
    public void Server_error_is_cached_then_self_heals_after_ttl()
    {
        WithClock(() =>
        {
            var cache = new ExceptionCachePerTenant();
            cache.CacheException(Http(HttpStatusCode.InternalServerError));

            // Within the TTL: still wedged.
            _now = _base + ExceptionCachePolicy.TransientServerTtl - TimeSpan.FromSeconds(1);
            Assert.Throws<HttpResponseException>(() => cache.ThrowCachedExceptionIfAny());

            // Past the TTL: self-healed — no throw, so the next Get() re-probes the API.
            _now = _base + ExceptionCachePolicy.TransientServerTtl + TimeSpan.FromSeconds(1);
            cache.ThrowCachedExceptionIfAny();
        });
    }

    // Same self-heal behavior for the keyed cache; a permanent 404 on another key stays wedged.
    [Fact]
    public void Keyed_cache_server_error_self_heals_but_deterministic_stays()
    {
        WithClock(() =>
        {
            var cache = new ExceptionsCachePer<string>();
            cache.CacheException("blip", Http(HttpStatusCode.BadGateway));
            cache.CacheException("gone", Http(HttpStatusCode.NotFound));

            _now = _base + TimeSpan.FromSeconds(30);
            Assert.Throws<HttpResponseException>(() => cache.ThrowCachedExceptionIfAny("blip"));

            _now = _base + ExceptionCachePolicy.TransientServerTtl + TimeSpan.FromSeconds(1);
            cache.ThrowCachedExceptionIfAny("blip");                                             // self-healed
            Assert.Throws<HttpResponseException>(() => cache.ThrowCachedExceptionIfAny("gone")); // still wedged
        });
    }

    // --- clock harness ---
    private static readonly DateTime _base = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime _now = _base;

    private static void WithClock(Action body)
    {
        var original = ExceptionCachePolicy.Clock;
        _now = _base;
        ExceptionCachePolicy.Clock = () => _now;
        try { body(); }
        finally { ExceptionCachePolicy.Clock = original; }
    }
}
