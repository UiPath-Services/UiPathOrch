using System;
using System.Net;
using System.Net.Http;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins ExceptionCachePolicy: which failures the per-key exception caches treat as
// deterministic (cacheable so the same failing API call isn't retried) vs transient
// or recoverable (never cached). The notable case is 401 Unauthorized — it must NOT
// be cached, otherwise a token that merely expired or was rotated server-side gets a
// stale 401 re-thrown before the session's re-auth can run, wedging the cache slot.
public class ExceptionCachePolicyTests
{
    private static HttpResponseException Http(HttpStatusCode code) =>
        new("err", new HttpResponseMessage(code));

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]          // 400
    [InlineData(HttpStatusCode.Forbidden)]           // 403
    [InlineData(HttpStatusCode.NotFound)]            // 404
    [InlineData(HttpStatusCode.Gone)]                // 410
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.NotImplemented)]      // 501
    [InlineData(HttpStatusCode.BadGateway)]          // 502
    public void Deterministic_statuses_are_cacheable(HttpStatusCode code)
    {
        Assert.True(ExceptionCachePolicy.IsDeterministic(Http(code)));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]        // 401 — recoverable via re-auth, must NOT cache
    [InlineData(HttpStatusCode.RequestTimeout)]      // 408
    [InlineData(HttpStatusCode.TooManyRequests)]     // 429
    [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]      // 504
    public void Transient_or_recoverable_statuses_are_not_cacheable(HttpStatusCode code)
    {
        Assert.False(ExceptionCachePolicy.IsDeterministic(Http(code)));
    }

    [Fact]
    public void Unauthorized_is_not_cached_so_reauth_can_recover()
    {
        // Regression guard for the "401 wedges the cache" bug. The session flips
        // _isAuthenticated off on a 401 and re-authenticates on the next call; caching
        // the 401 would re-throw it before that ever runs.
        Assert.False(ExceptionCachePolicy.IsDeterministic(Http(HttpStatusCode.Unauthorized)));
    }

    [Fact]
    public void DeterministicApiException_is_cacheable()
    {
        Assert.True(ExceptionCachePolicy.IsDeterministic(new DeterministicApiException("version not supported")));
    }

    [Fact]
    public void Non_http_exceptions_are_not_cacheable()
    {
        Assert.False(ExceptionCachePolicy.IsDeterministic(new InvalidOperationException("x")));
        Assert.False(ExceptionCachePolicy.IsDeterministic(new HttpRequestException("network down")));
        Assert.False(ExceptionCachePolicy.IsDeterministic(new OperationCanceledException()));
    }

    // Behavioral guard on the actual cache: a 401 must not wedge the slot (so the next
    // Get() reaches the API and re-auths), while a deterministic 404 stays wedged.
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
}
