using System.Linq;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots — the web-parity
// derivation behind New/Update-OrchTrigger -MachineRobots.
//
// The web trigger dialog never sends MachineRobots alone: the same PUT/POST always
// carries ExecutorRobots ({Id} per distinct robot), and the server persists the
// schedule-to-robot relation (RobotUserName / the trigger screen's Account column /
// GetRobotIdsForSchedule) from ExecutorRobots, not from the MachineRobots rows.
// HAR-verified against OnPrem 22.10: a PUT carrying only MachineRobots stores RobotId
// but reads back with RobotUserName null and an empty Account display. ExecutorRobots
// is also the only assignment field API v11 has (MachineRobots joined the DTO by v15),
// so this derivation is what keeps -MachineRobots meaningful on old servers.
public class DeriveExecutorRobotsTests
{
    private static MachineRobotSession Mr(long? robotId, long? machineId = 1) =>
        new() { RobotId = robotId, MachineId = machineId };

    [Fact]
    public void NullInput_YieldsNull()
    {
        Assert.Null(OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots(null));
    }

    [Fact]
    public void EmptyInput_YieldsNull()
    {
        Assert.Null(OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots([]));
    }

    [Fact]
    public void RowsWithoutRobotId_AreIgnored_AllNullYieldsNull()
    {
        Assert.Null(OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots([Mr(null), Mr(null)]));
    }

    [Fact]
    public void SinglePair_YieldsSingleIdOnlyExecutor()
    {
        var result = OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots([Mr(5, 6)]);

        Assert.NotNull(result);
        var executor = Assert.Single(result!);
        Assert.Equal(5, executor.Id);
        // The web PUT sends {"Id": N} only — no name fields.
        Assert.Null(executor.Name);
        Assert.Null(executor.MachineName);
    }

    [Fact]
    public void SameRobotOnMultipleMachines_IsDeduplicated()
    {
        // One robot paired with two machines is one relation entry, like the web PUT.
        var result = OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots(
            [Mr(5, 6), Mr(5, 7), Mr(9, 6)]);

        Assert.NotNull(result);
        Assert.Equal([5L, 9L], result!.Select(r => r.Id!.Value).ToArray());
    }

    [Fact]
    public void RobotIdOrder_IsPreserved()
    {
        var result = OrchestratorPSCmdlet.DeriveExecutorRobotsFromMachineRobots(
            [Mr(9), Mr(5), Mr(7)]);

        Assert.Equal([9L, 5L, 7L], result!.Select(r => r.Id!.Value).ToArray());
    }
}
