using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Covers the Path-shape coercion in OrchestratorPSCmdlet.GetTargetDriveNames.
//
// The smart-default flow (see OrchCmdlets.cs) hands the bound -Path value to
// ExtractDriveNamesFromBoundPath; subclasses lean on this helper rather than
// reimplementing Path parsing, so its contract -- accept string / string[] /
// other IEnumerable<string>, skip empties, parse drive prefix -- needs an
// explicit lock-in. The behavior it serves: only flush PendingWarning for
// drives this cmdlet actually touches (the regression fix for the
// "Orch1:\> Get-OrchAsset" -> warnings about unrelated 'local:\' noise).
public class TargetDriveNamesTests
{
    [Fact]
    public void NullPathObject_ProducesNoDrives()
    {
        Assert.Empty(OrchestratorPSCmdlet.ExtractDriveNamesFromBoundPath(null));
    }

    [Fact]
    public void SingleStringPath_ParsesDriveName()
    {
        var result = OrchestratorPSCmdlet.ExtractDriveNamesFromBoundPath("Orch1:\\Shared");
        Assert.Equal(new[] { "Orch1" }, result);
    }

    [Fact]
    public void StringArrayPath_ParsesEachDrive()
    {
        var result = OrchestratorPSCmdlet.ExtractDriveNamesFromBoundPath(
            new[] { "Orch1:\\Shared", "Orch2:\\Foo\\Bar", "Local:" });
        Assert.Equal(new[] { "Orch1", "Orch2", "Local" }, result);
    }

    [Fact]
    public void EmptyAndUnparseableEntries_AreSkipped()
    {
        // Empty strings, null entries, and non-PSDrive paths produce no
        // drive name -- the caller's target set stays clean.
        var result = OrchestratorPSCmdlet.ExtractDriveNamesFromBoundPath(
            new[] { "", "Orch1:\\Shared", "NoDrive", null! });
        Assert.Equal(new[] { "Orch1" }, result);
    }

    [Fact]
    public void NonStringObject_ProducesNoDrives()
    {
        // BoundParameters can hold any object the binder produced; an int /
        // bool / custom type means "not a Path-shaped value" -- skip it.
        Assert.Empty(OrchestratorPSCmdlet.ExtractDriveNamesFromBoundPath(42));
        Assert.Empty(OrchestratorPSCmdlet.ExtractDriveNamesFromBoundPath(true));
    }
}
