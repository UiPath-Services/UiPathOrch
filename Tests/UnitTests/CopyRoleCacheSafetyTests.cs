using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Regression guard for the Copy-OrchRole fix (CopyRole.cs): PostRole strips the role in place,
// including nulling each Permission's Id/RoleId. Copy-OrchRole iterates the SOURCE drive's cached
// roles, so it must pass a DeepCopy — otherwise the in-place strip corrupts the source Role cache
// and its shared Permission instances (observable on a later Get-OrchRole).
//
// The full cmdlet path needs a live drive and can't be unit-mocked, so this pins the property the
// fix relies on: DeepCopy produces an independent Permission graph. A regression to a shallow copy
// (which would re-introduce the bug) fails this test.
public class CopyRoleCacheSafetyTests
{
    [Fact]
    public void DeepCopy_of_Role_isolates_nested_permissions()
    {
        var original = new Role
        {
            Id = 5,
            Name = "Admin",
            IsStatic = true,
            Permissions = new() { new Permission { Name = "Units.View", Id = 11, RoleId = 5 } },
        };

        var copy = OrchCollectionExtensions.DeepCopy(original);
        // Mutate the copy exactly as PostRole would before POSTing.
        copy.Id = null;
        copy.IsStatic = null;
        copy.Permissions![0].Id = null;
        copy.Permissions![0].RoleId = null;

        // The source role and its Permission are untouched.
        Assert.Equal(5, original.Id);
        Assert.True(original.IsStatic);
        Assert.Equal(11, original.Permissions![0].Id);
        Assert.Equal(5, original.Permissions![0].RoleId);
        Assert.NotSame(original.Permissions[0], copy.Permissions[0]);
    }
}
