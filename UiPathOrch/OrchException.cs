using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace UiPath.PowerShell.Commands;

public class HttpResponseException(string message, HttpResponseMessage response) : Exception(message)
{
    public HttpResponseMessage Response { get; } = response;
    public HttpStatusCode StatusCode => Response.StatusCode;
}

/// <summary>
/// Failures that will deterministically recur for the same input without an
/// HTTP round trip — e.g., a guard that says "this API version removed the
/// endpoint" or "this configuration cannot service this request". The
/// exception cache layer treats these as cacheable, equivalent in spirit to
/// an HTTP status code in the deterministic whitelist (404 / 410 / 501 etc.).
/// Do NOT use this for transient or environment-dependent failures.
/// </summary>
public class DeterministicApiException(string message, Exception? inner = null)
    : InvalidOperationException(message, inner);

internal static class ExceptionCachePolicy
{
    // 500/502 are usually transient (a server hiccup, gateway blip, or deploy) yet the retry
    // layer deliberately does not replay them, and they can take minutes — not seconds — to
    // clear. Caching them keeps tab-completion fast during the outage; a short TTL then lets the
    // slot self-heal without Clear-OrchCache. Sized to match "recovers in a couple of minutes".
    internal static readonly TimeSpan TransientServerTtl = TimeSpan.FromMinutes(2);

    // Sentinel duration: cache until ClearCache (a deterministic failure that won't recover).
    internal static readonly TimeSpan Permanent = TimeSpan.MaxValue;

    // Injectable clock so the TTL/expiry logic is unit-testable without real time.
    internal static Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

    /// <summary>
    /// How long <paramref name="ex"/> may be cached and re-thrown without re-attempting the API
    /// call:
    ///   <c>null</c> — do not cache (transient/recoverable; the next call must reach the API),
    ///   <see cref="Permanent"/> — cache until ClearCache (deterministic; retrying is pointless),
    ///   a finite span — cache then expire (transient-over-minutes; self-heals after the TTL).
    /// </summary>
    internal static TimeSpan? CacheDuration(Exception ex) => ex switch
    {
        HttpResponseException http => DurationForStatus(http.StatusCode),
        DeterministicApiException => Permanent,
        _ => null,
    };

    private static TimeSpan? DurationForStatus(HttpStatusCode code)
    {
        // Stay consistent with the retry layer: anything HttpRetryPolicy retries as transient
        // (429/503/504) is owned by retry+backoff — never cache it here, so a condition that
        // survived the retries is re-probed on the next call. Deriving this from IsTransient
        // (rather than re-listing those codes) keeps the two layers from drifting apart.
        if (UiPath.OrchAPI.HttpRetryPolicy.IsTransient(code)) return null;

        return code switch
        {
            HttpStatusCode.BadRequest => Permanent,      // 400 — query is malformed; same query, same error
            HttpStatusCode.Forbidden => Permanent,       // 403 — permission won't auto-grant
            HttpStatusCode.NotFound => Permanent,        // 404 — resource isn't coming back
            HttpStatusCode.Gone => Permanent,            // 410 — explicit permanent removal
            HttpStatusCode.NotImplemented => Permanent,  // 501 — feature absent on this server
            // 500/502: the retry layer does NOT replay these (an immediate retry won't fix a
            // server error), but they often clear over minutes — cache briefly, then self-heal.
            HttpStatusCode.InternalServerError => TransientServerTtl,  // 500
            HttpStatusCode.BadGateway => TransientServerTtl,           // 502
            // 401 is deliberately NOT cached: the session flips _isAuthenticated off on a 401 and
            // the next call re-authenticates (a token that merely expired/rotated then recovers);
            // caching it would re-throw before that re-auth runs, wedging the slot until
            // Clear-OrchCache. 408 and everything else are likewise not cached.
            _ => null,
        };
    }

    // Absolute expiry instant for a cacheable exception, or null when it must not be cached.
    internal static DateTime? ExpiryFor(Exception ex)
    {
        if (CacheDuration(ex) is not { } span) return null;
        return span == Permanent ? DateTime.MaxValue : Clock() + span;
    }
}

// Holds a cached failure and when it expires. Kept a class (not a struct) so the cache field stays
// a single atomic reference — ThrowCachedExceptionIfAny reads it without a lock, and a torn struct
// read could otherwise surface a half-written entry.
internal sealed class CachedError(Exception exception, DateTime expiresUtc)
{
    public Exception Exception { get; } = exception;
    public DateTime ExpiresUtc { get; } = expiresUtc;
    public bool IsExpired => ExceptionCachePolicy.Clock() >= ExpiresUtc;
}

public class ExceptionCachePerTenant
{
    private CachedError? _cached;

    // Cache an exception that should short-circuit subsequent API calls, per ExceptionCachePolicy.
    // Exceptions the policy declines (null duration) are silently ignored (no cache write).
    public void CacheException(Exception ex)
    {
        if (ExceptionCachePolicy.ExpiryFor(ex) is { } expiresUtc)
        {
            _cached = new CachedError(ex, expiresUtc);
        }
    }

    // Re-throw the cached exception if one exists and is still within its TTL. An expired entry
    // is dropped (self-heal) so the next call reaches the API again.
    public void ThrowCachedExceptionIfAny()
    {
        var cached = _cached;
        if (cached is null) return;
        if (cached.IsExpired)
        {
            _cached = null;
            return;
        }
        throw cached.Exception;
    }

    public void ClearCache()
    {
        _cached = null;
    }
}

public class ExceptionsCachePer<T> where T : IEquatable<T>
{
    // Note that Lazy<T> is thread-safe
    private readonly Lazy<ConcurrentDictionary<T, CachedError>> _exceptionsCache =
        new(() => new ConcurrentDictionary<T, CachedError>());

    // Cache an exception that should short-circuit subsequent API calls for this key, per
    // ExceptionCachePolicy. Exceptions the policy declines (null duration) are silently ignored.
    public void CacheException(T key, Exception ex)
    {
        if (ExceptionCachePolicy.ExpiryFor(ex) is { } expiresUtc)
        {
            _exceptionsCache.Value[key] = new CachedError(ex, expiresUtc);
        }
    }

    // Re-throw the cached exception for this key if one exists and is still within its TTL. An
    // expired entry is dropped (self-heal) so the next call for that key reaches the API again.
    public void ThrowCachedExceptionIfAny(T key)
    {
        if (_exceptionsCache.IsValueCreated &&
            _exceptionsCache.Value.TryGetValue(key, out var cached))
        {
            if (cached.IsExpired)
            {
                _exceptionsCache.Value.TryRemove(key, out _);
                return;
            }
            throw cached.Exception;
        }
    }

    public bool ClearCache(T? key)
    {
        if (key is null) return false;
        return _exceptionsCache.Value.TryRemove(key, out var _);
    }

    /// <summary>
    /// Drop every cached exception whose key matches the predicate. Useful for
    /// composite-key exception caches when an external mutation invalidates only
    /// a slice of the cache (e.g. all entries for one folderId across all keys).
    /// </summary>
    public void ClearCache(Func<T, bool> predicate)
    {
        if (!_exceptionsCache.IsValueCreated) return;
        // Snapshot keys before mutation: ConcurrentDictionary.Keys is a live view.
        foreach (var key in _exceptionsCache.Value.Keys.Where(predicate).ToList())
        {
            _exceptionsCache.Value.TryRemove(key, out _);
        }
    }

    public void ClearCache()
    {
        if (_exceptionsCache.IsValueCreated)
        {
            _exceptionsCache.Value.Clear();
        }
    }
}

public class OrchException : Exception
{
    public object? Target;

    internal static string? ExtractMessage(string msg)
    {
        string? ret = null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(msg);
            JsonElement root = doc.RootElement;

            // Helper method to get property value safely
            static JsonElement GetPropertyValue(JsonElement element, string propertyName)
            {
                element.TryGetProperty(propertyName, out JsonElement value);
                return value;
            }

            // Extract the main error message from the root or from the "error" object
            string title = GetPropertyValue(root, "title").ToString();
            if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "message").ToString();
            if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "Message").ToString();
            if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "errorMessage").ToString();
            if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "error").ToString();

            // Check if there's an "error" object containing the message
            if (root.TryGetProperty("error", out JsonElement errorElement) && errorElement.ValueKind == JsonValueKind.Object)
            {
                string errorMessage = GetPropertyValue(errorElement, "message").ToString();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    title = errorMessage;
                }

                // Check for nested error details — either an array of
                // { message } objects, or (ABP envelopes) a plain string that
                // carries the actionable text, e.g. "You are not allowed to
                // perform this operation."
                if (errorElement.TryGetProperty("details", out JsonElement detailsElement))
                {
                    if (detailsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement detail in detailsElement.EnumerateArray())
                        {
                            string detailMessage = GetPropertyValue(detail, "message").ToString();
                            if (!string.IsNullOrEmpty(detailMessage))
                            {
                                title = detailMessage; // If there are multiple details, the last one will be used.
                            }
                        }
                    }
                    else if (detailsElement.ValueKind == JsonValueKind.String)
                    {
                        string detail = detailsElement.ToString();
                        if (!string.IsNullOrEmpty(detail) && !string.Equals(detail, title, StringComparison.Ordinal))
                        {
                            title = string.IsNullOrEmpty(title) ? detail : $"{title} {detail}";
                        }
                    }
                }
            }

            // Extract specific errors
            List<string> errorMessages = new();
            if (root.TryGetProperty("errors", out JsonElement errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty errorProperty in errorsElement.EnumerateObject())
                {
                    string propertyName = errorProperty.Name;
                    JsonElement errorMessagesArray = errorProperty.Value;

                    if (errorMessagesArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement errorMessage in errorMessagesArray.EnumerateArray())
                        {
                            errorMessages.Add($"{propertyName}: {errorMessage.ToString()}");
                        }
                    }
                }
            }

            // Bulk-operation envelopes carry per-item failures as an ARRAY of
            // { code, description } (e.g. Add-/Copy-OrchPmUser DuplicateUserName,
            // Add-DuUser), often nested under a "result" object. Pull out each
            // description (or message) so the readable text isn't lost to the
            // raw-JSON fallback.
            static void CollectErrorArray(JsonElement parent, List<string> sink)
            {
                if (parent.ValueKind == JsonValueKind.Object &&
                    parent.TryGetProperty("errors", out JsonElement arr) &&
                    arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement e in arr.EnumerateArray())
                    {
                        if (e.ValueKind != JsonValueKind.Object) continue;
                        string desc = GetPropertyValue(e, "description").ToString();
                        if (string.IsNullOrEmpty(desc)) desc = GetPropertyValue(e, "message").ToString();
                        if (!string.IsNullOrEmpty(desc)) sink.Add(desc);
                    }
                }
            }
            CollectErrorArray(root, errorMessages);
            if (root.TryGetProperty("result", out JsonElement resultElement))
            {
                CollectErrorArray(resultElement, errorMessages);
            }

            // Combine the main message and the specific errors (dropping empties
            // so an absent title doesn't leave a leading space).
            ret = string.Join(' ', new[] { title }.Concat(errorMessages)
                .Where(s => !string.IsNullOrEmpty(s)).Distinct());
        }
        catch (Exception ex)
        {
            // Falls back to the original message; log so unexpected error envelopes can be diagnosed.
            System.Diagnostics.Debug.WriteLine($"OrchException.GetReadableMessage parse failed: {ex.GetType().Name}: {ex.Message}");
        }

        if (!string.IsNullOrEmpty(ret))
        {
            return ret;
        }

        // Return original message if parsing fails
        return msg;
    }

    internal static string? ExtractMessage(Exception ex)
    {
        return ExtractMessage(ex.Message);
    }

    private static string CreateExceptionMessage(string? target, string? message)
    {
        if (string.IsNullOrEmpty(target))
            return message ?? "";
        else
            return "\"" + target + "\": " + message;
    }

    private static string CreateExceptionMessage(string? target, Exception ex)
    {
        return CreateExceptionMessage(target, ExtractMessage(ex));
    }

    private static string CreateExceptionMessage(string target, string message, Exception ex)
    {
        return CreateExceptionMessage(target, $"{message}: {ExtractMessage(ex)}");
    }

    public OrchException(object? target, Exception ex)
        : base(CreateExceptionMessage(target?.ToString() ?? "", ex), ex)
    {
    }

    public OrchException(string? target, string message)
        : base(CreateExceptionMessage(target, message))
    {
    }

    public OrchException(string? target, Exception ex)
        : base(CreateExceptionMessage(target, ex), ex)
    {
    }

    public OrchException(string target, string message, Exception ex)
        : base(CreateExceptionMessage(target, message, ex), ex)
    {
    }
}
