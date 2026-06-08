using System.Management.Automation;
using Xunit;
using static UiPath.PowerShell.Commands.GetUserSessionCmdlet;

namespace UnitTests;

// Tests for BuildSessionOrderByClause -- the OData $orderby builder behind
// Get-OrchUserSession -OrderBy / -OrderAscending. Verifies the friendly key ->
// field mapping, the per-field direction (default descending for parity with
// the other Get-Orch* list cmdlets, ascending under -OrderAscending), and that
// an invalid key surfaces the clear PSArgumentException rather than a raw throw.
public class SessionOrderByTests
{
    [Fact]
    public void Null_ReturnsNull()
        => Assert.Null(BuildSessionOrderByClause(null, ascending: false));

    [Fact]
    public void Empty_ReturnsNull()
        => Assert.Null(BuildSessionOrderByClause([], ascending: true));

    [Fact]
    public void SingleField_DefaultsToDescending()
        => Assert.Equal("&$orderby=HostMachineName desc",
            BuildSessionOrderByClause(["Hostname"], ascending: false));

    [Fact]
    public void SingleField_AscendingWhenRequested()
        => Assert.Equal("&$orderby=HostMachineName asc",
            BuildSessionOrderByClause(["Hostname"], ascending: true));

    [Fact]
    public void MapsFriendlyKeyToFieldPath()
        => Assert.Equal("&$orderby=Robot/User/UserName asc",
            BuildSessionOrderByClause(["User"], ascending: true));

    [Fact]
    public void MultipleFields_EachCarriesItsOwnDirection()
    {
        // OData "A,B desc" would leave A at the default; every field must carry
        // the direction explicitly.
        var clause = BuildSessionOrderByClause(["User", "Hostname"], ascending: false);
        Assert.Equal("&$orderby=Robot/User/UserName desc,HostMachineName desc", clause);
    }

    [Fact]
    public void UnknownKey_ThrowsClearPSArgumentException()
    {
        var ex = Assert.Throws<PSArgumentException>(
            () => BuildSessionOrderByClause(["Bogus"], ascending: true));
        Assert.Contains("Bogus", ex.Message);
        Assert.Contains("OrderBy", ex.Message);
        Assert.Contains("Hostname", ex.Message); // valid values listed
    }
}
