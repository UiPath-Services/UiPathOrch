using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Confirms the Get-OrchUserSession -OrderDescending switch is wired all the way
// through to the OData $orderby clause via the actual cmdlet instance (not just
// the pure builder): absent => ascending (the original default order), present
// => descending, applied to every field. Also pins the parameter surface so the
// reverted -OrderAscending experiment cannot creep back.
public class GetUserSessionParameterTests
{
    [Fact]
    public void OrderDescending_Absent_ProducesAscending()
    {
        var cmd = new GetUserSessionCmdlet { OrderBy = ["Hostname"] };
        Assert.Equal("&$orderby=HostMachineName asc", cmd.MakeOrderBy());
    }

    [Fact]
    public void OrderDescending_Present_ProducesDescending()
    {
        var cmd = new GetUserSessionCmdlet { OrderBy = ["Hostname"], OrderDescending = true };
        Assert.Equal("&$orderby=HostMachineName desc", cmd.MakeOrderBy());
    }

    [Fact]
    public void OrderDescending_Present_AppliesToEveryField()
    {
        var cmd = new GetUserSessionCmdlet { OrderBy = ["User", "Hostname"], OrderDescending = true };
        Assert.Equal("&$orderby=Robot/User/UserName desc,HostMachineName desc", cmd.MakeOrderBy());
    }

    [Fact]
    public void NoOrderBy_ProducesNoClause_EvenWithOrderDescending()
    {
        var cmd = new GetUserSessionCmdlet { OrderDescending = true };
        Assert.Null(cmd.MakeOrderBy());
    }

    [Fact]
    public void ParameterSurface_HasOrderDescendingSwitch_NotOrderAscending()
    {
        var t = typeof(GetUserSessionCmdlet);

        var desc = t.GetProperty("OrderDescending");
        Assert.NotNull(desc);
        Assert.Equal(typeof(SwitchParameter), desc!.PropertyType);
        Assert.NotNull(desc.GetCustomAttribute<ParameterAttribute>());

        // The earlier -OrderAscending (descending-default) experiment was reverted.
        Assert.Null(t.GetProperty("OrderAscending"));
    }
}
