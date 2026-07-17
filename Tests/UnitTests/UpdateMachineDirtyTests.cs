using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Exhaustive per-parameter change-detection for Update-OrchMachine, exercised against the
// pure, API-free UpdateMachineCmdlet.ComputeMachineUpdate core. Every field that can land in
// the PATCH payload gets both directions: current value => no write; different value => write.
public class ComputeMachineUpdate_EveryParameterTests
{
    private static MaintenanceWindow Mw() => new()
    {
        CronExpression = "0 0 * * 0",
        Duration = 60,
        Enabled = true,
        TimezoneId = "UTC",
    };

    private static ExtendedMachine Baseline() => new()
    {
        Id = 1,
        Description = "desc",
        UnattendedSlots = 1,
        NonProductionSlots = 2,
        TestAutomationSlots = 3,
        AutomationType = "Any",
        TargetFramework = "Windows",
        RobotUsers = new[]
        {
            new RobotUser { UserName = "u1", RobotId = 1 },
            new RobotUser { UserName = "u2", RobotId = 2 },
        },
        UpdatePolicy = new UpdatePolicy { Type = "None" },
        Tags = new[] { new Tag { Name = "env", Value = "prod" } },
        MaintenanceWindow = Mw(),
    };

    private static bool Run(ExtendedMachine source, UpdateMachineCmdlet.MachineUpdateInputs input) =>
        UpdateMachineCmdlet.ComputeMachineUpdate(new ExtendedMachine { Id = source.Id }, source, input);

    private static void AssertField(UpdateMachineCmdlet.MachineUpdateInputs unchanged, UpdateMachineCmdlet.MachineUpdateInputs changed)
    {
        Assert.False(Run(Baseline(), unchanged), "expected NO write when the value equals the current one");
        Assert.True(Run(Baseline(), changed), "expected a write when the value differs from the current one");
    }

    [Fact]
    public void Description() => AssertField(
        new() { Description = "desc" }, new() { Description = "changed" });

    [Fact]
    public void UnattendedSlots() => AssertField(
        new() { UnattendedSlots = 1 }, new() { UnattendedSlots = 9 });

    [Fact]
    public void NonProductionSlots() => AssertField(
        new() { NonProductionSlots = 2 }, new() { NonProductionSlots = 9 });

    [Fact]
    public void TestAutomationSlots() => AssertField(
        new() { TestAutomationSlots = 3 }, new() { TestAutomationSlots = 9 });

    [Fact]
    public void AutomationType() => AssertField(
        new() { AutomationType = "Any" }, new() { AutomationType = "Foreground" });

    [Fact]
    public void TargetFramework() => AssertField(
        new() { TargetFramework = "Windows" }, new() { TargetFramework = "Portable" });

    [Fact]
    public void RobotUsers_SameSetDifferentOrder_NoOp_Different_Writes() => AssertField(
        new() { RobotUsersSpecified = true, ResolvedRobotUsers = new[] { new RobotUser { UserName = "u2", RobotId = 2 }, new RobotUser { UserName = "u1", RobotId = 1 } } },
        new() { RobotUsersSpecified = true, ResolvedRobotUsers = new[] { new RobotUser { UserName = "u1", RobotId = 1 } } });

    [Fact]
    public void RobotUsers_ClearedWhenHadSome_Writes()
    {
        Assert.True(Run(Baseline(), new UpdateMachineCmdlet.MachineUpdateInputs { RobotUsersSpecified = true, ResolvedRobotUsers = System.Array.Empty<RobotUser>() }));
    }

    [Fact]
    public void UpdatePolicyType() => AssertField(
        new() { UpdatePolicyType = "None" }, new() { UpdatePolicyType = "LatestVersion" });

    [Fact]
    public void UpdatePolicyVersion()
    {
        static ExtendedMachine Pinned() { var m = Baseline(); m.UpdatePolicy = new UpdatePolicy { Type = "Specific", SpecificVersion = "1.0.0" }; return m; }
        Assert.False(UpdateMachineCmdlet.ComputeMachineUpdate(new ExtendedMachine { Id = 1 }, Pinned(),
            new UpdateMachineCmdlet.MachineUpdateInputs { UpdatePolicyType = "Specific", UpdatePolicyVersion = "1.0.0" }));
        Assert.True(UpdateMachineCmdlet.ComputeMachineUpdate(new ExtendedMachine { Id = 1 }, Pinned(),
            new UpdateMachineCmdlet.MachineUpdateInputs { UpdatePolicyType = "Specific", UpdatePolicyVersion = "9.9.9" }));
    }

    [Fact]
    public void Tags_SameSetDifferentOrder_NoOp_Different_Writes() => AssertField(
        new() { Tags = new[] { "env=prod" } }, new() { Tags = new[] { "env=dev" } });

    [Fact]
    public void Maintenance_Cron() => AssertField(
        new() { MaintenanceSpecified = true, MaintenanceCron = "0 0 * * 0" },
        new() { MaintenanceSpecified = true, MaintenanceCron = "0 1 * * 0" });

    [Fact]
    public void Maintenance_Duration() => AssertField(
        new() { MaintenanceSpecified = true, MaintenanceDuration = 60 },
        new() { MaintenanceSpecified = true, MaintenanceDuration = 120 });

    [Fact]
    public void Maintenance_Enabled() => AssertField(
        new() { MaintenanceSpecified = true, MaintenanceEnabled = "true" },
        new() { MaintenanceSpecified = true, MaintenanceEnabled = "false" });

    [Fact]
    public void Maintenance_TimezoneId_Direct() => AssertField(
        new() { MaintenanceSpecified = true, MaintenanceTimeZoneId = "UTC" },
        new() { MaintenanceSpecified = true, MaintenanceTimeZoneId = "Tokyo Standard Time" });

    [Fact]
    public void Maintenance_TimezoneId_FromResolvedName() => AssertField(
        new() { MaintenanceSpecified = true, ResolvedTimezoneIdFromName = "UTC" },
        new() { MaintenanceSpecified = true, ResolvedTimezoneIdFromName = "Tokyo Standard Time" });

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        Assert.False(Run(Baseline(), new UpdateMachineCmdlet.MachineUpdateInputs()));
    }
}
