using System;
using System.Net;
using System.Net.Http.Headers;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins the pure retry/backoff decision logic used by the HTTP chokepoint:
//   - 401 -> re-auth once, then return (no loop)
//   - 429/503/504 -> back off and retry up to MaxTransientRetries, honoring Retry-After
//   - everything else -> return unchanged
public class HttpRetryPolicyTests
{
    [Fact]
    public void Unauthorized_triggers_a_single_reauth()
    {
        Assert.Equal(HttpRetryPolicy.Action.Reauth,
            HttpRetryPolicy.Decide(HttpStatusCode.Unauthorized, isIdempotent: true, reauthUsed: false, transientAttempt: 0));
        // Once the one-shot re-auth is spent, a still-401 returns to the caller.
        Assert.Equal(HttpRetryPolicy.Action.Return,
            HttpRetryPolicy.Decide(HttpStatusCode.Unauthorized, isIdempotent: true, reauthUsed: true, transientAttempt: 0));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]    // 429
    [InlineData(HttpStatusCode.ServiceUnavailable)] // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]     // 504
    public void Transient_statuses_back_off_until_budget_exhausted(HttpStatusCode status)
    {
        Assert.True(HttpRetryPolicy.IsTransient(status));
        for (int attempt = 0; attempt < HttpRetryPolicy.MaxTransientRetries; attempt++)
        {
            Assert.Equal(HttpRetryPolicy.Action.Backoff,
                HttpRetryPolicy.Decide(status, isIdempotent: true, reauthUsed: false, transientAttempt: attempt));
        }
        // At the budget, stop retrying and return the response.
        Assert.Equal(HttpRetryPolicy.Action.Return,
            HttpRetryPolicy.Decide(status, isIdempotent: true, reauthUsed: false, transientAttempt: HttpRetryPolicy.MaxTransientRetries));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]                  // 200
    [InlineData(HttpStatusCode.BadRequest)]          // 400
    [InlineData(HttpStatusCode.Forbidden)]           // 403
    [InlineData(HttpStatusCode.NotFound)]            // 404
    [InlineData(HttpStatusCode.Conflict)]            // 409
    [InlineData(HttpStatusCode.InternalServerError)] // 500 — not retried (could be a real server bug)
    [InlineData(HttpStatusCode.BadGateway)]          // 502 — not retried
    public void Non_retryable_statuses_return(HttpStatusCode status)
    {
        Assert.False(HttpRetryPolicy.IsTransient(status));
        Assert.Equal(HttpRetryPolicy.Action.Return,
            HttpRetryPolicy.Decide(status, isIdempotent: true, reauthUsed: false, transientAttempt: 0));
    }

    [Fact]
    public void Backoff_is_exponential_and_capped_without_RetryAfter()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(500), HttpRetryPolicy.BackoffDelay(0, null)); // 500 * 2^0
        Assert.Equal(TimeSpan.FromSeconds(1), HttpRetryPolicy.BackoffDelay(1, null));        // 500 * 2^1
        Assert.Equal(TimeSpan.FromSeconds(2), HttpRetryPolicy.BackoffDelay(2, null));        // 500 * 2^2
        // Far-out attempts are clamped to MaxDelay rather than exploding.
        Assert.Equal(HttpRetryPolicy.MaxDelay, HttpRetryPolicy.BackoffDelay(20, null));
    }

    [Fact]
    public void Backoff_honors_RetryAfter_clamped_to_max()
    {
        Assert.Equal(TimeSpan.FromSeconds(7), HttpRetryPolicy.BackoffDelay(0, TimeSpan.FromSeconds(7)));
        // A huge Retry-After is capped so the cmdlet can't hang.
        Assert.Equal(HttpRetryPolicy.MaxDelay, HttpRetryPolicy.BackoffDelay(0, TimeSpan.FromMinutes(5)));
        // A negative Retry-After becomes zero.
        Assert.Equal(TimeSpan.Zero, HttpRetryPolicy.BackoffDelay(0, TimeSpan.FromSeconds(-3)));
    }

    [Fact]
    public void ResolveRetryAfter_handles_delta_date_and_absent()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
        Assert.Null(HttpRetryPolicy.ResolveRetryAfter(null, now));
        // delta-seconds form
        Assert.Equal(TimeSpan.FromSeconds(30),
            HttpRetryPolicy.ResolveRetryAfter(new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)), now));
        // HTTP-date in the future -> the remaining wait
        Assert.Equal(TimeSpan.FromSeconds(45),
            HttpRetryPolicy.ResolveRetryAfter(new RetryConditionHeaderValue(now.AddSeconds(45)), now));
        // HTTP-date already in the past -> zero, never negative
        Assert.Equal(TimeSpan.Zero,
            HttpRetryPolicy.ResolveRetryAfter(new RetryConditionHeaderValue(now.AddSeconds(-10)), now));
    }

    // C1 regression: a 503/504 can arrive after a non-idempotent POST (create/add) already committed
    // server-side, so retrying would duplicate it. Such requests must NOT back off on 503/504.
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)] // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]     // 504
    public void Non_idempotent_request_is_not_retried_on_5xx(HttpStatusCode status)
    {
        Assert.Equal(HttpRetryPolicy.Action.Return,
            HttpRetryPolicy.Decide(status, isIdempotent: false, reauthUsed: false, transientAttempt: 0));
    }

    // 429 means the request was rejected unprocessed, so it is safe to retry even a POST.
    [Fact]
    public void Non_idempotent_request_still_retries_on_429()
    {
        Assert.Equal(HttpRetryPolicy.Action.Backoff,
            HttpRetryPolicy.Decide(HttpStatusCode.TooManyRequests, isIdempotent: false, reauthUsed: false, transientAttempt: 0));
    }

    // Idempotent methods (GET/PUT/DELETE/PATCH) re-apply safely, so they keep retrying 503/504.
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void Idempotent_request_still_retries_on_5xx(HttpStatusCode status)
    {
        Assert.Equal(HttpRetryPolicy.Action.Backoff,
            HttpRetryPolicy.Decide(status, isIdempotent: true, reauthUsed: false, transientAttempt: 0));
    }
}
