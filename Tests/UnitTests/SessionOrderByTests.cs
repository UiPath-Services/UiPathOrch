using System.Management.Automation;
using Xunit;
using static UiPath.PowerShell.Commands.GetUserSessionCmdlet;

namespace UnitTests;

// Tests for BuildSessionOrderByClause -- the OData $orderby builder behind
// Get-OrchUserSession -OrderBy / -OrderDescending. Verifies the friendly key ->
// field mapping, the per-field direction (default ascending -- the natural order
// for the categorical session fields -- and descending under -OrderDescending),
// and that an invalid key surfaces the clear PSArgumentException.
public class SessionOrderByTests
{
    [Fact]
    public void Null_ReturnsNull()
        => Assert.Null(BuildSessionOrderByClause(null, descending: false));

    [Fact]
    public void Empty_ReturnsNull()
        => Assert.Null(BuildSessionOrderByClause([], descending: true));

    [Fact]
    public void SingleField_DefaultsToAscending()
        => Assert.Equal("&$orderby=HostMachineName asc",
            BuildSessionOrderByClause(["Hostname"], descending: false));

    [Fact]
    public void SingleField_DescendingWhenRequested()
        => Assert.Equal("&$orderby=HostMachineName desc",
            BuildSessionOrderByClause(["Hostname"], descending: true));

    [Fact]
    public void MapsFriendlyKeyToFieldPath()
        => Assert.Equal("&$orderby=Robot/User/UserName asc",
            BuildSessionOrderByClause(["User"], descending: false));

    [Fact]
    public void MultipleFields_EachCarriesItsOwnDirection()
    {
        // OData "A,B desc" would leave A at the default; every field must carry
        // the direction explicitly.
        var clause = BuildSessionOrderByClause(["User", "Hostname"], descending: true);
        Assert.Equal("&$orderby=Robot/User/UserName desc,HostMachineName desc", clause);
    }

    [Fact]
    public void UnknownKey_ThrowsClearPSArgumentException()
    {
        var ex = Assert.Throws<PSArgumentException>(
            () => BuildSessionOrderByClause(["Bogus"], descending: false));
        Assert.Contains("Bogus", ex.Message);
        Assert.Contains("OrderBy", ex.Message);
        Assert.Contains("Hostname", ex.Message); // valid values listed
    }
}
