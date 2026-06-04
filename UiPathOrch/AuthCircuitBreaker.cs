namespace UiPath.OrchAPI;

// Sticky "authentication is broken" latch for one session.
//
// Tripped only when a freshly re-issued token is STILL rejected with 401 — i.e. the
// one-shot re-auth ran and the new token is also unauthorized, which means the
// credential/grant is genuinely broken rather than merely expired. A token that just
// expired or was rotated recovers on the re-auth and never trips this.
//
// Once tripped, EnsureAuthenticated fails fast for every subsequent call instead of
// each call re-authenticating and retrying. That stops a broken-auth bulk operation
// from hammering the IdP/API (re-auth + retry) once per item; the caller sees one clear
// auth error per cmdlet. Reset only by rebuilding the session (Import-OrchConfig) — the
// deliberate recovery point after the underlying cause is fixed, so a transient blip is
// never latched forever without a way back (unlike caching the 401 itself, which the
// per-key exception cache deliberately does NOT do).
internal sealed class AuthCircuitBreaker
{
    private volatile System.Exception? _cause;

    internal bool IsTripped => _cause is not null;

    internal void Trip(System.Exception cause) => _cause = cause;

    internal void Reset() => _cause = null;

    // Re-throw the stored auth failure if the breaker is tripped (fail fast); no-op otherwise.
    internal void ThrowIfTripped()
    {
        System.Exception? cause = _cause;
        if (cause is not null)
        {
            throw cause;
        }
    }
}
