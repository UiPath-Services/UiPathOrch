using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;
using ReleaseUpdateInputs = UiPath.PowerShell.Commands.UpdateProcessCmdlet.ReleaseUpdateInputs;

namespace UnitTests;

// Per-field change-detection coverage for Update-OrchProcess's release PATCH payload
// (UpdateProcessCmdlet.ComputeReleaseUpdate). Every field that can land in the PATCH gets a
// both-direction test: the current value is a no-op, a different value writes. The two Retention
// updates and EntryPoint reassignment live outside this pure core (separate API / package feed)
// and are not exercised here.
public class ComputeReleaseUpdate_EveryFieldTests
{
    private static ProcessSettings Ps() => new()
    {
        ErrorRecordingEnabled = false,
        Duration = 40,
        Frequency = 500,
        Quality = 100,
        AutoStartProcess = false,
        AlwaysRunning = false,
        AutopilotForRobots = new AutopilotForRobotsSettings { Enabled = false, HealingEnabled = false },
    };

    private static VideoRecordingSettings Vrs() => new()
    {
        VideoRecordingType = "None",
        QueueItemVideoRecordingType = "None",
        MaxDurationSeconds = 180,
    };

    private static Release Baseline() => new()
    {
        Id = 5,
        Name = "Proc",
        Description = "desc",
        InputArguments = "{}",
        SpecificPriorityValue = 50,
        HiddenForAttendedUser = false,
        RemoteControlAccess = "None",
        ProcessVersion = "1.0.0",
        Tags = new[] { new Tag { Name = "env", Value = "prod" } },
        ProcessSettings = Ps(),
        VideoRecordingSettings = Vrs(),
    };

    private static void AssertField(ReleaseUpdateInputs unchanged, ReleaseUpdateInputs changed)
    {
        var s1 = Baseline();
        Assert.False(UpdateProcessCmdlet.ComputeReleaseUpdate(new Release { Id = s1.Id }, s1, unchanged),
            "expected NO write when the value equals the current one");
        var s2 = Baseline();
        Assert.True(UpdateProcessCmdlet.ComputeReleaseUpdate(new Release { Id = s2.Id }, s2, changed),
            "expected a write when the value differs from the current one");
    }

    [Fact] public void NewName() => AssertField(new() { NewName = "Proc" }, new() { NewName = "Renamed" });
    [Fact] public void Description() => AssertField(new() { Description = "desc" }, new() { Description = "other" });
    [Fact] public void InputArguments() => AssertField(new() { InputArguments = "{}" }, new() { InputArguments = "{\"a\":1}" });
    [Fact] public void SpecificPriorityValue() => AssertField(new() { SpecificPriorityValue = 50 }, new() { SpecificPriorityValue = 30 });
    [Fact] public void HiddenForAttendedUser() => AssertField(new() { HiddenForAttendedUser = "false" }, new() { HiddenForAttendedUser = "true" });
    [Fact] public void RemoteControlAccess() => AssertField(new() { RemoteControlAccess = "None" }, new() { RemoteControlAccess = "Full" });
    [Fact] public void Version() => AssertField(new() { Version = "1.0.0" }, new() { Version = "2.0.0" });
    [Fact] public void Tags() => AssertField(new() { Tags = new[] { "env=prod" } }, new() { Tags = new[] { "env=dev" } });

    // ProcessSettings sub-block
    [Fact] public void ErrorRecordingEnabled() => AssertField(new() { ErrorRecordingEnabled = "false" }, new() { ErrorRecordingEnabled = "true" });
    [Fact] public void Duration() => AssertField(new() { Duration = 40 }, new() { Duration = 60 });
    [Fact] public void Frequency() => AssertField(new() { Frequency = 500 }, new() { Frequency = 250 });
    [Fact] public void Quality() => AssertField(new() { Quality = 100 }, new() { Quality = 80 });
    [Fact] public void AutoStartProcess() => AssertField(new() { AutoStartProcess = "false" }, new() { AutoStartProcess = "true" });
    [Fact] public void AlwaysRunning() => AssertField(new() { AlwaysRunning = "false" }, new() { AlwaysRunning = "true" });
    [Fact] public void A4R_Enabled() => AssertField(new() { A4R_Enabled = "false" }, new() { A4R_Enabled = "true" });
    [Fact] public void A4R_HealingEnabled() => AssertField(new() { A4R_HealingEnabled = "false" }, new() { A4R_HealingEnabled = "true" });

    // VideoRecordingSettings sub-block
    [Fact] public void VideoRecordingType() => AssertField(new() { VideoRecordingType = "None" }, new() { VideoRecordingType = "Failed" });
    [Fact] public void QueueItemVideoRecordingType() => AssertField(new() { QueueItemVideoRecordingType = "None" }, new() { QueueItemVideoRecordingType = "Failed" });
    [Fact] public void MaxDurationSeconds() => AssertField(new() { MaxDurationSeconds = 180 }, new() { MaxDurationSeconds = 90 });

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        var s = Baseline();
        Assert.False(UpdateProcessCmdlet.ComputeReleaseUpdate(new Release { Id = s.Id }, s, new ReleaseUpdateInputs()));
    }

    [Fact]
    public void Tags_SameSetDifferentOrder_IsNoOp()
    {
        var s = Baseline();
        s.Tags = new[] { new Tag { Name = "a", Value = "1" }, new Tag { Name = "b", Value = "2" } };
        Assert.False(UpdateProcessCmdlet.ComputeReleaseUpdate(new Release { Id = s.Id }, s,
            new ReleaseUpdateInputs { Tags = new[] { "b=2", "a=1" } }));
    }
}
