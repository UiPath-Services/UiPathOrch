using System.IO;
using System.Linq;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Pins the rules behind Invoke-OrchApi's -Uri completion: how the embedded endpoint catalog
// (Resources\ApiEndpoints.txt, generated from the swagger corpus by
// Tools\Update-ApiEndpointCatalog.ps1) is parsed, and how a candidate is filtered and rendered.
//
// The completer itself needs a live drive, so these exercise the pure decision functions it is
// built out of. CompleterExceptionSafetyTests separately guarantees the completer never throws.
public class ApiEndpointCatalogTests
{
    private static ApiEndpoint[] ParseText(string text)
        => ApiEndpointCatalog.Parse(new StringReader(text)).ToArray();

    // ----- parsing -----

    [Fact]
    public void Parses_all_five_fields()
    {
        var e = Assert.Single(ParseText("O\t/odata/Assets\tGET,POST\t11-20\tGet Assets"));

        Assert.Equal(ApiEndpointBase.Orchestrator, e.Base);
        Assert.Equal("/odata/Assets", e.Path);
        Assert.Equal("GET,POST", e.Methods);
        Assert.Equal(11, e.MinVersion);
        Assert.Equal(20, e.MaxVersion);
        Assert.Equal("Get Assets", e.Summary);
    }

    // The base codes are spelled as strings, not as the (internal) enum, because a public
    // xUnit signature may not expose an internal type.
    [Theory]
    [InlineData("O", nameof(ApiEndpointBase.Orchestrator))]
    [InlineData("I", nameof(ApiEndpointBase.Identity))]
    [InlineData("P", nameof(ApiEndpointBase.Portal))]
    [InlineData("S", nameof(ApiEndpointBase.Service))]
    public void Maps_every_base_code(string code, string expected)
        => Assert.Equal(expected, Assert.Single(ParseText($"{code}\t/x\tGET\t\t")).Base.ToString());

    [Theory]
    [InlineData("")]                       // blank
    [InlineData("# a comment")]            // comment
    [InlineData("X\t/x\tGET\t\t")]         // unknown base code
    [InlineData("O")]                      // no path field
    [InlineData("O\t\tGET\t\t")]           // empty path
    public void Drops_malformed_rows_instead_of_throwing(string line)
        => Assert.Empty(ParseText(line));

    [Fact]
    public void Tolerates_rows_missing_the_trailing_optional_fields()
    {
        var e = Assert.Single(ParseText("P\t/api/auditLog/{partitionGlobalId}"));

        Assert.Equal("", e.Methods);
        Assert.Equal(0, e.MinVersion);
        Assert.Equal("", e.Summary);
    }

    [Theory]
    [InlineData("11-20", 11, 20)]
    [InlineData("20", 20, 20)]
    [InlineData("", 0, 0)]
    [InlineData("   ", 0, 0)]
    [InlineData("garbage", 0, 0)]
    [InlineData("11-", 0, 0)]
    public void Parses_version_ranges(string range, int min, int max)
        => Assert.Equal((min, max), ApiEndpointCatalog.ParseVersionRange(range));

    // ----- version filtering -----
    //
    // The catalog's newest Orchestrator document is the ceiling; an endpoint that survives into
    // it has no known removal point. NewestVersion comes from the real embedded catalog, so the
    // "still current" cases are written against it rather than a hard-coded 20.

    private static ApiEndpoint Tagged(int min, int max)
        => new(ApiEndpointBase.Orchestrator, "/odata/X", "GET", min, max, "");

    [Fact]
    public void Offers_untagged_endpoints_at_any_version()
    {
        var untagged = new ApiEndpoint(ApiEndpointBase.Identity, "/api/Group", "GET", 0, 0, "");

        Assert.True(ApiEndpointCatalog.MatchesVersion(untagged, 11.0));
        Assert.True(ApiEndpointCatalog.MatchesVersion(untagged, null));
    }

    [Fact]
    public void Offers_everything_when_the_drive_version_is_unknown()
    {
        // A drive that hasn't made an API call yet has no api-supported-versions header to go on.
        // Guessing would hide real endpoints, so nothing is excluded.
        Assert.True(ApiEndpointCatalog.MatchesVersion(Tagged(18, 20), null));
        Assert.True(ApiEndpointCatalog.MatchesVersion(Tagged(11, 11), null));
    }

    [Fact]
    public void Hides_endpoints_introduced_after_the_tenants_version()
    {
        Assert.False(ApiEndpointCatalog.MatchesVersion(Tagged(18, 20), 11.0));
        Assert.True(ApiEndpointCatalog.MatchesVersion(Tagged(18, 20), 18.0));
        Assert.True(ApiEndpointCatalog.MatchesVersion(Tagged(18, 20), 19.0));
    }

    [Fact]
    public void Hides_endpoints_removed_before_the_tenants_version()
    {
        // Present in v11 only — a v20 tenant must not be offered it.
        Assert.True(ApiEndpointCatalog.MatchesVersion(Tagged(11, 11), 11.0));
        Assert.False(ApiEndpointCatalog.MatchesVersion(Tagged(11, 11), 20.0));
    }

    [Fact]
    public void Still_offers_current_endpoints_on_a_tenant_newer_than_the_catalog()
    {
        int newest = ApiEndpointCatalog.NewestVersion;
        Assert.True(newest > 0);

        // Reaches the newest document we know: no known removal point, so a tenant on the NEXT
        // version must still see it. Without this the catalog would go silent the day
        // Orchestrator ships a version we haven't harvested.
        Assert.True(ApiEndpointCatalog.MatchesVersion(Tagged(11, newest), newest + 1.0));

        // But one genuinely dropped along the way stays hidden.
        Assert.False(ApiEndpointCatalog.MatchesVersion(Tagged(11, newest - 2), newest + 1.0));
    }

    // ----- method / switch filtering -----

    [Theory]
    [InlineData("GET,POST", "GET", true)]
    [InlineData("GET,POST", "post", true)]    // case-insensitive
    [InlineData("GET,POST", "DELETE", false)]
    [InlineData("GET,POST", null, true)]      // -Method unbound: don't filter
    [InlineData("GET,POST", "", true)]
    [InlineData("", "DELETE", true)]          // unknown method list: the catalog is the gap, not the user
    public void Filters_by_method_only_when_bound(string methods, string? method, bool expected)
    {
        var e = new ApiEndpoint(ApiEndpointBase.Orchestrator, "/odata/X", methods, 0, 0, "");
        Assert.Equal(expected, ApiEndpointCatalog.MatchesMethod(e, method));
    }

    [Theory]
    // base code, -Identity, -Portal, offered?
    [InlineData("O", false, false, true)]
    [InlineData("S", false, false, true)]   // same host, no switch
    [InlineData("I", false, false, false)]
    [InlineData("P", false, false, false)]
    [InlineData("I", true, false, true)]
    [InlineData("O", true, false, false)]
    [InlineData("P", false, true, true)]
    [InlineData("O", false, true, false)]
    public void Selects_the_base_the_switches_reach(string baseCode, bool identity, bool portal, bool expected)
    {
        var e = Assert.Single(ParseText($"{baseCode}\t/x\tGET\t\t"));
        Assert.Equal(expected, ApiEndpointCatalog.MatchesSwitches(e, identity, portal));
    }

    // ----- id substitution from the drive context -----

    [Fact]
    public void Fills_the_partition_placeholder()
        => Assert.Equal(
            "/api/Group/9f1c-2b3e",
            ApiEndpointCatalog.FillPlaceholders("/api/Group/{partitionGlobalId}", "9f1c-2b3e", null));

    [Fact]
    public void Fills_the_project_placeholder_from_a_DU_or_TM_location()
        => Assert.Equal(
            "/du_/api/framework/projects/proj-42/document-types",
            ApiEndpointCatalog.FillPlaceholders(
                "/du_/api/framework/projects/{projectId}/document-types", null, "proj-42"));

    [Fact]
    public void Fills_both_ids_in_one_path()
        => Assert.Equal(
            "/x/pid/y/proj-42",
            ApiEndpointCatalog.FillPlaceholders("/x/{partitionGlobalId}/y/{projectId}", "pid", "proj-42"));

    [Fact]
    public void Fills_every_occurrence()
        => Assert.Equal(
            "/api/x/pid/y/pid",
            ApiEndpointCatalog.FillPlaceholders("/api/x/{partitionGlobalId}/y/{partitionGlobalId}", "pid", null));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Leaves_the_template_intact_when_the_id_is_unknown(string? id)
    {
        // A drive that has never authenticated can't produce the partition id without an API
        // round-trip, and at a DU/TM drive's ROOT no single project is in scope — a <Tab> keypress
        // may not go to the API for either. The template is still a usable completion.
        Assert.Equal(
            "/api/Group/{partitionGlobalId}",
            ApiEndpointCatalog.FillPlaceholders("/api/Group/{partitionGlobalId}", id, null));

        Assert.Equal(
            "/testmanager_/api/v2/{projectId}/testsets",
            ApiEndpointCatalog.FillPlaceholders("/testmanager_/api/v2/{projectId}/testsets", null, id));
    }

    [Fact]
    public void Leaves_paths_without_a_placeholder_alone()
        => Assert.Equal("/odata/Assets",
            ApiEndpointCatalog.FillPlaceholders("/odata/Assets", "pid", "proj-42"));

    [Fact]
    public void A_null_context_fills_nothing()
        => Assert.Equal("/api/Group/{partitionGlobalId}",
            ApiEndpointCatalog.FillPlaceholders("/api/Group/{partitionGlobalId}", context: null));

    // ----- unresolved placeholders: the send-time guard and the tooltip flag -----
    //
    // Anything the drive context can't fill -- {token}, {id}, {objectType}, ... -- must be caught
    // before the request goes out, or a literal "{token}" reaches the server and comes back a 404.
    // UnresolvedPlaceholders is the shared detector: the cmdlet refuses on a non-empty result, the
    // completer flags it in the tooltip.

    [Fact]
    public void No_unresolved_placeholders_when_the_path_has_none()
        => Assert.Empty(ApiEndpointCatalog.UnresolvedPlaceholders("/testmanager_/api/serverinfo"));

    [Fact]
    public void Finds_a_lone_manual_placeholder()
        => Assert.Equal(
            new[] { "{token}" },
            ApiEndpointCatalog.UnresolvedPlaceholders("/testmanager_/api/webhookconnector/{token}/defects"));

    [Fact]
    public void Finds_every_distinct_placeholder_in_first_seen_order()
        => Assert.Equal(
            new[] { "{projectId}", "{objectType}", "{id}" },
            ApiEndpointCatalog.UnresolvedPlaceholders(
                "/testmanager_/api/v2/{projectId}/attachments/{objectType}/{id}"));

    [Fact]
    public void Collapses_a_repeated_placeholder_to_one_entry()
        => Assert.Equal(
            new[] { "{partitionGlobalId}" },
            ApiEndpointCatalog.UnresolvedPlaceholders("/x/{partitionGlobalId}/y/{partitionGlobalId}"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_unresolved_placeholders_in_a_null_or_empty_path(string? path)
        => Assert.Empty(ApiEndpointCatalog.UnresolvedPlaceholders(path));

    [Fact]
    public void Only_the_manual_ids_survive_context_substitution()
    {
        // The exact flow the send-time guard sees: fill the context ids first, then detect what is
        // left. {projectId} and {partitionGlobalId} go, the caller-supplied ids stay.
        string filled = ApiEndpointCatalog.FillPlaceholders(
            "/testmanager_/api/v2/{projectId}/attachments/{objectType}/{id}", "pid", "proj-42");

        Assert.Equal(
            new[] { "{objectType}", "{id}" },
            ApiEndpointCatalog.UnresolvedPlaceholders(filled));
    }

    [Fact]
    public void Manual_placeholder_hint_names_a_connector_token()
    {
        var e = new ApiEndpoint(ApiEndpointBase.Service,
            "/testmanager_/api/webhookconnector/{token}/defects", "GET", 0, 0, "");

        Assert.Equal("fill {token} in yourself", ApiPathCompleter.ManualPlaceholderHint(e));
    }

    [Fact]
    public void Manual_placeholder_hint_ignores_the_context_filled_ids()
    {
        // {projectId} has its own ProjectHint and {partitionGlobalId} always fills, so neither is
        // the manual-hint's business -- only a caller-supplied id would be.
        var e = new ApiEndpoint(ApiEndpointBase.Service,
            "/testmanager_/api/v2/{projectId}/testcases", "GET", 0, 0, "");

        Assert.Null(ApiPathCompleter.ManualPlaceholderHint(e));
    }

    [Fact]
    public void Manual_placeholder_hint_is_null_when_nothing_is_manual()
        => Assert.Null(ApiPathCompleter.ManualPlaceholderHint(
            new ApiEndpoint(ApiEndpointBase.Orchestrator, "/odata/Assets", "GET", 0, 0, "")));

    // ----- a project id belongs to ONE service -----

    [Theory]
    [InlineData("/testmanager_/api/v2/{projectId}/testcases", nameof(ApiService.TestManager))]
    [InlineData("/du_/api/framework/projects/{projectId}", nameof(ApiService.DocumentUnderstanding))]
    [InlineData("/odata/Assets", nameof(ApiService.None))]
    [InlineData("/aifabric_/ai-deployer/v2/mlskills", nameof(ApiService.None))]
    public void Reads_the_service_off_the_gateway_prefix(string path, string expected)
        => Assert.Equal(expected, ApiEndpointCatalog.ServiceOf(path).ToString());

    [Fact]
    public void Does_not_paste_a_TM_project_id_into_a_DU_path()
    {
        // A Test Manager location says nothing about which DU project is meant. Substituting its id
        // into a /du_ path would address nothing at all — silently, with a plausible-looking URL.
        var tmContext = TmContext("tm-proj-1");

        Assert.Equal(
            "/testmanager_/api/v2/tm-proj-1/testcases",
            ApiEndpointCatalog.FillPlaceholders("/testmanager_/api/v2/{projectId}/testcases", tmContext));

        Assert.Equal(
            "/du_/api/framework/projects/{projectId}/document-types",
            ApiEndpointCatalog.FillPlaceholders("/du_/api/framework/projects/{projectId}/document-types", tmContext));
    }

    // A context carrying a TM project. Drive/Folder aren't read by FillPlaceholders, so a bare
    // record with the id and its service is enough to pin the service-matching rule.
    private static ApiContext TmContext(string projectId)
        => new(Drive: null!, Folder: null, PartitionGlobalId: null,
               ProjectId: projectId, ProjectKind: ApiService.TestManager, ContextPath: @"Orch1Tm:\HEHE");

    // ----- word matching -----
    //
    // The completer filters with the shared CreateWPFromWordToComplete, so -Uri follows the
    // same convention as every other parameter: a plain word is a prefix, and an explicit
    // wildcard opts into substring search. With ~900 endpoints that wildcard is the main way
    // in — `*du*`<Ctrl+Space> lists the Document Understanding endpoints.
    private sealed class PatternProbe : ApiPathCompleter
    {
        internal static WildcardPattern For(string word) => CreateWPFromWordToComplete(word);
    }

    [Theory]
    [InlineData("/odata/As", "/odata/Assets", true)]       // plain word: prefix match
    [InlineData("/odata/As", "/api/Assets", false)]
    [InlineData("*du*", "/du_/api/framework/projects", true)]   // explicit wildcard: substring
    [InlineData("*du*", "/odata/Assets", false)]
    [InlineData("*Asset*", "/odata/Assets", true)]
    [InlineData("*Asset*", "/odata/Robots", false)]
    [InlineData("", "/odata/Assets", true)]                // nothing typed: everything matches
    public void Matches_the_typed_word(string word, string uri, bool expected)
        => Assert.Equal(expected, PatternProbe.For(word).IsMatch(uri));

    // ----- the shipped catalog -----

    [Fact]
    public void Embedded_catalog_loads_and_covers_every_base()
    {
        var all = ApiEndpointCatalog.All;
        Assert.NotEmpty(all);

        foreach (var b in new[] { ApiEndpointBase.Orchestrator, ApiEndpointBase.Identity, ApiEndpointBase.Portal, ApiEndpointBase.Service })
        {
            Assert.Contains(all, e => e.Base == b);
        }
    }

    [Fact]
    public void Embedded_catalog_has_the_partition_scoped_identity_endpoints()
    {
        // The whole point of the {partitionGlobalId} substitution: these must be in the catalog
        // with the placeholder spelled exactly as FillPlaceholders looks for it.
        Assert.Contains(ApiEndpointCatalog.All,
            e => e.Base == ApiEndpointBase.Identity && e.Path == "/api/Group/{partitionGlobalId}");

        Assert.Contains(ApiEndpointCatalog.All,
            e => e.Base == ApiEndpointBase.Portal && e.Path == "/api/auditLog/{partitionGlobalId}");

        Assert.All(
            ApiEndpointCatalog.All.Where(e => e.Path.Contains("partitionGlobalId")),
            e => Assert.Contains(ApiEndpointCatalog.PartitionPlaceholder, e.Path));
    }

    [Fact]
    public void Embedded_catalog_has_the_project_scoped_DU_and_TM_endpoints()
    {
        // The DU/TM counterpart: {projectId} is what a location on a DU/TM drive fills.
        Assert.Contains(ApiEndpointCatalog.All,
            e => e.Base == ApiEndpointBase.Service
              && e.Path == "/du_/api/framework/projects/{projectId}");

        Assert.Contains(ApiEndpointCatalog.All,
            e => e.Base == ApiEndpointBase.Service
              && e.Path.StartsWith("/testmanager_/api/v2/{projectId}/"));

        Assert.All(
            ApiEndpointCatalog.All.Where(e => e.Path.Contains("projectId")),
            e => Assert.Contains(ApiEndpointCatalog.ProjectPlaceholder, e.Path));
    }

    [Fact]
    public void Embedded_catalog_paths_are_rooted_and_untagged_outside_orchestrator()
    {
        Assert.All(ApiEndpointCatalog.All, e => Assert.StartsWith("/", e.Path));

        // Only Orchestrator carries a version range — it's the only base with an
        // api-supported-versions signal to filter against.
        Assert.All(
            ApiEndpointCatalog.All.Where(e => e.Base != ApiEndpointBase.Orchestrator),
            e => Assert.Equal(0, e.MinVersion));
    }

    [Fact]
    public void Embedded_service_endpoints_carry_their_gateway_prefix()
    {
        // A Service path is tenant-root relative and must name its service, or Invoke-OrchApi
        // would resolve it against the Orchestrator base and hit the wrong service.
        Assert.All(
            ApiEndpointCatalog.All.Where(e => e.Base == ApiEndpointBase.Service),
            e => Assert.True(
                e.Path.StartsWith("/testmanager_/") || e.Path.StartsWith("/du_/") || e.Path.StartsWith("/aifabric_/"),
                $"service endpoint without a known gateway prefix: {e.Path}"));
    }
}
