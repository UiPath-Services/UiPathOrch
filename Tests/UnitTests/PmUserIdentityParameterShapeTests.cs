using System;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Locks the user-selecting parameter of Get/Copy/Update/Remove-PmUser as
// -UserName with the alias -Email.
//
// These cmdlets bind the selector ByPropertyName, and PowerShell tries the
// parameter's own name before its aliases. A local user on Automation Suite /
// on-premises can have an empty email but never an empty userName, so with
// -Email as the primary name, `Get-PmUser | Copy-PmUser` (or a Get-PmUser CSV)
// bound the empty email and failed with "Cannot bind argument to parameter
// 'Email' because it is an empty string". -Email stays as an alias so existing
// scripts and Email-only CSVs keep working.
public class PmUserIdentityParameterShapeTests
{
    public static TheoryData<Type> Cmdlets => new()
    {
        typeof(GetPmUserCmdlet),
        typeof(CopyPmUserCmdlet),
        typeof(UpdatePmUserCmdlet),
        typeof(RemovePmUserCmdlet),
    };

    [Theory]
    [MemberData(nameof(Cmdlets))]
    public void UserNameIsPrimaryAndBindsByPropertyName(Type cmdlet)
    {
        var prop = cmdlet.GetProperty("UserName", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(string[]), prop!.PropertyType);

        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a =>
        {
            Assert.Equal(0, a.Position);
            Assert.True(a.ValueFromPipelineByPropertyName);
        });
    }

    [Theory]
    [MemberData(nameof(Cmdlets))]
    public void EmailIsAnAliasNotAParameter(Type cmdlet)
    {
        Assert.Null(cmdlet.GetProperty("Email", BindingFlags.Public | BindingFlags.Instance));

        var aliases = cmdlet.GetProperty("UserName")!
            .GetCustomAttributes<AliasAttribute>()
            .SelectMany(a => a.AliasNames);
        Assert.Contains("Email", aliases);
    }
}
