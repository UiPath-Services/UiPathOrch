using System.Reflection;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Regression test for the 1.5.3 -> 1.5.4 PKCE-on-Import-OrchConfig bug.
//
// Cause: PerOrganization caches' ClearCache() read `_drive.PartitionGlobalId`,
// which was a getter that delegated to GetPartitionGlobalId() -- an active
// method whose fallback path issues an authenticated Users API call. During
// Import-OrchConfig's drive teardown (SessionState.Drive.Remove ->
// OrchProvider.RemoveDrive -> OrchDriveInfo.ClearAllCache -> per-cache
// ClearCache), every Enabled but never-authed drive thus triggered PKCE in
// turn -- one browser per drive.
//
// Fix: split the API surface in two on OrchDriveInfoBase:
//   PartitionGlobalId      -- passive property, returns the cached field
//                             (null until populated); safe from cleanup paths.
//   GetPartitionGlobalId() -- active method, lazily fetches when needed;
//                             only called from data-fetch paths.
//
// These tests lock in (a) the split exists on the base and (b) the
// OrchDriveInfo getter is genuinely passive (a single field load, no call
// instruction) so a future "simplify" refactor that re-collapses them
// cannot silently regress the PKCE behavior.
public class PartitionGlobalIdPassivenessTests
{
    [Fact]
    public void OrchDriveInfoBase_DefinesPassivePropertyAndActiveMethod()
    {
        var prop = typeof(OrchDriveInfoBase).GetProperty(
            "PartitionGlobalId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.True(prop!.GetGetMethod(nonPublic: true)!.IsAbstract,
            "PartitionGlobalId must remain an abstract property on the base.");

        var method = typeof(OrchDriveInfoBase).GetMethod(
            "GetPartitionGlobalId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.True(method!.IsAbstract,
            "GetPartitionGlobalId must remain an abstract method on the base.");
    }

    [Fact]
    public void OrchDriveInfo_PartitionGlobalIdGetter_IsPassiveFieldLoad()
    {
        var getter = typeof(OrchDriveInfo).GetProperty(
            "PartitionGlobalId",
            BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetGetMethod(nonPublic: true)!;

        var body = getter.GetMethodBody();
        Assert.NotNull(body);
        var il = body!.GetILAsByteArray()!;

        // Expected IL for `=> _partitionGlobalId`:
        //   ldarg.0 (0x02)
        //   ldfld   (0x7B) + 4-byte field token
        //   ret     (0x2A)
        // A call/callvirt would mean the getter is invoking
        // GetPartitionGlobalId() (or another method) -- the regression shape.
        Assert.Equal(0x02, il[0]);                       // ldarg.0
        Assert.Equal(0x7B, il[1]);                       // ldfld
        Assert.Equal(0x2A, il[il.Length - 1]);           // ret
        Assert.DoesNotContain((byte)0x28, il);           // call
        Assert.DoesNotContain((byte)0x6F, il);           // callvirt
    }
}
