using System;
using System.Collections.Generic;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Unit tests for OrchCollectionExtensions.AddTimeRange, extracted from the hand-built OData time
// filters in Get-OrchJob / Get-OrchQueueItem. Pins the exact "(Field ge/lt <utc>)" shape (UTC,
// millisecond precision, trailing Z), the inclusive ge / exclusive lt operators, the ge-before-lt
// order, UTC normalization, and the null-bound handling.
public class AddTimeRangeTests
{
    private static readonly DateTime After = new(2026, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc);
    private static readonly DateTime Before = new(2026, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

    [Fact]
    public void BothBounds_EmitsGeThenLt()
    {
        var f = new List<string>();
        f.AddTimeRange("DueDate", After, Before);
        Assert.Equal(
            new[]
            {
                "(DueDate ge 2026-01-02T03:04:05.123Z)",
                "(DueDate lt 2026-12-31T23:59:59.999Z)",
            },
            f);
    }

    [Fact]
    public void AfterOnly_EmitsGe()
    {
        var f = new List<string>();
        f.AddTimeRange("CreationTime", After, null);
        Assert.Equal(new[] { "(CreationTime ge 2026-01-02T03:04:05.123Z)" }, f);
    }

    [Fact]
    public void BeforeOnly_EmitsLt()
    {
        var f = new List<string>();
        f.AddTimeRange("EndTime", null, Before);
        Assert.Equal(new[] { "(EndTime lt 2026-12-31T23:59:59.999Z)" }, f);
    }

    [Fact]
    public void NeitherBound_EmitsNothing()
    {
        var f = new List<string>();
        f.AddTimeRange("StartProcessing", null, null);
        Assert.Empty(f);
    }

    [Fact]
    public void NormalizesToUtc()
    {
        // A non-UTC (Local) input representing the same instant must round-trip to the UTC string.
        var f = new List<string>();
        var local = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc).ToLocalTime();
        f.AddTimeRange("ResumeTime", local, null);
        Assert.Equal(new[] { "(ResumeTime ge 2026-06-09T12:00:00.000Z)" }, f);
    }

    [Fact]
    public void AppendsToExistingFilter()
    {
        var f = new List<string> { "(QueueDefinitionId eq 5)" };
        f.AddTimeRange("DeferDate", After, null);
        Assert.Equal(2, f.Count);
        Assert.Equal("(QueueDefinitionId eq 5)", f[0]);
    }
}
