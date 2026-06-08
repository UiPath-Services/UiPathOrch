using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Commands.GetJobCmdlet;

namespace UnitTests;

// Regression tests for OrderJobsForOutput -- the sort applied on the no-filter
// cache-output path of Get-OrchJob. Mirrors QueueItemCacheOrderingTests.
//
// -OrderBy is offered via JobOrderableItems but is a completer, not a
// ValidateSet, so a value outside that set reaches the sort. Before the default
// arm was added, such a value matched no case and the switch fell through with
// no output -- every cached job silently dropped. The central assertion is that
// no input is ever dropped.
public class JobCacheOrderingTests
{
    private static Job Item(long id, DateTime? creation = null, DateTime? start = null) =>
        new() { Id = id, CreationTime = creation, StartTime = start };

    private static readonly Job[] Sample =
    [
        Item(3, creation: new DateTime(2026, 1, 3)),
        Item(1, creation: new DateTime(2026, 1, 1)),
        Item(2, creation: new DateTime(2026, 1, 2)),
    ];

    [Theory]
    [InlineData("CreationTime")]          // the default value injected by the cmdlet
    [InlineData("Release/Name")]
    [InlineData("State")]
    [InlineData("SpecificPriorityValue")]
    [InlineData("StartTime")]
    [InlineData("EndTime")]
    [InlineData("SourceType")]
    [InlineData("Id")]                    // not in JobOrderableItems -> unlisted
    [InlineData("NoSuchField")]           // arbitrary unlisted value
    [InlineData(null)]
    [InlineData("")]
    public void EveryOrderBy_EmitsAllItems(string? orderBy)
    {
        var result = OrderJobsForOutput(Sample, orderBy, ascending: false).ToList();
        Assert.Equal(Sample.Length, result.Count); // the bug emitted 0
    }

    [Fact]
    public void DefaultOrderBy_CreationTime_IsDescendingByDefault()
    {
        var result = OrderJobsForOutput(Sample, "CreationTime", ascending: false).ToList();
        Assert.Equal(new long?[] { 3, 2, 1 }, result.Select(j => j.Id));
    }

    [Fact]
    public void UnlistedOrderBy_FallsBackToCreationTimeOrdering()
    {
        var result = OrderJobsForOutput(Sample, "NoSuchField", ascending: false).ToList();
        Assert.Equal(new long?[] { 3, 2, 1 }, result.Select(j => j.Id));
    }

    [Fact]
    public void CreationTime_Ascending_WhenOrderAscending()
    {
        var result = OrderJobsForOutput(Sample, "CreationTime", ascending: true).ToList();
        Assert.Equal(new long?[] { 1, 2, 3 }, result.Select(j => j.Id));
    }

    [Fact]
    public void StartTime_OrdersByStartTime_NotCreationTime()
    {
        var items = new[]
        {
            Item(1, creation: new DateTime(2026, 1, 1), start: new DateTime(2026, 2, 3)),
            Item(2, creation: new DateTime(2026, 1, 2), start: new DateTime(2026, 2, 1)),
            Item(3, creation: new DateTime(2026, 1, 3), start: new DateTime(2026, 2, 2)),
        };
        var asc = OrderJobsForOutput(items, "StartTime", ascending: true).ToList();
        Assert.Equal(new long?[] { 2, 3, 1 }, asc.Select(j => j.Id));
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty_NoThrow()
    {
        Assert.Empty(OrderJobsForOutput([], "CreationTime", ascending: false));
    }
}
