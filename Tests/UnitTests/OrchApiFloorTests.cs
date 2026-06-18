using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins the centralized API-version capability table that replaced the magic-number
// comparisons scattered through OrchAPISession's per-endpoint methods. The behavior that
// must be preserved is the nullable-comparison semantics of the historic inline checks:
// when the connection's ApiVersion is unknown (null), BOTH `>= floor` (Supports) and
// `< floor` (Below) are false, so a caller falls to its legacy branch and never strips.
public class OrchApiFloorTests
{
    [Theory]
    [InlineData(19.0, 19.0, true)]
    [InlineData(20.0, 19.0, true)]
    [InlineData(18.0, 19.0, false)]
    [InlineData(15.0, 16.0, false)]
    public void Supports_is_greater_or_equal(double apiVersion, double floor, bool expected)
        => Assert.Equal(expected, OrchApiFloor.Supports(apiVersion, floor));

    [Theory]
    [InlineData(18.0, 19.0, true)]
    [InlineData(15.0, 16.0, true)]
    [InlineData(19.0, 19.0, false)]
    [InlineData(20.0, 19.0, false)]
    public void Below_is_strictly_less(double apiVersion, double floor, bool expected)
        => Assert.Equal(expected, OrchApiFloor.Below(apiVersion, floor));

    // The crucial invariant: an unknown version is false for BOTH predicates (they are NOT
    // negations of each other in the null case). This matches `null >= N == false` and
    // `null < N == false`, the semantics of the original `ApiVersion >= N` / `< N` code.
    [Theory]
    [InlineData(16.0)]
    [InlineData(18.0)]
    [InlineData(19.0)]
    public void Unknown_version_is_false_for_both_predicates(double floor)
    {
        Assert.False(OrchApiFloor.Supports(null, floor));
        Assert.False(OrchApiFloor.Below(null, floor));
    }

    // Guards the documented floor values so an accidental edit to the table is caught.
    [Fact]
    public void Floor_values_match_documented_thresholds()
    {
        Assert.Equal(16.0, OrchApiFloor.QueueCreateAction);
        Assert.Equal(19.0, OrchApiFloor.QueueGetAction);
        Assert.Equal(16.0, OrchApiFloor.QueueRetentionMerge);
        Assert.Equal(18.0, OrchApiFloor.QueueRetryAbandonedItems);
        Assert.Equal(19.0, OrchApiFloor.QueueStaleRetention);
        Assert.Equal(18.0, OrchApiFloor.AlertsRemoved);
        Assert.Equal(12.0, OrchApiFloor.PackageEntryPointMetadata);
        Assert.Equal(19.0, OrchApiFloor.ReleaseGetAction);
        Assert.Equal(17.0, OrchApiFloor.ReleaseCreateAction);
        Assert.Equal(19.0, OrchApiFloor.ReleaseCloudRetentionDefault);
        Assert.Equal(19.0, OrchApiFloor.ReleaseV19Fields);
        Assert.Equal(17.0, OrchApiFloor.ReleaseV17Fields);
        Assert.Equal(16.0, OrchApiFloor.ReleaseV16Fields);
        Assert.Equal(15.0, OrchApiFloor.ReleaseEntryPointId);
        Assert.Equal(14.0, OrchApiFloor.ReleaseSpecificPriority);
        Assert.Equal(17.0, OrchApiFloor.ReleaseRetentionReadable);
    }
}
