using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// The -Priority name -> SpecificPriorityValue mapping. It runs in Update/New-OrchProcess and
// Update-OrchTrigger's ProcessRecord, ahead of the diff core, so the dirty tests start from the
// already-converted int and never exercised this table. Now protected internal, so it is testable.
public class PriorityConversionTests
{
    [Theory]
    [InlineData("Critical", 95)]
    [InlineData("Highest", 85)]
    [InlineData("VeryHigh", 75)]
    [InlineData("High", 65)]
    [InlineData("MediumHigh", 55)]
    [InlineData("Medium", 45)]
    [InlineData("MediumLow", 35)]
    [InlineData("Low", 25)]
    [InlineData("VeryLow", 15)]
    [InlineData("Lowest", 5)]
    public void Maps_each_priority_name(string name, int expected)
        => Assert.Equal(expected, OrchestratorPSCmdlet.ConvertPriorityToSpecificPriorityValue(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bogus")]
    [InlineData("high")]   // case-sensitive: not the "High" bucket
    public void Unknown_or_null_maps_to_null(string? name)
        => Assert.Null(OrchestratorPSCmdlet.ConvertPriorityToSpecificPriorityValue(name));
}
