using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Exhaustive per-field change-detection for Update-OrchApiTrigger's pure core
// (UpdateApiTriggerCmdlet.ComputeApiTriggerUpdate). For every field that can land in the
// UpdateHttpTrigger payload: the current value is a no-op, a different value writes. No API.
public class ComputeApiTriggerUpdate_EveryFieldTests
{
    private static HttpTrigger Baseline() => new()
    {
        Id = "trg-id",
        Name = "trg",
        Method = "POST",
        Slug = "s1",
        CallingMode = "cm1",
        RunAsCaller = false,
        Description = "desc",
        Enabled = true,
        RuntimeType = "Unattended",
        ResumeOnSameContext = false,
        StopStrategy = "SoftStop",
        StopJobAfterSeconds = 30,
        KillJobAfterSeconds = 60,
        AlertPendingJobAfterSeconds = 90,
        AlertRunningJobAfterSeconds = 120,
        RemoteControlAccess = "None",
        ConsecutiveJobFailuresThreshold = 5,
        InputArguments = "{}",
        ReleaseKey = "key-1",
        MachineRobots = new[] { new MachineRobotSession { MachineId = 1, RobotId = 2, SessionId = 3 } },
    };

    private static void AssertField(UpdateApiTriggerCmdlet.ApiTriggerUpdateInputs unchanged, UpdateApiTriggerCmdlet.ApiTriggerUpdateInputs changed)
    {
        var s1 = Baseline();
        Assert.False(UpdateApiTriggerCmdlet.ComputeApiTriggerUpdate(OrchCollectionExtensions.DeepCopy(s1), s1, unchanged),
            "expected NO write when the value equals the current one");
        var s2 = Baseline();
        Assert.True(UpdateApiTriggerCmdlet.ComputeApiTriggerUpdate(OrchCollectionExtensions.DeepCopy(s2), s2, changed),
            "expected a write when the value differs from the current one");
    }

    [Fact] public void NewName() => AssertField(new() { NewName = "trg" }, new() { NewName = "trg2" });
    [Fact] public void Method() => AssertField(new() { Method = "POST" }, new() { Method = "GET" });
    [Fact] public void Slug() => AssertField(new() { Slug = "s1" }, new() { Slug = "s2" });
    [Fact] public void CallingMode() => AssertField(new() { CallingMode = "cm1" }, new() { CallingMode = "cm2" });
    [Fact] public void RunAsCaller() => AssertField(new() { RunAsCaller = "false" }, new() { RunAsCaller = "true" });
    [Fact] public void Description() => AssertField(new() { Description = "desc" }, new() { Description = "desc2" });
    [Fact] public void Enabled() => AssertField(new() { Enabled = "true" }, new() { Enabled = "false" });
    [Fact] public void RuntimeType() => AssertField(new() { RuntimeType = "Unattended" }, new() { RuntimeType = "Attended" });
    [Fact] public void ResumeOnSameContext() => AssertField(new() { ResumeOnSameContext = "false" }, new() { ResumeOnSameContext = "true" });
    [Fact] public void StopStrategy() => AssertField(new() { StopStrategy = "SoftStop" }, new() { StopStrategy = "Kill" });
    [Fact] public void StopJobAfterSeconds() => AssertField(new() { StopJobAfterSeconds = 30 }, new() { StopJobAfterSeconds = 60 });
    [Fact] public void KillJobAfterSeconds() => AssertField(new() { KillJobAfterSeconds = 60 }, new() { KillJobAfterSeconds = 90 });
    [Fact] public void AlertPendingJobAfterSeconds() => AssertField(new() { AlertPendingJobAfterSeconds = 90 }, new() { AlertPendingJobAfterSeconds = 30 });
    [Fact] public void AlertRunningJobAfterSeconds() => AssertField(new() { AlertRunningJobAfterSeconds = 120 }, new() { AlertRunningJobAfterSeconds = 60 });
    [Fact] public void RemoteControlAccess() => AssertField(new() { RemoteControlAccess = "None" }, new() { RemoteControlAccess = "Full" });
    [Fact] public void ConsecutiveJobFailuresThreshold() => AssertField(new() { ConsecutiveJobFailuresThreshold = 5 }, new() { ConsecutiveJobFailuresThreshold = 10 });
    [Fact] public void InputArguments() => AssertField(new() { InputArguments = "{}" }, new() { InputArguments = "{\"a\":1}" });

    [Fact]
    public void MachineRobots_SameSetDifferentOrder_NoOp_DifferentSet_Writes() => AssertField(
        new()
        {
            MachineRobotsSpecified = true,
            ResolvedMachineRobots = new[]
            {
                new MachineRobotSession { MachineId = 1, RobotId = 2, SessionId = 3 },
            },
        },
        new()
        {
            MachineRobotsSpecified = true,
            ResolvedMachineRobots = new[]
            {
                new MachineRobotSession { MachineId = 1, RobotId = 2, SessionId = 4 }, // different SessionId
            },
        });

    [Fact]
    public void Release_SameKey_NoOp_DifferentKey_Writes() => AssertField(
        new() { ReleaseResolved = true, ResolvedReleaseKey = "key-1" },
        new() { ReleaseResolved = true, ResolvedReleaseKey = "key-2" });

    [Fact]
    public void Release_SpecifiedButUnresolved_IsNoOp()
    {
        // Name supplied but not found: AssignIdFromName wrote an error and never resolved, so
        // releaseResolved stays false and the trigger must not be written on that account.
        var s = Baseline();
        Assert.False(UpdateApiTriggerCmdlet.ComputeApiTriggerUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateApiTriggerCmdlet.ApiTriggerUpdateInputs { ReleaseResolved = false, ResolvedReleaseKey = null }));
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        var s = Baseline();
        Assert.False(UpdateApiTriggerCmdlet.ComputeApiTriggerUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateApiTriggerCmdlet.ApiTriggerUpdateInputs()));
    }
}
