using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// StartStrategy is the "Execute the process X times" count in BOTH folder kinds. The copy may
// only fall back to a single run when the source value is not a usable count -- see
// OrchProvider.StartStrategyNeedsReset for the two measurements (modern Automation Suite
// 24.10.11, classic standalone 21.10.4) this encodes.
//
// The regression these guard: an earlier version reset the field for every classic SOURCE, which
// dropped the count on exactly the classic-to-modern migrations it was meant to serve, and the
// version before that reset it for every modern DESTINATION, which dropped it always.
public class StartStrategyNeedsResetTests
{
    [Theory]
    [InlineData(1)]   // Dynamic Allocation, "Execute the process 1 time"
    [InlineData(3)]   // ... 3 times -- measured on both 21.10.4 (classic) and 24.10.11 (modern)
    [InlineData(5)]
    [InlineData(10000)] // documented ceiling for dynamic allocation
    public void A_positive_count_is_carried_across(int startStrategy)
        => Assert.False(OrchProvider.StartStrategyNeedsReset(startStrategy));

    [Theory]
    [InlineData(-1)]  // classic "All Robots" -- no modern equivalent
    [InlineData(0)]
    public void A_value_below_one_is_not_a_count_and_falls_back(int startStrategy)
        => Assert.True(OrchProvider.StartStrategyNeedsReset(startStrategy));

    [Fact]
    public void An_absent_value_falls_back()
        => Assert.True(OrchProvider.StartStrategyNeedsReset(null));
}
