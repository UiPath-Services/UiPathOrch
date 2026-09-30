using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins MachineFieldVersions — the guard that keeps a runtime slot an older Orchestrator does not
// model out of the request — and UpdateMachineCmdlet.DescribeUnboundPayload, which translates the
// answer that comes back when something else in the body is the problem.
//
// The guard covers only the four newest slots on purpose. Probing a 21.10.4 (API 13.0) server
// showed the inherited "introduced in v15" notes to be wrong for AutomationType, TargetFramework
// and AutomationCloudSlots (that server accepts and applies them) and wrong in the other
// direction for MaintenanceWindow (placed at v13, rejected there), so those fields are sent and
// the failure is explained instead of being pre-empted from a table that cannot be trusted.
public class MachineFieldVersionsTests
{
    private static (int? value, List<string> warnings) Run(int? value, int introducedIn, double? apiVersion)
    {
        var warnings = new List<string>();
        var got = MachineFieldVersions.SupportedOrNull(value, introducedIn, apiVersion, "AppTestSlots", "Orch1:", warnings.Add);
        return (got, warnings);
    }

    [Fact]
    public void NotSpecified_StaysNull_AndIsSilent()
    {
        var (value, warnings) = Run(null, MachineFieldVersions.AppTestSlots, 18);
        Assert.Null(value);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ServerNewEnough_PassesThrough()
    {
        var (value, warnings) = Run(1, MachineFieldVersions.AppTestSlots, 19);
        Assert.Equal(1, value);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ServerTooOld_DropsTheSlot_AndNamesTheVersion()
    {
        var (value, warnings) = Run(1, MachineFieldVersions.AppTestSlots, 18);
        Assert.Null(value);
        var warning = Assert.Single(warnings);
        Assert.Contains("-AppTestSlots", warning);
        Assert.Contains("19.0", warning);
        Assert.Contains("18", warning);
        // Drive-scoped messages quote the target, as Set-OrchAsset / Add-DuUser do — without
        // the quotes a drive name ends in the colon the sentence needs ("local:\:").
        Assert.StartsWith("\"Orch1:\":", warning);
    }

    [Fact]
    public void ZeroIsAValue_NotAbsence()
    {
        // Clearing a slot is a real request, so it must be reported as dropped too.
        var (value, warnings) = Run(0, MachineFieldVersions.AppTestSlots, 18);
        Assert.Null(value);
        Assert.Single(warnings);
    }

    [Fact]
    public void FractionalApiVersion_ComparesNumerically()
    {
        // A 20.10.16 Orchestrator reports API 11.1, so the version is not an integer.
        var (value, warnings) = Run(1, MachineFieldVersions.AppTestSlots, 11.1);
        Assert.Null(value);
        Assert.Contains("11.1", Assert.Single(warnings));
    }

    [Fact]
    public void UnknownApiVersion_SendsTheValueAsIs()
    {
        // A drive not signed in yet has no version; AddMachine's strips read that the same way.
        var (value, warnings) = Run(1, MachineFieldVersions.AppTestSlots, null);
        Assert.Equal(1, value);
        Assert.Empty(warnings);
    }

    [Fact]
    public void OnlyTheVerifiedSlots_AreGated()
    {
        Assert.Equal(19, MachineFieldVersions.AppTestSlots);
        Assert.Equal(19, MachineFieldVersions.HostingSlots);
        Assert.Equal(20, MachineFieldVersions.PerformanceTestSlots);
        Assert.Equal(20, MachineFieldVersions.FunctionSlots);
    }

    [Fact]
    public void UnboundPayload_NamesTheFieldsAndTheVersion()
    {
        var payload = new ExtendedMachine { Id = 1, UnattendedSlots = 2, MaintenanceWindow = new MaintenanceWindow() };
        var hint = UpdateMachineCmdlet.DescribeUnboundPayload(
            new Exception("; machineDto must not be null"), payload, 13);

        Assert.NotNull(hint);
        Assert.Contains("MaintenanceWindow", hint);
        Assert.Contains("UnattendedSlots", hint);
        Assert.Contains("API 13", hint);
        // The API's own words belong to the error record, not to this warning.
        Assert.DoesNotContain("machineDto", hint);
    }

    [Fact]
    public void OtherFailures_AreLeftAlone()
    {
        var payload = new ExtendedMachine { Id = 1, UnattendedSlots = 2 };
        Assert.Null(UpdateMachineCmdlet.DescribeUnboundPayload(new Exception("License expired!"), payload, 13));
    }
}
