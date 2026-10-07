using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Org-scoped caches are process-wide and were keyed by the partition id alone. Every on-premises
// install's default organization has the same partition id (identical from 20.10.16 to 25.10.2,
// 2026-10-07), so with two on-premises drives mounted, Get-PmUser on one returned the other
// server's users, an "unsupported" result cached for 20.10 was replayed for 21.10 and 22.4, and the
// Copy-Pm* cmdlets skipped every item as "same organization". The key is now server + partition.
public class OrgKeyTests
{
    private const string DefaultOrgPartition = "d5bd4618-34f9-4a7a-841f-998407b81e71";

    [Fact]
    public void Two_on_premises_servers_with_the_same_partition_are_different_organizations()
        => Assert.NotEqual(
            OrchDriveInfoBase.OrgKey(OrchAPISession.ToServerAuthority("https://op2010.local"), DefaultOrgPartition),
            OrchDriveInfoBase.OrgKey(OrchAPISession.ToServerAuthority("https://op2510.local"), DefaultOrgPartition));

    [Fact]
    public void Two_tenants_of_one_cloud_organization_share_it()
        => Assert.Equal(
            OrchDriveInfoBase.OrgKey(OrchAPISession.ToServerAuthority("https://cloud.uipath.com/myorg/DefaultTenant"), "p1"),
            OrchDriveInfoBase.OrgKey(OrchAPISession.ToServerAuthority("https://cloud.uipath.com/myorg/Tenant2/"), "p1"));

    [Fact]
    public void Two_cloud_organizations_differ_by_partition()
        => Assert.NotEqual(
            OrchDriveInfoBase.OrgKey(OrchAPISession.ToServerAuthority("https://cloud.uipath.com/a/DefaultTenant"), "p1"),
            OrchDriveInfoBase.OrgKey(OrchAPISession.ToServerAuthority("https://cloud.uipath.com/b/DefaultTenant"), "p2"));

    [Theory]
    [InlineData("https://op2510.local", "https://op2510.local")]
    [InlineData("https://OP2510.Local/", "https://op2510.local")]
    [InlineData("https://op2510.local:8443/Default", "https://op2510.local:8443")]
    [InlineData("https://cloud.uipath.com/org/tenant", "https://cloud.uipath.com")]
    public void The_server_is_scheme_host_and_port(string baseUrl, string expected)
        => Assert.Equal(expected, OrchAPISession.ToServerAuthority(baseUrl));
}
