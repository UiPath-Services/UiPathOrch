using System;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// The auth circuit breaker latches a drive only on a 401 from Orchestrator itself. Before, any
// 401 after a re-auth latched it: on 20.10.16 the Identity API answers every Pm call 401 with a
// good token, so one Get-PmRobotAccount made the whole drive fail until Import-OrchConfig
// (2026-10-07). On-premises Identity and portal sit under the Orchestrator root, and other
// services are reached through it, so the test is the path, not the prefix.
public class IsOrchestratorApiRequestTests
{
    [Theory]
    // On-premises: Orchestrator, Identity and portal share the root.
    [InlineData("https://op2010.local/odata/Folders", "https://op2010.local", true)]
    [InlineData("https://op2010.local/api/Status/Version", "https://op2010.local", true)]
    [InlineData("https://op2010.local/odata/ProcessSchedules?$filter=x", "https://op2010.local", true)]
    [InlineData("https://op2010.local/identity/api/RobotAccount/abc", "https://op2010.local", false)]
    [InlineData("https://op2010.local/portal/api/x", "https://op2010.local", false)]
    // Automation Suite: Orchestrator under /orchestrator_, other services beside it.
    [InlineData("https://as.example/default/DefaultTenant/orchestrator_/odata/Robots", "https://as.example/default/DefaultTenant/orchestrator_", true)]
    [InlineData("https://as.example/identity_/api/Group/x", "https://as.example/default/DefaultTenant/orchestrator_", false)]
    // Cloud: other services are reached through the Orchestrator base.
    [InlineData("https://cloud.uipath.com/org/tenant/odata/Queues", "https://cloud.uipath.com/org/tenant", true)]
    [InlineData("https://cloud.uipath.com/org/tenant/testmanager_/api/v2/projects", "https://cloud.uipath.com/org/tenant", false)]
    [InlineData("https://cloud.uipath.com/org/identity_/api/Group/x", "https://cloud.uipath.com/org/tenant", false)]
    // A Root written with a trailing slash puts "//" before the endpoint.
    [InlineData("https://cloud.uipath.com/org/tenant//odata/Queues", "https://cloud.uipath.com/org/tenant/", true)]
    // A sibling whose name only starts like the base is not under it.
    [InlineData("https://cloud.uipath.com/org/tenant2/odata/Queues", "https://cloud.uipath.com/org/tenant", false)]
    public void Recognises_Orchestrator_requests(string url, string orchestratorBase, bool expected)
        => Assert.Equal(expected, OrchAPISession.IsOrchestratorApiRequest(new Uri(url), orchestratorBase));

    [Fact]
    public void A_request_without_a_uri_is_not_Orchestrator()
        => Assert.False(OrchAPISession.IsOrchestratorApiRequest(null, "https://op2010.local"));
}
