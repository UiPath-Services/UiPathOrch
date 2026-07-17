using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;
using Inputs = UiPath.PowerShell.Commands.UpdateTestSetScheduleCmdlet.TestSetScheduleUpdateInputs;

namespace UnitTests;

// Per-field change-detection for Update-OrchTestSetSchedule (ComputeTestSetScheduleUpdate). The
// scalar fields get a both-direction test; TestSetId/CalendarId (resolved in the cmdlet, passed in)
// get same-id no-op, different-id write, and empty-string clear. The name->id resolution itself
// needs the live lists and stays in the cmdlet, so it is out of scope here.
public class UpdateTestSetScheduleDirtyTests
{
    private static TestSetSchedule Sched() => new()
    {
        Id = 5,
        Name = "sched",
        Description = "desc",
        Enabled = true,
        CronExpression = "0 0 * * *",
        TimeZoneId = "UTC",
        TestSetId = 10,
        CalendarId = 20,
    };

    private static (bool dirty, TestSetSchedule payload) Run(Inputs input)
    {
        var source = Sched();
        var payload = Sched();
        bool dirty = UpdateTestSetScheduleCmdlet.ComputeTestSetScheduleUpdate(payload, source, input);
        return (dirty, payload);
    }

    private static void AssertField(Inputs unchanged, Inputs changed)
    {
        Assert.False(Run(unchanged).dirty, "expected NO write when the value equals the current one");
        Assert.True(Run(changed).dirty, "expected a write when the value differs");
    }

    [Fact] public void NothingSpecified_IsNoOp() => Assert.False(Run(new()).dirty);

    [Fact] public void NewName() => AssertField(new() { NewName = "sched" }, new() { NewName = "renamed" });
    [Fact] public void Description() => AssertField(new() { Description = "desc" }, new() { Description = "changed" });
    [Fact] public void Enabled() => AssertField(new() { Enabled = "true" }, new() { Enabled = "false" });
    [Fact] public void CronExpression() => AssertField(new() { CronExpression = "0 0 * * *" }, new() { CronExpression = "0 1 * * *" });
    [Fact] public void TimeZoneId() => AssertField(new() { TimeZoneId = "UTC" }, new() { TimeZoneId = "Tokyo Standard Time" });

    // TestSet id — resolved in the cmdlet and passed in.
    [Fact] public void TestSet_SameId_IsNoOp() => Assert.False(Run(new() { ResolvedTestSetId = 10 }).dirty);

    [Fact]
    public void TestSet_DifferentId_Writes()
    {
        var r = Run(new() { ResolvedTestSetId = 11 });
        Assert.True(r.dirty);
        Assert.Equal(11, r.payload.TestSetId);
    }

    [Fact] public void TestSet_NotSpecified_IsNoOp() => Assert.False(Run(new()).dirty);

    [Fact]
    public void TestSet_EmptyClearWhenSet_Writes()
    {
        var r = Run(new() { TestSetCleared = true });
        Assert.True(r.dirty);
        Assert.Null(r.payload.TestSetId);
    }

    // Calendar id.
    [Fact] public void Calendar_SameId_IsNoOp() => Assert.False(Run(new() { ResolvedCalendarId = 20 }).dirty);

    [Fact]
    public void Calendar_DifferentId_Writes()
    {
        var r = Run(new() { ResolvedCalendarId = 21 });
        Assert.True(r.dirty);
        Assert.Equal(21, r.payload.CalendarId);
    }

    [Fact]
    public void Calendar_EmptyClearWhenSet_Writes()
    {
        var r = Run(new() { CalendarCleared = true });
        Assert.True(r.dirty);
        Assert.Null(r.payload.CalendarId);
    }
}
