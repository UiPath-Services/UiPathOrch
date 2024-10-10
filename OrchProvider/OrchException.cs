using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands
{
    public class HttpResponseException(string message, HttpResponseMessage response) : Exception(message)
    {
        public HttpResponseMessage Response { get; } = response;
        public HttpStatusCode StatusCode => Response.StatusCode;
    }

    public class ExceptionCachePerTenant
    {
        private Exception? _exceptionCache;

        // 再度 API call しても、必ず同じ原因で失敗する例外についてはキャッシュしておき
        // あとでまた同じ API call してしまうことを抑止する
        public void CacheException(HttpResponseException ex)
        {
            // TODO: 例外をキャッシュすべき条件が、ほかにもないか？
            if (ex.StatusCode == HttpStatusCode.Forbidden ||
                ex.StatusCode == HttpStatusCode.BadGateway ||
                ex.StatusCode == HttpStatusCode.BadRequest ||
                ex.StatusCode == HttpStatusCode.Unauthorized ||
                ex.StatusCode == HttpStatusCode.InternalServerError ||
                ex.StatusCode == HttpStatusCode.NotFound)
            {
                _exceptionCache = ex;
            }
        }

        // キャッシュした例外があればスローする
        public void ThrowCachedExceptionIfAny()
        {
            if (_exceptionCache != null)
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
        // Lazy<T> はスレッドセーフであることに注意
        private readonly Lazy<ConcurrentDictionary<T, Exception>> _exceptionsCache =
            new(() => new ConcurrentDictionary<T, Exception>());

        // 再度 API call しても、必ず同じ原因で失敗する例外についてはキャッシュしておき
        // あとでまた同じ API call してしまうことを抑止する
        public void CacheException(T key, HttpResponseException ex)
        {
            // TODO: 例外をキャッシュすべき条件が、ほかにもないか？
            if (ex.StatusCode == HttpStatusCode.Forbidden ||
                ex.StatusCode == HttpStatusCode.BadGateway ||
                ex.StatusCode == HttpStatusCode.BadRequest ||
                ex.StatusCode == HttpStatusCode.Unauthorized ||
                ex.StatusCode == HttpStatusCode.InternalServerError ||
                ex.StatusCode == HttpStatusCode.NotFound)
            {
                _exceptionsCache.Value[key] = ex;
            }
        }

        // キャッシュした例外があればスローする
        public void ThrowCachedExceptionIfAny(T key)
        {
            if (_exceptionsCache.IsValueCreated &&
                _exceptionsCache.Value.TryGetValue(key, out var ex))
            {
                throw ex;
            }
        }

        public void TryRemove(T key)
        {
            _exceptionsCache.Value.TryRemove(key, out var _);
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

        //internal static string? ExtractMessage(string msg)
        //{
        //    string ret = null;
        //    try
        //    {
        //        using JsonDocument doc = JsonDocument.Parse(msg);
        //        JsonElement root = doc.RootElement;

        //        // Helper method to get property value safely
        //        static JsonElement GetPropertyValue(JsonElement element, string propertyName)
        //        {
        //            element.TryGetProperty(propertyName, out JsonElement value);
        //            return value;
        //        }

        //        // Extract the main error message
        //        string title = GetPropertyValue(root, "title").ToString();
        //        if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "message").ToString();
        //        if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "Message").ToString();
        //        if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "errorMessage").ToString();
        //        if (string.IsNullOrEmpty(title)) title = GetPropertyValue(root, "error").ToString();

        //        // Extract specific errors
        //        List<string> errorMessages = [];
        //        if (root.TryGetProperty("errors", out JsonElement errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
        //        {
        //            foreach (JsonProperty errorProperty in errorsElement.EnumerateObject())
        //            {
        //                string propertyName = errorProperty.Name;
        //                JsonElement errorMessagesArray = errorProperty.Value;

        //                if (errorMessagesArray.ValueKind == JsonValueKind.Array)
        //                {
        //                    foreach (JsonElement errorMessage in errorMessagesArray.EnumerateArray())
        //                    {
        //                        errorMessages.Add($"{propertyName}: {errorMessage.ToString()}");
        //                    }
        //                }
        //            }
        //        }

        //        // Combine the main message and the specific errors
        //        ret = string.Join(' ', new[] { title }.Concat(errorMessages).Distinct());
        //    }
        //    catch { }

        //    if (!string.IsNullOrEmpty(ret))
        //    {
        //        return ret;
        //    }

        //    // Return original message if parsing fails
        //    return msg;
        //}

        internal static string? ExtractMessage(string msg)
        {
            string ret = null;
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
            catch { }

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

        private static string CreateExceptionMessage(string target, string? message)
        {
            return "\"" + target + "\": " + message;
        }

        private static string CreateExceptionMessage(string target, Exception ex)
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

        public OrchException(string target, string message)
            : base(CreateExceptionMessage(target, message))
        {
        }

        public OrchException(string target, Exception ex)
            : base(CreateExceptionMessage(target, ex), ex)
        {
        }

        public OrchException(string target, string message, Exception ex)
            : base(CreateExceptionMessage(target, message, ex), ex)
        {
        }
    }
}
