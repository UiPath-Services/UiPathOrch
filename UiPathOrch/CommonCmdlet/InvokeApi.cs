using System.Collections;
using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Invokes an arbitrary Orchestrator (or Identity / Portal) API endpoint using the
/// authentication and folder context bound to a UiPathOrch PSDrive. Modeled as a superset
/// of Invoke-RestMethod for the parameters that make sense in this context; auth, proxy,
/// and certificate parameters are intentionally omitted because the drive owns them.
///
/// JSON responses are converted to PSObject (PSCustomObject) so they integrate with
/// Format-Table / Select-Object / Sort-Object. For OData collection responses
/// (`{ "value": [...] }`), the wrapper is unwrapped and each item is emitted with a
/// `Path` property indicating the originating drive context.
/// </summary>
[Cmdlet(VerbsLifecycle.Invoke, "OrchApi", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(PSObject), typeof(string))]
public class InvokeOrchApiCmdlet : OrchestratorPSCmdlet
{
    // PSTypeName attached to every JSON record we emit. Used by Format.ps1xml to
    // group results by Path. See UiPathOrch.Format.ps1xml.
    internal const string OutputTypeName = "UiPathOrch.ApiResponseItem";

    [Parameter(Mandatory = true, Position = 0)]
    [Alias("Uri")]
    [ArgumentCompleter(typeof(ApiPathCompleter))]
    public string ApiPath { get; set; } = null!;

    [Parameter]
    public string Method { get; set; } = "GET";

    [Parameter]
    public object? Body { get; set; }

    [Parameter]
    public string ContentType { get; set; } = "application/json";

    [Parameter]
    public IDictionary? Headers { get; set; }

    [Parameter]
    public string? OutFile { get; set; }

    [Parameter]
    public string? InFile { get; set; }

    [Parameter]
    public string? StatusCodeVariable { get; set; }

    [Parameter]
    public string? ResponseHeadersVariable { get; set; }

    [Parameter]
    public SwitchParameter SkipHttpErrorCheck { get; set; }

    [Parameter]
    public SwitchParameter Identity { get; set; }

    [Parameter]
    public SwitchParameter Portal { get; set; }

    [Parameter]
    public SwitchParameter SkipFolderContext { get; set; }

    [Parameter]
    public SwitchParameter Raw { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        if (Identity.IsPresent && Portal.IsPresent)
        {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Specify only one of -Identity or -Portal."),
                "InvokeOrchApiSwitchConflict", ErrorCategory.InvalidArgument, null));
            return;
        }

        if (Body is not null && InFile is not null)
        {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Specify only one of -Body or -InFile."),
                "InvokeOrchApiBodyConflict", ErrorCategory.InvalidArgument, null));
            return;
        }

        // Resolve the drive context: the UiPathOrch drive named by -Path or, with no -Path, the
        // current location. A DU or TM drive resolves too — it is a view onto the same tenant, so
        // the call runs against its parent Orchestrator drive's auth and base URLs, and the project
        // the location points at becomes the {projectId} of the DU/TM endpoints (see ApiContext).
        var effectivePath = EffectivePath(Path, LiteralPath);
        string resolvePath = string.IsNullOrEmpty(effectivePath)
            ? SessionState.Path.CurrentLocation.Path
            : effectivePath;

        ApiContext? context;
        try
        {
            context = ApiContextResolver.Resolve(SessionState, effectivePath, allowFetch: true);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "InvokeOrchApiResolvePath",
                ErrorCategory.ObjectNotFound, resolvePath));
            return;
        }

        if (context is null)
        {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException(
                    $"'{resolvePath}' is not on a UiPathOrch, DU or TM drive. " +
                    "Pass -Path explicitly or cd into one of those drives."),
                "InvokeOrchApiNoPath", ErrorCategory.InvalidOperation, null));
            return;
        }

        OrchDriveInfo drive = context.Drive;
        Folder? folder = context.Folder;

        // Fill the ids the context knows ({partitionGlobalId}, {projectId}). Tab completion already
        // emits them substituted, so this is for the paths that don't come from it — a template
        // pasted from the swagger docs, or one written into a script. A project id is only ever
        // substituted into a path of ITS OWN service (see ApiEndpointCatalog.FillPlaceholders).
        string apiPath = ApiEndpointCatalog.FillPlaceholders(ApiPath, context);

        // A {projectId} the context couldn't supply would go out as a literal and come back a 404.
        // Say which -Path would have filled it instead — the same thing the completer's tooltip says.
        if (apiPath.Contains(ApiEndpointCatalog.ProjectPlaceholder, StringComparison.Ordinal))
        {
            var service = ApiEndpointCatalog.ServiceOf(apiPath);
            string label = ApiContextResolver.ServiceLabel(service);
            string? shadowDrive = ApiContextResolver.ShadowDriveNameFor(SessionState, drive, service);

            string remedy = shadowDrive is not null
                ? $"cd {shadowDrive}:{System.IO.Path.DirectorySeparatorChar}<project>, or pass -Path {shadowDrive}:{System.IO.Path.DirectorySeparatorChar}<project>."
                : label.Length > 0
                    ? $"This tenant has no {label} drive mounted."
                    : "Substitute it yourself, or run from the drive that scopes it.";

            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException(
                    $"'{ApiPath}' is scoped by a {(label.Length > 0 ? label + " " : "")}project, and " +
                    $"'{context.ContextPath}' does not name one, so {ApiEndpointCatalog.ProjectPlaceholder} " +
                    $"could not be filled. {remedy}"),
                "InvokeOrchApiUnresolvedProjectId", ErrorCategory.InvalidArgument, ApiPath));
            return;
        }

        // Any other placeholder the context can't fill ({token}, {id}, {objectType}, ...) would go
        // out as a literal and come back a 404. The drive only knows {partitionGlobalId} and
        // {projectId} (handled above); everything else is a value only the caller has, so refuse
        // with the list to fill rather than send a request that cannot succeed. This also catches a
        // template pasted from the swagger docs whose ids were never substituted.
        var unresolved = ApiEndpointCatalog.UnresolvedPlaceholders(apiPath);
        if (unresolved.Count > 0)
        {
            string list = string.Join(", ", unresolved);
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException(
                    $"'{ApiPath}' still contains {list}, which must be filled in before the request is sent. " +
                    $"The drive fills {ApiEndpointCatalog.PartitionPlaceholder} and {ApiEndpointCatalog.ProjectPlaceholder} " +
                    "from its context; any other placeholder is a value only you have (a connector token, a resource " +
                    $"id, ...). Replace {list} with the actual value and run again."),
                "InvokeOrchApiUnresolvedPlaceholder", ErrorCategory.InvalidArgument, ApiPath));
            return;
        }

        string baseUrl;
        if (Identity.IsPresent) baseUrl = drive.OrchAPISession._base_url_identity;
        else if (Portal.IsPresent) baseUrl = drive.OrchAPISession._base_url_portal;
        else baseUrl = drive.OrchAPISession._base_url_orchestrator;

        string url;
        if (apiPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            apiPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // An absolute URL is accepted only if it points at the same origin
            // (scheme + host + port) the drive already talks to — its Orchestrator,
            // Identity, or Portal base URL. Otherwise the drive's live bearer token
            // would be sent to an arbitrary (and possibly http-downgraded) host.
            if (!Uri.TryCreate(apiPath, UriKind.Absolute, out var absUri))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException($"'{apiPath}' is not a valid absolute URI."),
                    "InvokeOrchApiInvalidUri", ErrorCategory.InvalidArgument, apiPath));
                return;
            }
            var session = drive.OrchAPISession;
            bool sameOrigin = IsKnownOrigin(absUri, session._base_url_orchestrator)
                           || IsKnownOrigin(absUri, session._base_url_identity)
                           || IsKnownOrigin(absUri, session._base_url_portal);
            if (!sameOrigin)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException(
                        $"Absolute URL origin '{absUri.Scheme}://{absUri.Authority}' does not match the drive's " +
                        "Orchestrator, Identity, or Portal origin. The drive's access token is only sent to its own " +
                        "deployment; pass a relative API path (e.g. 'odata/Releases'), or use -Identity / -Portal."),
                    "InvokeOrchApiCrossHostUri", ErrorCategory.InvalidArgument, apiPath));
                return;
            }
            url = apiPath;
        }
        else
        {
            url = baseUrl.TrimEnd('/') +
                  (apiPath.StartsWith('/') ? apiPath : "/" + apiPath);
        }

        var httpMethod = new HttpMethod(Method.ToUpperInvariant());
        using var request = new HttpRequestMessage(httpMethod, url);

        // Folder context: only on the Orchestrator base URL. Identity/Portal endpoints don't
        // accept it, and the root folder has no Id. A DU/TM drive has no folder at all
        // (context.Folder is null there), so the header is simply not sent.
        bool injectFolder = !Identity.IsPresent && !Portal.IsPresent && !SkipFolderContext.IsPresent;
        if (injectFolder && folder?.Id is long folderId && folderId != 0)
        {
            request.Headers.Add("X-UIPATH-OrganizationUnitId", folderId.ToString());
        }

        if (InFile is not null)
        {
            string resolved = SessionState.Path.GetUnresolvedProviderPathFromPSPath(InFile);
            byte[] bytes = File.ReadAllBytes(resolved);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                string.IsNullOrEmpty(ContentType) ? "application/octet-stream" : ContentType);
            request.Content = content;
        }
        else if (Body is not null)
        {
            request.Content = BuildBodyContent(Body, ContentType);
        }

        // Apply -Headers AFTER content is built so HTTP content headers
        // (Content-Type, Content-Disposition, etc.) can be routed to
        // request.Content.Headers. Adding them to request.Headers silently
        // fails (HttpClient rejects content headers there with TryAddWithout-
        // Validation reporting false), which used to make caller-supplied
        // Content-Type effectively a no-op.
        if (Headers is not null)
        {
            foreach (DictionaryEntry kv in Headers)
            {
                if (kv.Key is null) continue;
                string key = kv.Key.ToString()!;
                // Drive owns auth — don't let callers override it.
                if (string.Equals(key, "Authorization", StringComparison.OrdinalIgnoreCase)) continue;
                string value = kv.Value?.ToString() ?? "";

                if (ContentHeaderNames.Contains(key))
                {
                    if (request.Content is not null)
                    {
                        // Remove any default already set (e.g., Content-Type
                        // from -ContentType / BuildBodyContent) so the caller
                        // value wins — explicit -Headers overrides the default.
                        request.Content.Headers.Remove(key);
                        request.Content.Headers.TryAddWithoutValidation(key, value);
                    }
                    // else: no body → silently ignore. GET/HEAD with content
                    // headers in -Headers has no meaningful target.
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }
        }

        // ShouldProcess for non-idempotent / non-read methods. Bypassable via -Confirm:$false.
        if (httpMethod != HttpMethod.Get && httpMethod != HttpMethod.Head)
        {
            string target = $"{httpMethod.Method} {url}";
            if (!ShouldProcess(target, "Invoke OrchApi"))
            {
                return;
            }
        }

        HttpResponseMessage response;
        try
        {
            response = drive.OrchAPISession.SendApiRequest(request);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "InvokeOrchApiSendError",
                ErrorCategory.ConnectionError, url));
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(StatusCodeVariable))
            {
                SessionState.PSVariable.Set(StatusCodeVariable, (int)response.StatusCode);
            }

            if (!string.IsNullOrEmpty(ResponseHeadersVariable))
            {
                var hdr = new Hashtable(StringComparer.OrdinalIgnoreCase);
                foreach (var h in response.Headers)
                {
                    hdr[h.Key] = string.Join(",", h.Value);
                }
                if (response.Content?.Headers is not null)
                {
                    foreach (var h in response.Content.Headers)
                    {
                        hdr[h.Key] = string.Join(",", h.Value);
                    }
                }
                SessionState.PSVariable.Set(ResponseHeadersVariable, hdr);
            }

            bool isHttpError = !response.IsSuccessStatusCode && !SkipHttpErrorCheck.IsPresent;

            // Binary download takes precedence — never deserialize, never inject Path.
            if (!string.IsNullOrEmpty(OutFile))
            {
                if (isHttpError)
                {
                    string err = ReadBodyCapped(response.Content, ErrorBodyMaxBytes);
                    WriteError(BuildHttpErrorRecord(response, err, url));
                    return;
                }
                string outPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(OutFile);
                using var stream = response.Content!.ReadAsStream();
                using var file = File.Create(outPath);
                stream.CopyTo(file);
                return;
            }

            // For non-success responses we don't need the full body — just enough to surface
            // the upstream error message. Avoid loading multi-MB HTML error pages.
            string body = isHttpError
                ? ReadBodyCapped(response.Content, ErrorBodyMaxBytes)
                : response.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";

            if (isHttpError)
            {
                WriteError(BuildHttpErrorRecord(response, body, url));
                return;
            }

            if (string.IsNullOrEmpty(body)) return;

            if (Raw.IsPresent)
            {
                WriteObject(body);
                return;
            }

            JsonDocument? doc;
            try
            {
                doc = JsonDocument.Parse(body);
            }
            catch
            {
                // Not JSON — surface as raw text rather than silently swallowing.
                WriteObject(body);
                return;
            }

            using (doc)
            {
                object? converted = ConvertJson(doc.RootElement);
                EmitWithPathInjection(converted, context.ContextPath);
            }
        }
        finally
        {
            response.Dispose();
        }
    }

    // ----- helpers -----

    // True if the candidate absolute URL shares scheme + host + port with baseUrl,
    // i.e. it targets the same origin the drive is already authenticated against.
    // internal (not private) so the cross-host token-leak guard can be unit-tested
    // directly — see InvokeOriginGuardTests.
    internal static bool IsKnownOrigin(Uri candidate, string? baseUrl)
    {
        return !string.IsNullOrEmpty(baseUrl)
            && Uri.TryCreate(baseUrl, UriKind.Absolute, out var b)
            && string.Equals(candidate.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.Host, b.Host, StringComparison.OrdinalIgnoreCase)
            && candidate.Port == b.Port;
    }

    internal static HttpContent BuildBodyContent(object body, string contentType)
    {
        // A caller can hand us a [string] that PowerShell wrapped in a live PSObject
        // (e.g. some ConvertTo-Json pipeline shapes). Unwrap to the BaseObject first so
        // a wrapped JSON string is recognized as a string below, instead of falling into
        // JsonSerializer.Serialize(PSObject) — which throws a System.Text.Json
        // object-cycle exception.
        if (body is PSObject pso) body = pso.BaseObject;

        // A direct byte[] body bypasses serialization (for blobs etc.).
        if (body is byte[] bytes)
        {
            var c = new ByteArrayContent(bytes);
            c.Headers.ContentType = MediaTypeHeaderValue.Parse(
                string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
            return c;
        }

        string payload = body is string s
            ? s
            : JsonSerializer.Serialize(body, JsonTools.jsoWhenWritingNull);

        return new StringContent(payload, Encoding.UTF8,
            string.IsNullOrEmpty(contentType) ? "application/json" : contentType);
    }

    private const int ErrorBodyMaxBytes = 8192;

    // HTTP entity-body headers — these belong on HttpContent.Headers, not
    // HttpRequestMessage.Headers. List per RFC 7231 §3.1 (representation
    // metadata) plus a couple of payload-related headers HttpClient routes
    // the same way (Content-Disposition, Last-Modified, Expires, Allow).
    private static readonly HashSet<string> ContentHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Type", "Content-Length", "Content-Encoding", "Content-Language",
        "Content-Location", "Content-MD5", "Content-Range", "Content-Disposition",
        "Last-Modified", "Expires", "Allow",
    };

    private static string ReadBodyCapped(HttpContent? content, int maxBytes)
    {
        if (content is null) return "";
        try
        {
            using var stream = content.ReadAsStream();
            var buffer = new byte[maxBytes];
            int read = 0;
            int n;
            while (read < maxBytes && (n = stream.Read(buffer, read, maxBytes - read)) > 0)
            {
                read += n;
            }
            return Encoding.UTF8.GetString(buffer, 0, read);
        }
        catch
        {
            return "";
        }
    }

    private static ErrorRecord BuildHttpErrorRecord(HttpResponseMessage response, string body, string url)
    {
        // Trim aggressively — the response body can include short-lived tokens or PII when
        // the upstream emits debug detail. The OAuth2 error envelope we'd typically extract
        // for AuthManager doesn't apply to arbitrary endpoints, so just truncate.
        string snippet = body.Length <= 1024 ? body : body[..1024] + "…";
        var ex = new HttpRequestException(
            $"{(int)response.StatusCode} {response.ReasonPhrase}: {snippet}");
        return new ErrorRecord(ex, "InvokeOrchApiHttpError",
            ErrorCategory.InvalidResult, url);
    }

    private void EmitWithPathInjection(object? value, string contextPath)
    {
        if (value is null)
        {
            WriteObject(null);
            return;
        }

        // OData collection: { "value": [...], "@odata.context": ..., ... } — unwrap.
        if (TryGetODataValueArray(value, out var items))
        {
            foreach (var item in items!)
            {
                AttachPath(item, contextPath);
                WriteObject(item);
            }
            return;
        }

        // Bare array — emit each, attach Path per element.
        if (value is object?[] topArr)
        {
            foreach (var item in topArr)
            {
                AttachPath(item, contextPath);
                WriteObject(item);
            }
            return;
        }

        // Single object or scalar.
        AttachPath(value, contextPath);
        WriteObject(value);
    }

    private static bool TryGetODataValueArray(object? value, out object?[]? items)
    {
        items = null;
        if (value is PSObject pso && pso.Properties["value"]?.Value is object?[] arr)
        {
            items = arr;
            return true;
        }
        return false;
    }

    private static void AttachPath(object? item, string contextPath)
    {
        if (item is not PSObject pso) return;  // scalars and arrays don't carry Path

        if (pso.Properties["Path"] is null)
        {
            pso.Properties.Add(new PSNoteProperty("Path", contextPath));
        }
        else
        {
            pso.Properties["Path"].Value = contextPath;
        }

        if (!pso.TypeNames.Contains(OutputTypeName))
        {
            pso.TypeNames.Insert(0, OutputTypeName);
        }
    }

    private static object? ConvertJson(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                var pso = new PSObject();
                foreach (var p in el.EnumerateObject())
                {
                    pso.Properties.Add(new PSNoteProperty(p.Name, ConvertJson(p.Value)));
                }
                return pso;

            case JsonValueKind.Array:
                var len = el.GetArrayLength();
                var arr = new object?[len];
                int i = 0;
                foreach (var item in el.EnumerateArray())
                {
                    arr[i++] = ConvertJson(item);
                }
                return arr;

            case JsonValueKind.String:
                return el.GetString();

            case JsonValueKind.Number:
                if (el.TryGetInt64(out long lv)) return lv;
                if (el.TryGetDouble(out double dv)) return dv;
                return el.GetDecimal();

            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return null;
        }
    }
}
