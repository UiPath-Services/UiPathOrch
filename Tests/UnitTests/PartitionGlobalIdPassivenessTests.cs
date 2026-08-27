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

    // Coverage instrumentation rewrites every method to record hits, so an instrumented build's
    // IL is not the IL that ships -- the getter gains exactly the `call` this test forbids. The
    // shape assertions below are therefore about the shipped assembly only; the "does not invoke
    // GetPartitionGlobalId" assertion, which is the actual regression, holds either way.
    private static bool IsInstrumentedForCoverage =>
        typeof(OrchDriveInfo).Assembly.GetTypes()
            .Any(t => t.Namespace?.StartsWith("Coverlet.Core.Instrumentation", StringComparison.Ordinal) == true);

    private static bool ContainsSequence(byte[] haystack, byte[] needle) =>
        Enumerable.Range(0, haystack.Length - needle.Length + 1)
            .Any(i => haystack.Skip(i).Take(needle.Length).SequenceEqual(needle));

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

        // The regression shape is the getter invoking GetPartitionGlobalId(), whose fallback
        // path issues an authenticated API call. Look for that method's metadata token rather
        // than for call opcodes: a raw byte scan cannot tell an opcode from an operand byte
        // (a field token containing 0x28 would read as a `call`), and coverage instrumentation
        // legitimately injects calls of its own.
        var active = typeof(OrchDriveInfo).GetMethod(
            "GetPartitionGlobalId",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.False(
            ContainsSequence(il, BitConverter.GetBytes(active.MetadataToken)),
            "PartitionGlobalId's getter must not invoke GetPartitionGlobalId().");

        if (IsInstrumentedForCoverage) return;

        // Expected IL for `=> _partitionGlobalId`, in the shipped assembly:
        //   ldarg.0 (0x02)
        //   ldfld   (0x7B) + 4-byte field token
        //   ret     (0x2A)
        Assert.Equal(0x02, il[0]);                       // ldarg.0
        Assert.Equal(0x7B, il[1]);                       // ldfld
        Assert.Equal(0x2A, il[il.Length - 1]);           // ret
        Assert.Equal(7, il.Length);                      // 1 + 1 + 4 + 1: nothing else fits
    }
}
