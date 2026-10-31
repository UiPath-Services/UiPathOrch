using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Test steps are addressed by position. Positions count from 0, negative ones from the end, and
// a request that changes nothing must send no operation.
public class TmTestStepTests
{
    [Theory]
    [InlineData(0, 3, 0)]
    [InlineData(2, 3, 2)]
    [InlineData(-1, 3, 2)] // the last step
    [InlineData(-3, 3, 0)]
    public void ResolvePosition_InRange(int position, int count, int expected)
        => Assert.Equal(expected, TmTestSteps.ResolvePosition(position, count));

    [Theory]
    [InlineData(3, 3)]
    [InlineData(-4, 3)]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void ResolvePosition_OutOfRange_Null(int position, int count)
        => Assert.Null(TmTestSteps.ResolvePosition(position, count));

    [Theory]
    [InlineData(0, 3, 0)]
    [InlineData(3, 3, 3)]  // after the last step
    [InlineData(-1, 3, 3)] // append
    [InlineData(-2, 3, 2)] // before the last step
    [InlineData(-4, 3, 0)]
    [InlineData(-1, 0, 0)] // append to an empty test case
    [InlineData(0, 0, 0)]
    public void ResolveInsertPosition_InRange(int position, int count, int expected)
        => Assert.Equal(expected, TmTestSteps.ResolveInsertPosition(position, count));

    [Theory]
    [InlineData(4, 3)]
    [InlineData(-5, 3)]
    [InlineData(1, 0)]
    public void ResolveInsertPosition_OutOfRange_Null(int position, int count)
        => Assert.Null(TmTestSteps.ResolveInsertPosition(position, count));

    private static List<TmTestStep> Steps(int n)
        => Enumerable.Range(0, n).Select(i => new TmTestStep { id = $"s{i}", orderNo = i, description = $"D{i}" }).ToList();

    [Fact]
    public void SelectByPosition_SortedDistinct_AndReportsOutOfRange()
    {
        var outOfRange = new List<int>();
        var selected = TmTestSteps.SelectByPosition(Steps(4), [3, 0, -1, 9, -9], outOfRange);
        Assert.Equal(["s0", "s3"], selected.Select(s => s.id));
        Assert.Equal([9, -9], outOfRange);
    }

    [Fact]
    public void Patch_NothingGiven_NoOperation()
        => Assert.Empty(UpdateTmTestStepCmdlet.ComputeTestStepPatch(Steps(1)[0], null, null, null));

    [Fact]
    public void Patch_SameValues_NoOperation()
        => Assert.Empty(UpdateTmTestStepCmdlet.ComputeTestStepPatch(Steps(1)[0], "D0", "", ""));

    [Fact]
    public void Patch_AllChanged_ThreeOperations_InFieldOrder()
    {
        var ops = UpdateTmTestStepCmdlet.ComputeTestStepPatch(Steps(1)[0], "a", "b", "c");
        Assert.Equal(["/description", "/expectedResult", "/clipboardData"], ops.Select(o => o.path));
    }

    // An empty OrderNo cell in a CSV must reach the cmdlets as "not given", not as step 0.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyString_BecomesNull(string input)
    {
        var attribute = new EmptyStringToNullArgumentTransformationAttribute();
        Assert.Null(attribute.Transform(null!, input));
        Assert.Null(attribute.Transform(null!, new System.Management.Automation.PSObject(input)));
    }

    [Fact]
    public void OtherValues_PassThrough()
    {
        var attribute = new EmptyStringToNullArgumentTransformationAttribute();
        Assert.Equal("0", attribute.Transform(null!, "0"));
        Assert.Equal(-1, attribute.Transform(null!, -1));
    }
}
