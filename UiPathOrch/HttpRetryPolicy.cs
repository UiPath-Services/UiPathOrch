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

    // Half-width of the jitter band applied to the exponential backoff: the computed delay is
    // scaled by a factor drawn from [1 - JitterRatio, 1 + JitterRatio].
    //
    // Without jitter every caller that took the same 429 waits the identical 500ms / 1s / 2s and
    // they all retry in lockstep -- re-creating the burst that tripped the throttle, burning the
    // whole retry budget on synchronized collisions. That is the expected shape here rather than
    // an edge case: cmdlets fan out over folders and drives (Parallel.ForEach in the link/move
    // bases and the argument completers, OrchThreadPool in the Get-* paths) behind a 15 req/s
    // limiter that refills on a one-second boundary, so concurrent requests are already clustered
    // when they hit the limit. Spreading the retries lets them drain in sequence instead.
    internal const double JitterRatio = 0.25;

    // Backoff before the next transient retry. Honors a server Retry-After when present,
    // otherwise exponential (BaseDelay * 2^attempt * jitterFactor). Both are clamped to
    // [0, MaxDelay] so a hostile or huge Retry-After can't hang the cmdlet.
    //
    // jitterFactor is passed in rather than drawn here so this stays a pure function of its
    // arguments (like the rest of this class) and its exact-value tests keep asserting exact
    // values; live callers pass NextJitterFactor(). It deliberately does NOT apply to a server
    // Retry-After -- that is an instruction, not an estimate, so it is honored as sent.
    internal static TimeSpan BackoffDelay(int transientAttempt, TimeSpan? retryAfter, double jitterFactor = 1.0)
    {
        if (retryAfter is { } ra)
        {
            return Clamp(ra, TimeSpan.Zero, MaxDelay);
        }
        double ms = BaseDelay.TotalMilliseconds * Math.Pow(2, Math.Max(0, transientAttempt)) * jitterFactor;
        return Clamp(TimeSpan.FromMilliseconds(ms), TimeSpan.Zero, MaxDelay);
    }

    // The jitter factor a live caller feeds to BackoffDelay. Random.Shared is thread-safe and
    // lock-free on .NET 6+, and avoids the classic trap of per-thread `new Random()` instances
    // seeded from the same clock tick -- which would hand every parallel retry the SAME factor
    // and defeat the entire point.
    internal static double NextJitterFactor() =>
        1.0 + ((Random.Shared.NextDouble() * 2.0) - 1.0) * JitterRatio;

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
