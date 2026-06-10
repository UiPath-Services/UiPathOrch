using System.Net;
using System.Net.Http.Headers;

namespace UiPath.OrchAPI;

// Pure retry / backoff policy for the single HTTP chokepoint (HttpClient_Send).
// Deliberately free of HttpClient / session state so the decision logic can be unit
// tested exhaustively without a live endpoint. Two independent retry budgets:
//   - 401 Unauthorized: re-authenticate and retry exactly ONCE (a token that merely
//     expired/rotated recovers; if a freshly-issued token is still rejected the auth
//     is genuinely broken and the caller trips its circuit breaker instead of looping).
//   - 429 / 503 / 504: transient server/throttle conditions, retried with backoff up
//     to MaxTransientRetries, honoring a server Retry-After when present.
// Everything else (2xx success, 4xx client errors, 5xx like 500/502) returns to the
// caller unchanged — retrying them is pointless.
internal static class HttpRetryPolicy
{
    internal const int MaxTransientRetries = 3;
    internal static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    internal enum Action
    {
        Return,   // stop: hand the response back to the caller (success or non-retryable)
        Reauth,   // 401: clear auth, re-authenticate, retry once
        Backoff,  // 429/503/504: wait BackoffDelay, then retry
    }

    // Decide what to do after receiving `status`.
    //   isIdempotent     — whether the request method is safe to replay (GET/PUT/DELETE/PATCH). A POST
    //                       create/add is not, so it must not be retried on 503/504 (the write may have
    //                       committed server-side before the gateway gave up -> duplicate). 429 is
    //                       retried regardless of method (it means the request was rejected unprocessed).
    //   reauthUsed       — whether the one-shot 401 re-auth has already been spent.
    //   transientAttempt — how many transient (429/5xx) retries have already happened.
    internal static Action Decide(HttpStatusCode status, bool isIdempotent, bool reauthUsed, int transientAttempt)
    {
        if (status == HttpStatusCode.Unauthorized)
        {
            return reauthUsed ? Action.Return : Action.Reauth;
        }
        if (IsTransient(status) && transientAttempt < MaxTransientRetries
            && (status == HttpStatusCode.TooManyRequests || isIdempotent))
        {
            return Action.Backoff;
        }
        return Action.Return;
    }

    internal static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests          // 429
        || status == HttpStatusCode.ServiceUnavailable    // 503
        || status == HttpStatusCode.GatewayTimeout;       // 504

    // Backoff before the next transient retry. Honors a server Retry-After when present,
    // otherwise exponential (BaseDelay * 2^attempt). Both are clamped to [0, MaxDelay] so
    // a hostile or huge Retry-After can't hang the cmdlet.
    internal static TimeSpan BackoffDelay(int transientAttempt, TimeSpan? retryAfter)
    {
        if (retryAfter is { } ra)
        {
            return Clamp(ra, TimeSpan.Zero, MaxDelay);
        }
        double ms = BaseDelay.TotalMilliseconds * Math.Pow(2, Math.Max(0, transientAttempt));
        return Clamp(TimeSpan.FromMilliseconds(ms), TimeSpan.Zero, MaxDelay);
    }

    // Resolve an HTTP Retry-After header (delta-seconds OR an HTTP-date) to a wait,
    // clamped to be non-negative. Returns null when the header is absent/unparseable.
    internal static TimeSpan? ResolveRetryAfter(RetryConditionHeaderValue? retryAfter, DateTimeOffset now)
    {
        if (retryAfter is null) return null;
        if (retryAfter.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }
        if (retryAfter.Date is { } date)
        {
            TimeSpan wait = date - now;
            return wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
        }
        return null;
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan low, TimeSpan high) =>
        value < low ? low : (value > high ? high : value);
}
