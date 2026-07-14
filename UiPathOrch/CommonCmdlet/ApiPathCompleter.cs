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
                ApiEndpointCatalog.TipHelp(endpoint));
        }
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
