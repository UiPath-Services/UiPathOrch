using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Locks the -Domain parameter surface on Add-OrchFolderUser (added in v1.6.0).
//
// The directory Search call (Robot resolution path) hard-codes
// `domain=autogen`, which works for Automation Cloud and non-federated OnPrem
// but is rejected with a generic 500 ("An unknown failure has occurred") on
// EntraID-federated OnPrem tenants. -Domain is the user-supplied escape
// hatch: it flows into both the Search URL and the AssignDomainUser payload.
//
// We can't E2E this without an EntraID-federated OnPrem tenant in the test
// environment (the customer who reported the bug runs on FastRetailing's
// OnPrem; we don't have a fixture there). The structural tests below at
// least prevent the parameter from being silently renamed / dropped / made
// mandatory by a future refactor before the customer-side verification
// completes.
public class AddFolderUserDomainShapeTests
{
    [Fact]
    public void HasDomainParameter()
    {
        var prop = typeof(AddFolderUserCmdlet).GetProperty(
            "Domain",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    [Fact]
    public void DomainIsOptionalAndNamedOnly()
    {
        // -Domain must stay opt-in: existing scripts call Add-OrchFolderUser
        // without it and rely on the autogen default. Promoting it to
        // Mandatory or to a Position would be a breaking change.
        var prop = typeof(AddFolderUserCmdlet).GetProperty("Domain")!;
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a =>
        {
            Assert.False(a.Mandatory, "-Domain must remain optional.");
            Assert.Equal(int.MinValue, a.Position);
        });
    }
}
