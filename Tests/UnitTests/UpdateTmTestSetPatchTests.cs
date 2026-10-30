using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Update-TmTestSet sends a JSON Patch with one "replace" per field that was given and differs from
// the current value -- including /enableCoverage, which the PUT body cannot carry.
public class UpdateTmTestSetPatchTests
{
    private static TmTestSet Current(bool? coverage = false) => new() { id = "s1", name = "Smoke", description = "desc", enableCoverage = coverage };

    [Fact]
    public void NothingGiven_NoOperation()
        => Assert.Empty(UpdateTmTestSetCmdlet.ComputeTestSetPatch(Current(), null, null, null));

    [Fact]
    public void SameValues_NoOperation()
        => Assert.Empty(UpdateTmTestSetCmdlet.ComputeTestSetPatch(Current(true), "Smoke", "desc", true));

    [Fact]
    public void CoverageOn_ReplacesWithBoolean()
    {
        var op = Assert.Single(UpdateTmTestSetCmdlet.ComputeTestSetPatch(Current(false), null, null, true));
        Assert.Equal("/enableCoverage", op.path);
        Assert.Equal(true, op.value); // a JSON boolean, not the string "true"
    }

    [Fact]
    public void CoverageOff_OnNullCurrent_NoOperation()
        => Assert.Empty(UpdateTmTestSetCmdlet.ComputeTestSetPatch(Current(null), null, null, false));

    [Fact]
    public void AllChanged_ThreeOperations_InFieldOrder()
    {
        var ops = UpdateTmTestSetCmdlet.ComputeTestSetPatch(Current(false), "Regression", "new", true);
        Assert.Equal(["/name", "/description", "/enableCoverage"], ops.Select(o => o.path));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("False", false)]
    [InlineData("TRUE", true)]
    public void ParseBool_AcceptsTrueFalse(string input, bool expected)
        => Assert.Equal(expected, TmPatch.ParseBool(input, "EnableCoverage"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParseBool_NotGiven_Null(string? input)
        => Assert.Null(TmPatch.ParseBool(input, "EnableCoverage"));

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    public void ParseBool_Other_Throws(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => TmPatch.ParseBool(input, "EnableCoverage"));
        Assert.Contains("-EnableCoverage must be true or false", ex.Message);
    }
}
