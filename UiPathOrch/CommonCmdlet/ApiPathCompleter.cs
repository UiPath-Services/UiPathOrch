using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Completer;

/// <summary>
/// Completes Invoke-OrchApi's -Uri (-ApiPath) from the known-endpoint catalog
/// (see <see cref="ApiEndpointCatalog"/>).
///
/// Everything is resolved from the drive context the call will actually run in — the drive named
/// by -Path, or the current location, on a UiPathOrch, DU or TM drive alike (see
/// <see cref="ApiContextResolver"/>):
///
///  * the base:    -Identity / -Portal select their own endpoint sets; with neither switch the
///                 Orchestrator endpoints and the other same-host services (/testmanager_, /du_,
///                 /aifabric_) are offered.
///  * the version: endpoints are filtered against the tenant's ApiVersion — the
///                 api-supported-versions header, already learned from the first API response of
///                 the session — so an old on-prem tenant is never offered an endpoint that only
///                 exists on a newer Orchestrator. Until the drive has made its first call
///                 ApiVersion is null and everything is offered.
///  * the method:  when -Method is bound, only endpoints serving that verb are offered.
///  * the ids:     {partitionGlobalId} and, on a DU/TM drive, {projectId} are substituted from the
///                 context, so the completed URI is ready to send.
///
/// COMPLETERS MUST NOT DO EXPENSIVE OR BLOCKING WORK, so the context is resolved with
/// allowFetch: false — ids come only from what is already in memory. An id that isn't known yet
/// stays as its {placeholder} rather than sending a <Tab> keypress to the API for it.
/// </summary>
internal class ApiPathCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        bool identity = ResolveSwitchParameter(fakeBoundParameters, "Identity");
        bool portal = ResolveSwitchParameter(fakeBoundParameters, "Portal");
        if (identity && portal) yield break;   // the cmdlet rejects this combination

        string? method = GetFakeBoundParameter(fakeBoundParameters, "Method");

        // Invoke-OrchApi takes a single -Path (it resolves to one folder), so one context.
        string? path = GetFakeBoundParameter(fakeBoundParameters, "Path")
                    ?? GetFakeBoundParameter(fakeBoundParameters, "LiteralPath");

        var context = ApiContextResolver.Resolve(SessionState, path, allowFetch: false);
        if (context is null) yield break;   // not on an Orchestrator / DU / TM drive

        double? apiVersion = context.Drive.OrchAPISession.ApiVersion;
        bool automationSuite = context.Drive._psDrive.ResolvedEdition == OrchEdition.AutomationSuite;

        // Same wildcard convention as every other completer: a plain word is a prefix match, and an
        // explicit wildcard opts into substring search — with ~900 endpoints in the catalog,
        // `*du*`<Ctrl+Space> is the way to find one whose prefix you don't know.
        var wp = CreateWPFromWordToComplete(wordToComplete);

        // The DU / TM drives of this same tenant, named in the hint on a project-scoped endpoint
        // whose {projectId} this context can't fill. Resolved once, and only from the mounted
        // drives, so the hint names a drive that actually exists.
        string? duDrive = ApiContextResolver.ShadowDriveNameFor(SessionState, context.Drive, ApiService.DocumentUnderstanding);
        string? tmDrive = ApiContextResolver.ShadowDriveNameFor(SessionState, context.Drive, ApiService.TestManager);

        foreach (var endpoint in ApiEndpointCatalog.All)
        {
            if (!ApiEndpointCatalog.MatchesSwitches(endpoint, identity, portal)) continue;
            if (!ApiEndpointCatalog.MatchesVersion(endpoint, apiVersion)) continue;
            if (!ApiEndpointCatalog.MatchesMethod(endpoint, method)) continue;

            string uri = BuildUri(endpoint, context, automationSuite);
            if (!wp.IsMatch(uri)) continue;

            yield return new CompletionResult(
                PathTools.EscapePSText(uri),
                uri,
                CompletionResultType.ParameterValue,
                TipHelp(endpoint, context, duDrive, tmDrive));
        }
    }

    /// <summary>
    /// The tooltip: the endpoint's own (methods, version range, summary), plus — when the endpoint
    /// is project-scoped and this context could not fill its {projectId} — what to do about it.
    ///
    /// These endpoints are offered rather than hidden ON PURPOSE. From a UiPathOrch drive a
    /// /testmanager_/api/v2/{projectId}/... path cannot run as completed, but dropping it from the
    /// list hides the endpoint's very existence; naming the -Path that fills it turns a dead
    /// completion into the instruction for getting a live one.
    /// </summary>
    internal static string TipHelp(ApiEndpoint endpoint, ApiContext context, string? duDrive, string? tmDrive)
    {
        string tip = ApiEndpointCatalog.TipHelp(endpoint);

        string? hint = ProjectHint(endpoint, context, duDrive, tmDrive);
        return hint is null ? tip : $"{tip}  <-- {hint}";
    }

    /// Null when the endpoint needs no project, or when this context already filled it.
    internal static string? ProjectHint(ApiEndpoint endpoint, ApiContext context, string? duDrive, string? tmDrive)
    {
        if (!endpoint.RequiresProject) return null;

        // Filled: the context's project belongs to this endpoint's service.
        if (context.ProjectKind == endpoint.Service && !string.IsNullOrEmpty(context.ProjectId)) return null;

        string label = ApiContextResolver.ServiceLabel(endpoint.Service);
        if (label.Length == 0) return null;   // {projectId} on a service we don't map to a drive

        string? driveName = endpoint.Service == ApiService.TestManager ? tmDrive : duDrive;

        return driveName is null
            ? $"{{projectId}}: needs a {label} project; this tenant has no {label} drive mounted"
            : $"{{projectId}}: fills in from a {label} project — cd {driveName}:\\<project>, or pass -Path {driveName}:\\<project>";
    }

    /// <summary>
    /// The URI to insert for an endpoint in a given context, with the context's ids substituted.
    ///
    /// Orchestrator / Identity / Portal endpoints go in as relative paths — Invoke-OrchApi resolves
    /// each against the matching base URL. The other services are relative to the TENANT ROOT, which
    /// equals the Orchestrator base everywhere EXCEPT Automation Suite, where the Orchestrator base
    /// carries an extra "/orchestrator_" segment that would mangle a /testmanager_ path. There they
    /// are emitted as an absolute URL off the tenant root, which Invoke-OrchApi accepts because it
    /// is the drive's own origin (see InvokeOrchApiCmdlet.IsKnownOrigin).
    /// </summary>
    internal static string BuildUri(ApiEndpoint endpoint, ApiContext context, bool automationSuite)
    {
        string path = ApiEndpointCatalog.FillPlaceholders(endpoint.Path, context);

        if (endpoint.Base == ApiEndpointBase.Service && automationSuite)
        {
            return context.Drive.OrchAPISession._base_url.TrimEnd('/') + path;
        }

        return path;
    }
}
