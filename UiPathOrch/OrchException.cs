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
    /// <summary>
    /// True iff <paramref name="ex"/> will recur for the same inputs and is
    /// therefore safe to cache and re-throw without re-attempting the API
    /// call. Transient failures (network errors, timeouts, 5xx-transient,
    /// 429 throttling, cancellation) return false.
    /// </summary>
    public static bool IsDeterministic(Exception ex) => ex switch
    {
        HttpResponseException http => IsDeterministicStatus(http.StatusCode),
        DeterministicApiException => true,
        _ => false,
    };

    private static bool IsDeterministicStatus(HttpStatusCode code) => code switch
    {
        HttpStatusCode.BadRequest => true,           // 400 — query is malformed; same query, same error
        HttpStatusCode.Unauthorized => true,         // 401 — token won't auto-recover
        HttpStatusCode.Forbidden => true,            // 403 — permission won't auto-grant
        HttpStatusCode.NotFound => true,             // 404 — resource isn't coming back
        HttpStatusCode.Gone => true,                 // 410 — explicit permanent removal
        HttpStatusCode.InternalServerError => true,  // 500 — kept for back-compat (could be transient)
        HttpStatusCode.NotImplemented => true,       // 501 — feature absent on this server
        HttpStatusCode.BadGateway => true,           // 502 — kept for back-compat (could be transient)
        _ => false,                                  // 408/429/503/504/etc. — transient, do not cache
    };
}

public class ExceptionCachePerTenant
{
    private Exception? _exceptionCache;

    // Cache exceptions that will always fail for the same reason on subsequent
    // API calls, to prevent making the same failing API call again. Non-
    // deterministic exceptions are silently ignored (no cache write).
    public void CacheException(Exception ex)
    {
        if (ExceptionCachePolicy.IsDeterministic(ex))
        {
            _exceptionCache = ex;
        }
    }

    // Throw the cached exception if one exists
    public void ThrowCachedExceptionIfAny()
    {
        if (_exceptionCache is not null)
        {
            throw _exceptionCache;
        }
    }

    public void ClearCache()
    {
        _exceptionCache = null;
    }
}

public class ExceptionsCachePer<T> where T : IEquatable<T>
{
    // Note that Lazy<T> is thread-safe
    private readonly Lazy<ConcurrentDictionary<T, Exception>> _exceptionsCache =
        new(() => new ConcurrentDictionary<T, Exception>());

    // Cache exceptions that will always fail for the same reason on subsequent
    // API calls, to prevent making the same failing API call again. Non-
    // deterministic exceptions are silently ignored (no cache write).
    public void CacheException(T key, Exception ex)
    {
        if (ExceptionCachePolicy.IsDeterministic(ex))
        {
            _exceptionsCache.Value[key] = ex;
        }
    }

    // Throw the cached exception if one exists
    public void ThrowCachedExceptionIfAny(T key)
    {
        if (_exceptionsCache.IsValueCreated &&
            _exceptionsCache.Value.TryGetValue(key, out var ex))
        {
            throw ex;
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

                // Check for nested error details
                if (errorElement.TryGetProperty("details", out JsonElement detailsElement) && detailsElement.ValueKind == JsonValueKind.Array)
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

            // Combine the main message and the specific errors
            ret = string.Join(' ', new[] { title }.Concat(errorMessages).Distinct());
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
