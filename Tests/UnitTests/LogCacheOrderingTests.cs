using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Commands.GetLogCmdlet;

namespace UnitTests;

// Regression tests for OrderRobotLogsForOutput -- the sort applied on the
// no-filter cache-output path of Get-OrchLog. Mirrors QueueItemCacheOrderingTests.
//
// -OrderBy is offered via LogOrderableItems (TimeStamp, Level) but is a
// completer, not a ValidateSet, so a value outside that set reaches the sort.
// Before the default arm was added, such a value matched neither case and the
// switch fell through with no output -- every cached log silently dropped. The
// central assertion is that no input is ever dropped.
public class LogCacheOrderingTests
{
    private static Log Item(long id, DateTime? timeStamp = null, string? level = null) =>
        new() { Id = id, TimeStamp = timeStamp, Level = level };

    private static readonly Log[] Sample =
    [
        Item(3, timeStamp: new DateTime(2026, 1, 3)),
        Item(1, timeStamp: new DateTime(2026, 1, 1)),
        Item(2, timeStamp: new DateTime(2026, 1, 2)),
    ];

    [Theory]
    [InlineData("TimeStamp")]   // the default value injected by the cmdlet
    [InlineData("Level")]
    [InlineData("Id")]          // not in LogOrderableItems -> unlisted
    [InlineData("NoSuchField")] // arbitrary unlisted value
    [InlineData(null)]
    [InlineData("")]
    public void EveryOrderBy_EmitsAllItems(string? orderBy)
    {
        var result = OrderRobotLogsForOutput(Sample, orderBy, ascending: false).ToList();
        Assert.Equal(Sample.Length, result.Count); // the bug emitted 0
    }

    [Fact]
    public void DefaultOrderBy_TimeStamp_IsDescendingByDefault()
    {
        var result = OrderRobotLogsForOutput(Sample, "TimeStamp", ascending: false).ToList();
        Assert.Equal(new long?[] { 3, 2, 1 }, result.Select(l => l.Id));
    }

    [Fact]
    public void UnlistedOrderBy_FallsBackToTimeStampOrdering()
    {
        var result = OrderRobotLogsForOutput(Sample, "NoSuchField", ascending: false).ToList();
        Assert.Equal(new long?[] { 3, 2, 1 }, result.Select(l => l.Id));
    }

    [Fact]
    public void TimeStamp_Ascending_WhenOrderAscending()
    {
        var result = OrderRobotLogsForOutput(Sample, "TimeStamp", ascending: true).ToList();
        Assert.Equal(new long?[] { 1, 2, 3 }, result.Select(l => l.Id));
    }

    [Fact]
    public void Level_OrdersByLevel_NotTimeStamp()
    {
        var items = new[]
        {
            Item(1, timeStamp: new DateTime(2026, 1, 1), level: "Warn"),
            Item(2, timeStamp: new DateTime(2026, 1, 2), level: "Error"),
            Item(3, timeStamp: new DateTime(2026, 1, 3), level: "Info"),
        };
        var asc = OrderRobotLogsForOutput(items, "Level", ascending: true).ToList();
        // ordinal string sort: Error < Info < Warn
        Assert.Equal(new long?[] { 2, 3, 1 }, asc.Select(l => l.Id));
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty_NoThrow()
    {
        Assert.Empty(OrderRobotLogsForOutput([], "TimeStamp", ascending: false));
    }
}
