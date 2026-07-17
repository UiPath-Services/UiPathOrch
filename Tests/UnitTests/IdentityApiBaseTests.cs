using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins OrchAPISession.ResolveIdentityApiBase: which identity API base a session uses.
//
// Two hard-won facts pull in opposite directions (both verified live):
//  * The PKCE AUTHORIZE endpoint must be HOST-level on Cloud — org-scoping it regressed
//    Entra-federated orgs with errorCode=219 (d57c287; see IdentityUrlAutoGenTests). The
//    config therefore auto-generates IdentityUrl = "https://{host}/identity_".
//  * The identity API must be ORG-scoped on Cloud — a newer org rejects partition-scoped
//    calls (/api/Group/{prt}, /api/User/BulkCreate) on the host-level route with
//    InvalidPartition, while "/{org}/identity_" works on every org; the Cloud admin
//    console itself calls "/{partitionGlobalId}/identity_". Older orgs accept both, which
//    is why the clobbering shipped unnoticed.
//
// ResolveIdentityApiBase reconciles them: on Cloud the auto-generated host-level default
// is NOT allowed to clobber the org-scoped API base, while an explicitly pinned
// (non-default) Identity Server still wins everywhere. The authorize endpoint keeps using
// the drive's IdentityUrl and is untouched by this.
public class IdentityApiBaseTests
{
    private const string OrgScoped = "https://cloud.uipath.com/ytsuda/identity_";
    private const string HostLevel = "https://cloud.uipath.com/identity_";

    // ----- the fix: Cloud + the auto-generated host-level value -----

    [Fact]
    public void Cloud_auto_generated_host_level_url_keeps_the_org_scoped_base()
        => Assert.Equal(OrgScoped,
            OrchAPISession.ResolveIdentityApiBase(OrgScoped, HostLevel, isCloudEdition: true));

    [Theory]
    [InlineData("https://cloud.uipath.com/identity_/")]   // trailing slash (setter trims, stay tolerant)
    [InlineData("HTTPS://CLOUD.UIPATH.COM/identity_")]    // scheme/host case
    public void Cloud_host_level_match_is_slash_and_case_tolerant(string identityUrl)
        => Assert.Equal(OrgScoped,
            OrchAPISession.ResolveIdentityApiBase(OrgScoped, identityUrl, isCloudEdition: true));

    // ----- a pinned (non-default) Identity Server always wins -----

    [Theory]
    [InlineData("https://id.contoso.com/identity")]                 // separate identity host
    [InlineData("https://cloud.uipath.com/custom/identity_")]       // same host, non-default path
    public void Cloud_explicitly_pinned_identity_server_wins(string pinned)
        => Assert.Equal(pinned,
            OrchAPISession.ResolveIdentityApiBase(OrgScoped, pinned, isCloudEdition: true));

    [Fact]
    public void Cloud_pinned_org_scoped_value_is_honored_verbatim()
        => Assert.Equal(OrgScoped,
            OrchAPISession.ResolveIdentityApiBase(OrgScoped, OrgScoped, isCloudEdition: true));

    // ----- non-Cloud editions keep today's behavior -----

    [Fact]
    public void AutomationSuite_keeps_the_host_level_override()
    {
        // AS deploys identity as a host-level service shared across orgs (verified on a
        // 2023.4 cluster) — there the auto-generated host-level value IS the right API base.
        const string asOrgScoped = "https://as.example.com/myorg/identity_";
        const string asHostLevel = "https://as.example.com/identity_";

        Assert.Equal(asHostLevel,
            OrchAPISession.ResolveIdentityApiBase(asOrgScoped, asHostLevel, isCloudEdition: false));
    }

    [Fact]
    public void OnPremises_pinned_identity_server_wins()
        => Assert.Equal("https://idsrv.corp.local/identity",
            OrchAPISession.ResolveIdentityApiBase(
                "https://orch.corp.local/identity", "https://idsrv.corp.local/identity",
                isCloudEdition: false));

    // ----- degenerate inputs -----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_identity_url_falls_back_to_the_computed_default(string? identityUrl)
        => Assert.Equal(OrgScoped,
            OrchAPISession.ResolveIdentityApiBase(OrgScoped, identityUrl, isCloudEdition: true));

    [Fact]
    public void Unparseable_default_defers_to_the_identity_url()
        => Assert.Equal(HostLevel,
            OrchAPISession.ResolveIdentityApiBase("not a uri", HostLevel, isCloudEdition: true));
}
