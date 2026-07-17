using System.Reflection;
using System.Text.RegularExpressions;

namespace UiPath.PowerShell.Core;

/// <summary>
/// Which base URL an endpoint hangs off — i.e. which Invoke-OrchApi switch reaches it.
/// </summary>
internal enum ApiEndpointBase
{
    /// Orchestrator base URL — Invoke-OrchApi with no switch.
    Orchestrator,
    /// Identity base URL — Invoke-OrchApi -Identity.
    Identity,
    /// Portal base URL — Invoke-OrchApi -Portal.
    Portal,
    /// Another service behind the same gateway (Test Manager, Document Understanding,
    /// AI Center). The path carries its own service prefix (/testmanager_, /du_, ...)
    /// and is relative to the TENANT ROOT, not to the Orchestrator base — the two
    /// differ on Automation Suite, where the Orchestrator base gains "/orchestrator_".
    Service,
}

/// <summary>
/// Which project-scoped service an endpoint belongs to. A {projectId} is only meaningful within
/// its own service: a Test Manager project id in a /du_ path addresses nothing.
/// </summary>
internal enum ApiService
{
    /// Not project-scoped (Orchestrator, Identity, Portal, AI Center).
    None,
    /// Document Understanding — /du_, projects live on a UiPathOrchDu drive.
    DocumentUnderstanding,
    /// Test Manager — /testmanager_, projects live on a UiPathOrchTm drive.
    TestManager,
}

/// <summary>
/// One known API endpoint: a path template plus the metadata the -Uri completer needs to
/// decide whether to offer it and what to show in the tooltip.
///
/// MinVersion/MaxVersion are the Orchestrator Web API version range the path was seen in
/// across the vNN swagger documents (0/0 = not version-tagged, i.e. Identity, Portal and
/// the other services, which expose no comparable version signal).
/// </summary>
internal sealed record ApiEndpoint(
    ApiEndpointBase Base,
    string Path,
    string Methods,
    int MinVersion,
    int MaxVersion,
    string Summary)
{
    internal bool HasPartitionPlaceholder =>
        Path.Contains(ApiEndpointCatalog.PartitionPlaceholder, StringComparison.Ordinal);

    /// True when the path is scoped by a project id it cannot run without.
    internal bool RequiresProject =>
        Path.Contains(ApiEndpointCatalog.ProjectPlaceholder, StringComparison.Ordinal);

    internal ApiService Service => ApiEndpointCatalog.ServiceOf(Path);
}

/// <summary>
/// The known-endpoint catalog behind Invoke-OrchApi's -Uri tab completion.
///
/// The data lives in the embedded Resources\ApiEndpoints.txt, generated from UiPath's
/// swagger corpus by Tools\Update-ApiEndpointCatalog.ps1 (Orchestrator v11..v20 unioned
/// and version-tagged, Identity, Test Manager, Document Understanding, AI Center; Portal
/// is harvested from OrchAPISession.cs, which has no swagger document). Parsing is lazy —
/// the file is only read on the first &lt;Tab&gt;, never at module load.
///
/// Everything here is pure and side-effect free (no session, no I/O beyond the one-time
/// resource read) so the completion rules are unit-testable — see ApiEndpointCatalogTests.
/// </summary>
internal static class ApiEndpointCatalog
{
    // The placeholders the drive context can fill. Both are spelled exactly as the swagger
    // documents name their path parameters, so the generated catalog carries them verbatim.
    internal const string PartitionPlaceholder = "{partitionGlobalId}";
    internal const string ProjectPlaceholder = "{projectId}";

    internal const string ResourceName = "UiPathOrch.Resources.ApiEndpoints.txt";

    private static readonly Lazy<ApiEndpoint[]> _all = new(LoadFromResource, isThreadSafe: true);

    internal static ApiEndpoint[] All => _all.Value;

    // The newest Orchestrator Web API version the catalog knows about. An endpoint still
    // present in that newest document has no known removal point, so it must stay visible
    // on a tenant NEWER than the catalog (see MatchesVersion).
    private static readonly Lazy<int> _newestVersion = new(
        () => All.Length == 0 ? 0 : All.Max(e => e.MaxVersion), isThreadSafe: true);

    internal static int NewestVersion => _newestVersion.Value;

    private static ApiEndpoint[] LoadFromResource()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null) return [];
        using var reader = new StreamReader(stream);
        return Parse(reader).ToArray();
    }

    /// <summary>
    /// Parses the catalog's tab-separated lines:
    ///   base TAB path TAB methods TAB versions TAB summary
    /// Blank lines and '#' comments are skipped, as is any line that doesn't carry at least
    /// a base and a path. Malformed rows are dropped rather than thrown on: a corrupt catalog
    /// must degrade to fewer completions, never to an error in the user's prompt.
    /// </summary>
    internal static IEnumerable<ApiEndpoint> Parse(TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0 || line[0] == '#') continue;

            var f = line.Split('\t');
            if (f.Length < 2) continue;

            ApiEndpointBase? b = f[0] switch
            {
                "O" => ApiEndpointBase.Orchestrator,
                "I" => ApiEndpointBase.Identity,
                "P" => ApiEndpointBase.Portal,
                "S" => ApiEndpointBase.Service,
                _ => null,
            };
            if (b is null) continue;

            string path = f[1];
            if (path.Length == 0) continue;

            string methods = f.Length > 2 ? f[2] : "";
            var (min, max) = ParseVersionRange(f.Length > 3 ? f[3] : "");
            string summary = f.Length > 4 ? f[4] : "";

            yield return new ApiEndpoint(b.Value, path, methods, min, max, summary);
        }
    }

    /// Parses "11-20" / "20" / "" into (min, max). Unparseable => (0, 0) = untagged.
    internal static (int Min, int Max) ParseVersionRange(string range)
    {
        if (string.IsNullOrWhiteSpace(range)) return (0, 0);

        int dash = range.IndexOf('-');
        if (dash < 0)
        {
            return int.TryParse(range, out int single) ? (single, single) : (0, 0);
        }

        return int.TryParse(range[..dash], out int min) && int.TryParse(range[(dash + 1)..], out int max)
            ? (min, max)
            : (0, 0);
    }

    /// <summary>
    /// Should this endpoint be offered on a tenant whose Web API version is
    /// <paramref name="apiVersion"/> (the api-supported-versions header, null when the drive
    /// hasn't made an API call yet)?
    ///
    /// Untagged endpoints and an unknown version both mean "no basis to exclude" — offer it.
    /// Otherwise the endpoint must have existed by that version (min &lt;= v) and must not have
    /// been dropped before it. The upper bound is deliberately NOT applied once the endpoint
    /// survives into the newest document the catalog knows: a tenant on a version newer than
    /// the catalog (v21 against a v20 corpus) must still see every current endpoint, while one
    /// that was genuinely removed along the way (max = 17) stays hidden.
    /// </summary>
    internal static bool MatchesVersion(ApiEndpoint endpoint, double? apiVersion)
    {
        if (endpoint.MinVersion == 0 || apiVersion is not double v) return true;

        if (v < endpoint.MinVersion) return false;

        return endpoint.MaxVersion >= NewestVersion || v <= endpoint.MaxVersion;
    }

    /// Does the endpoint serve <paramref name="method"/>? An unknown/blank method list is not
    /// grounds to hide an endpoint — the catalog, not the user, is the incomplete side there.
    internal static bool MatchesMethod(ApiEndpoint endpoint, string? method)
    {
        if (string.IsNullOrEmpty(method) || endpoint.Methods.Length == 0) return true;

        foreach (var m in endpoint.Methods.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(m.Trim(), method, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// The bases reachable with the given Invoke-OrchApi switches. Orchestrator and Service
    /// share the no-switch form (both are resolved off the drive's own host); -Identity and
    /// -Portal each select exactly one base.
    internal static bool MatchesSwitches(ApiEndpoint endpoint, bool identity, bool portal) => endpoint.Base switch
    {
        ApiEndpointBase.Identity => identity,
        ApiEndpointBase.Portal => portal,
        _ => !identity && !portal,
    };

    /// The project-scoped service a path belongs to, read off its gateway prefix.
    internal static ApiService ServiceOf(string path)
    {
        if (path.Contains("/testmanager_/", StringComparison.OrdinalIgnoreCase)) return ApiService.TestManager;
        if (path.Contains("/du_/", StringComparison.OrdinalIgnoreCase)) return ApiService.DocumentUnderstanding;
        return ApiService.None;
    }

    /// <summary>
    /// Substitutes the ids the drive context knows into a path template: the partition global id,
    /// and the DU/TM project id.
    ///
    /// The project id is only substituted into a path of the SERVICE the project belongs to. A
    /// location on a Test Manager drive says nothing about which Document Understanding project is
    /// meant, and pasting a TM project id into a /du_ path would silently address nothing.
    ///
    /// An id the context could not supply is left as its {placeholder}. That is deliberate: the
    /// endpoint stays discoverable, the tooltip says which -Path would fill it (see
    /// ApiPathCompleter), and it beats blocking a &lt;Tab&gt; keypress on an API round-trip.
    /// </summary>
    internal static string FillPlaceholders(string path, ApiContext? context)
        => FillPlaceholders(
            path,
            context?.PartitionGlobalId,
            context is not null && context.ProjectKind == ServiceOf(path) ? context.ProjectId : null);

    internal static string FillPlaceholders(string path, string? partitionGlobalId, string? projectId)
    {
        if (!string.IsNullOrEmpty(partitionGlobalId))
            path = path.Replace(PartitionPlaceholder, partitionGlobalId, StringComparison.Ordinal);

        if (!string.IsNullOrEmpty(projectId))
            path = path.Replace(ProjectPlaceholder, projectId, StringComparison.Ordinal);

        return path;
    }

    // A path-template placeholder: "{" then one or more non-brace characters then "}". Every
    // swagger path parameter this catalog carries is identifier-like ({projectId}, {token},
    // {id}, {objectType}, ...), so a permissive [^{}] body matches them all without straying
    // across segment boundaries.
    private static readonly Regex PlaceholderPattern = new(@"\{[^{}]+\}", RegexOptions.Compiled);

    /// <summary>
    /// The {placeholder} tokens still present in a path — those neither FillPlaceholders nor the
    /// drive context could fill. The completer flags them in the tooltip; the cmdlet refuses a
    /// request whose URI still carries one, because a literal "{token}" / "{id}" would only come
    /// back a 404. Distinct, in first-seen order.
    /// </summary>
    internal static IReadOnlyList<string> UnresolvedPlaceholders(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.IndexOf('{') < 0) return [];

        var found = new List<string>();
        foreach (Match m in PlaceholderPattern.Matches(path))
        {
            if (!found.Contains(m.Value, StringComparer.Ordinal)) found.Add(m.Value);
        }
        return found;
    }

    /// The tooltip shown beside a completion: the methods, then the version range when the
    /// endpoint is version-tagged, then the swagger summary.
    internal static string TipHelp(ApiEndpoint e)
    {
        var parts = new List<string>(3);
        if (e.Methods.Length > 0) parts.Add(e.Methods.Replace(",", " "));

        if (e.MinVersion > 0)
        {
            parts.Add(e.MinVersion == e.MaxVersion
                ? $"v{e.MinVersion}"
                : e.MaxVersion >= NewestVersion ? $"v{e.MinVersion}+" : $"v{e.MinVersion}-v{e.MaxVersion}");
        }

        if (e.Summary.Length > 0) parts.Add(e.Summary);

        return string.Join("  ", parts);
    }
}
