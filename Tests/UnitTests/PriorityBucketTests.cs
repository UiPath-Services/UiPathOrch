using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Servers below API 14 take only JobPriority (Low / Normal / High), so a 1-100 SpecificPriorityValue
// is sent as its bucket there. Triggers lacked the conversion that Releases had: on 20.10.16 (API
// 11.1) every trigger PUT carrying a value failed with 400 "model must not be null" (2026-10-06).
// The cut has to agree with the one Compare-Orch* reads a value back with.
public class PriorityBucketTests
{
    [Theory]
    [InlineData(1, "Low")]
    [InlineData(25, "Low")]
    [InlineData(30, "Low")]
    [InlineData(31, "Normal")]
    [InlineData(45, "Normal")]
    [InlineData(60, "Normal")]
    [InlineData(61, "High")]
    [InlineData(65, "High")]
    [InlineData(100, "High")]
    public void Buckets_a_value(int value, string expected)
        => Assert.Equal(expected, OrchAPISession.PriorityBucket(value));

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(60)]
    [InlineData(61)]
    [InlineData(100)]
    public void Agrees_with_the_comparison_cut(int value)
        => Assert.Equal(EntityComparison.EffectiveJobPriority(null, value), OrchAPISession.PriorityBucket(value));
}
