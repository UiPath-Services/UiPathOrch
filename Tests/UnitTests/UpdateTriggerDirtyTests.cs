using System;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Exhaustive per-field change detection for Update-OrchTrigger's pure core
// (UpdateTriggerCmdlet.ComputeTriggerUpdate). Every field that can flip the dirty flag / land
// in the PUT payload is asserted both directions: the current value is a no-op, a different
// value writes. No API — the name->id / robot-deserialization resolution is done by the cmdlet
// and passed in as already-resolved inputs.
public class ComputeTriggerUpdate_EveryFieldTests
{
    private static readonly DateTime BaseStop = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ProcessSchedule Baseline() => new()
    {
        Id = 1,
        Name = "Trig",
        Enabled = true,
        StartStrategy = 1,
        StopStrategy = "SoftStop",
        StopProcessExpression = "spx",
        KillProcessExpression = "kpx",
        AlertPendingExpression = "ape",
        AlertRunningExpression = "are",
        ConsecutiveJobFailuresThreshold = 3,
        JobFailuresGracePeriodInHours = 2,
        RuntimeType = "Unattended",
        InputArguments = "{}",
        ResumeOnSameContext = false,
        RunAsMe = false,
        IsConnected = true,
        ActivateOnJobComplete = false,
        ItemsActivationThreshold = 5,
        ItemsPerJobActivationTarget = 7,
        MaxJobsForActivation = 9,
        StartProcessCron = "0 0 * * *",
        StartProcessCronDetails = "{}",
        SpecificPriorityValue = 50,
        TimeZoneId = "UTC",
        CalendarId = 11,
        ReleaseId = 22,
        QueueDefinitionId = 33,
        StopProcessDate = BaseStop,
        MachineRobots = new[] { new MachineRobotSession { MachineId = 1, RobotId = 2, SessionId = 3 } },
        ExecutorRobots = new[] { new RobotExecutor { Id = 2 } },
    };

    private static void AssertField(UpdateTriggerCmdlet.TriggerUpdateInputs unchanged, UpdateTriggerCmdlet.TriggerUpdateInputs changed)
    {
        var s1 = Baseline();
        Assert.False(UpdateTriggerCmdlet.ComputeTriggerUpdate(OrchCollectionExtensions.DeepCopy(s1), s1, unchanged),
            "expected NO write when the value equals the current one");
        var s2 = Baseline();
        Assert.True(UpdateTriggerCmdlet.ComputeTriggerUpdate(OrchCollectionExtensions.DeepCopy(s2), s2, changed),
            "expected a write when the value differs from the current one");
    }

    [Fact] public void NewName() => AssertField(new() { NewName = "Trig" }, new() { NewName = "Renamed" });
    [Fact] public void Enabled() => AssertField(new() { Enabled = "true" }, new() { Enabled = "false" });
    [Fact] public void StartStrategy() => AssertField(new() { StartStrategy = 1 }, new() { StartStrategy = 2 });
    [Fact] public void StopStrategy() => AssertField(new() { StopStrategy = "SoftStop" }, new() { StopStrategy = "Kill" });
    [Fact] public void StopProcessExpression() => AssertField(new() { StopProcessExpression = "spx" }, new() { StopProcessExpression = "spx2" });
    [Fact] public void KillProcessExpression() => AssertField(new() { KillProcessExpression = "kpx" }, new() { KillProcessExpression = "kpx2" });
    [Fact] public void AlertPendingExpression() => AssertField(new() { AlertPendingExpression = "ape" }, new() { AlertPendingExpression = "ape2" });
    [Fact] public void AlertRunningExpression() => AssertField(new() { AlertRunningExpression = "are" }, new() { AlertRunningExpression = "are2" });
    [Fact] public void ConsecutiveJobFailuresThreshold() => AssertField(new() { ConsecutiveJobFailuresThreshold = 3 }, new() { ConsecutiveJobFailuresThreshold = 4 });
    [Fact] public void JobFailuresGracePeriodInHours() => AssertField(new() { JobFailuresGracePeriodInHours = 2 }, new() { JobFailuresGracePeriodInHours = 6 });
    [Fact] public void RuntimeType() => AssertField(new() { RuntimeType = "Unattended" }, new() { RuntimeType = "NonProduction" });
    [Fact] public void InputArguments() => AssertField(new() { InputArguments = "{}" }, new() { InputArguments = "{\"a\":1}" });
    [Fact] public void ResumeOnSameContext() => AssertField(new() { ResumeOnSameContext = "false" }, new() { ResumeOnSameContext = "true" });
    [Fact] public void RunAsMe() => AssertField(new() { RunAsMe = "false" }, new() { RunAsMe = "true" });
    [Fact] public void IsConnected() => AssertField(new() { IsConnected = "true" }, new() { IsConnected = "false" });
    [Fact] public void ActivateOnJobComplete() => AssertField(new() { ActivateOnJobComplete = "false" }, new() { ActivateOnJobComplete = "true" });
    [Fact] public void ItemsActivationThreshold() => AssertField(new() { ItemsActivationThreshold = 5 }, new() { ItemsActivationThreshold = 6 });
    [Fact] public void ItemsPerJobActivationTarget() => AssertField(new() { ItemsPerJobActivationTarget = 7 }, new() { ItemsPerJobActivationTarget = 8 });
    [Fact] public void MaxJobsForActivation() => AssertField(new() { MaxJobsForActivation = 9 }, new() { MaxJobsForActivation = 10 });
    [Fact] public void StartProcessCron() => AssertField(new() { StartProcessCron = "0 0 * * *" }, new() { StartProcessCron = "0 1 * * *" });
    [Fact] public void StartProcessCronDetails() => AssertField(new() { StartProcessCronDetails = "{}" }, new() { StartProcessCronDetails = "{\"x\":1}" });
    [Fact] public void SpecificPriorityValue() => AssertField(new() { SpecificPriorityValue = 50 }, new() { SpecificPriorityValue = 60 });
    [Fact] public void SpecificPriorityValueFromPriority() => AssertField(new() { SpecificPriorityValueFromPriority = 50 }, new() { SpecificPriorityValueFromPriority = 60 });
    [Fact] public void TimeZoneId() => AssertField(new() { TimeZoneId = "UTC" }, new() { TimeZoneId = "Tokyo Standard Time" });

    [Fact]
    public void CalendarId() => AssertField(
        new() { CalendarResolved = true, ResolvedCalendarId = 11 },
        new() { CalendarResolved = true, ResolvedCalendarId = 99 });

    [Fact]
    public void ReleaseId() => AssertField(
        new() { ReleaseResolved = true, ResolvedReleaseId = 22 },
        new() { ReleaseResolved = true, ResolvedReleaseId = 99 });

    [Fact]
    public void QueueDefinitionId() => AssertField(
        new() { QueueResolved = true, ResolvedQueueDefinitionId = 33 },
        new() { QueueResolved = true, ResolvedQueueDefinitionId = 99 });

    [Fact]
    public void TimeZoneId_Resolved() => AssertField(
        new() { TimeZoneResolved = true, ResolvedTimeZoneId = "UTC" },
        new() { TimeZoneResolved = true, ResolvedTimeZoneId = "Tokyo Standard Time" });

    [Fact]
    public void ResolvedId_NotResolved_IsNoOp()
    {
        // A supplied-but-unresolved name (resolved flag false) must never write, even with a
        // different id value present.
        var s = Baseline();
        Assert.False(UpdateTriggerCmdlet.ComputeTriggerUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateTriggerCmdlet.TriggerUpdateInputs { CalendarResolved = false, ResolvedCalendarId = 99 }));
    }

    [Fact]
    public void StopProcessDate() => AssertField(
        new() { UtcStopProcessDate = BaseStop },
        new() { UtcStopProcessDate = BaseStop.AddDays(1) });

    [Fact]
    public void MachineRobots() => AssertField(
        new() { MachineRobotsSpecified = true, ResolvedMachineRobots = new[] { new MachineRobotSession { MachineId = 1, RobotId = 2, SessionId = 3 } } },
        new() { MachineRobotsSpecified = true, ResolvedMachineRobots = new[] { new MachineRobotSession { MachineId = 1, RobotId = 9, SessionId = 3 } } });

    [Fact]
    public void ExecutorRobots() => AssertField(
        new() { ExecutorRobotsSpecified = true, ResolvedExecutorRobots = new[] { new RobotExecutor { Id = 2 } } },
        new() { ExecutorRobotsSpecified = true, ResolvedExecutorRobots = new[] { new RobotExecutor { Id = 9 } } });

    [Fact]
    public void MachineRobotsAlone_UnchangedSet_IsNoOp()
    {
        // -MachineRobots alone re-derives ExecutorRobots, but an unchanged set must not write.
        var s = Baseline();
        Assert.False(UpdateTriggerCmdlet.ComputeTriggerUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateTriggerCmdlet.TriggerUpdateInputs
            {
                MachineRobotsSpecified = true,
                ResolvedMachineRobots = new[] { new MachineRobotSession { MachineId = 1, RobotId = 2, SessionId = 3 } },
            }));
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        var s = Baseline();
        Assert.False(UpdateTriggerCmdlet.ComputeTriggerUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateTriggerCmdlet.TriggerUpdateInputs()));
    }
}
