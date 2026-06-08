using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Commands.GetQueueItemCmdlet;

namespace UnitTests;

// Regression tests for OrderQueueItemsForOutput -- the sort applied on the
// no-filter cache-output path of Get-OrchQueueItem.
//
// The bug this guards: -OrderBy defaults to "Id" (the Orchestrator web UI
// default for QueueItems, $orderby=Id desc), but the cache path switched only
// on the four date fields. "Id" matched no case and the switch fell through
// with no output, so `Get-OrchQueueItem` with no filter silently emitted
// nothing even when the cache was populated. Empirically reproduced live
// (20 cached items -> 0 emitted) before the fix.
//
// The core assertion every case must satisfy: the function never drops items.
public class QueueItemCacheOrderingTests
{
    private static QueueItem Item(long id, DateTime? due = null, DateTime? defer = null,
        DateTime? start = null, DateTime? end = null) =>
        new() { Id = id, DueDate = due, DeferDate = defer, StartProcessing = start, EndProcessing = end };

    private static readonly QueueItem[] Sample =
    [
        Item(3, due: new DateTime(2026, 1, 3)),
        Item(1, due: new DateTime(2026, 1, 1)),
        Item(2, due: new DateTime(2026, 1, 2)),
    ];

    // --- The regression itself --------------------------------------------

    [Theory]
    [InlineData("Id")]      // the default value injected by the cmdlet
    [InlineData(null)]      // null should never have reached here, but must be safe
    [InlineData("")]        // ditto for empty
    [InlineData("Status")]  // any unlisted / unexpected value must still emit
    public void DefaultAndUnlistedOrderBy_EmitAllItems(string? orderBy)
    {
        var result = OrderQueueItemsForOutput(Sample, orderBy, ascending: false).ToList();
        Assert.Equal(Sample.Length, result.Count); // the bug emitted 0
    }

    [Fact]
    public void DefaultOrderBy_Id_IsDescendingByDefault()
    {
        // Matches the web UI default $orderby=Id desc.
        var result = OrderQueueItemsForOutput(Sample, "Id", ascending: false).ToList();
        Assert.Equal(new long?[] { 3, 2, 1 }, result.Select(i => i.Id));
    }

    [Fact]
    public void Id_Ascending_WhenOrderAscending()
    {
        var result = OrderQueueItemsForOutput(Sample, "Id", ascending: true).ToList();
        Assert.Equal(new long?[] { 1, 2, 3 }, result.Select(i => i.Id));
    }

    [Fact]
    public void UnlistedOrderBy_FallsBackToIdOrdering()
    {
        // An unrecognized field must behave like "Id", not produce arbitrary order.
        var result = OrderQueueItemsForOutput(Sample, "NoSuchField", ascending: false).ToList();
        Assert.Equal(new long?[] { 3, 2, 1 }, result.Select(i => i.Id));
    }

    // --- The four date fields still work ----------------------------------

    [Theory]
    [InlineData("DueDate")]
    [InlineData("DeferDate")]
    [InlineData("StartProcessing")]
    [InlineData("EndProcessing")]
    public void DateFieldOrderBy_EmitAllItems(string orderBy)
    {
        var result = OrderQueueItemsForOutput(Sample, orderBy, ascending: false).ToList();
        Assert.Equal(Sample.Length, result.Count);
    }

    [Fact]
    public void DueDate_OrdersByDueDate_NotId()
    {
        var items = new[]
        {
            Item(1, due: new DateTime(2026, 1, 3)),
            Item(2, due: new DateTime(2026, 1, 1)),
            Item(3, due: new DateTime(2026, 1, 2)),
        };
        var asc = OrderQueueItemsForOutput(items, "DueDate", ascending: true).ToList();
        Assert.Equal(new long?[] { 2, 3, 1 }, asc.Select(i => i.Id));

        var desc = OrderQueueItemsForOutput(items, "DueDate", ascending: false).ToList();
        Assert.Equal(new long?[] { 1, 3, 2 }, desc.Select(i => i.Id));
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty_NoThrow()
    {
        Assert.Empty(OrderQueueItemsForOutput([], "Id", ascending: false));
    }
}
