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

    // The smart-default looks at two BoundParameters keys: "Path" (~88% of
    // cmdlets) and "Destination" (Copy-Orch* convention, 25 cmdlets). Tests
    // below lock in both keys are recognized, drive names from each are
    // included, and neither key alone or in combination is dropped.
    [Fact]
    public void BoundParameters_PathOnly_YieldsPathDrive()
    {
        var bound = new Dictionary<string, object> { ["Path"] = "Orch1:\\Shared" };
        var result = OrchestratorPSCmdlet
            .GetTargetDriveNamesFromBoundParameters(bound)
            .ToArray();
        Assert.Equal(new[] { "Orch1" }, result);
    }

    [Fact]
    public void BoundParameters_DestinationOnly_YieldsDestinationDrive()
    {
        // Copy-Orch* with -Destination but no -Path bound (e.g. running from
        // the source drive's current location).
        var bound = new Dictionary<string, object> { ["Destination"] = "Orch2:\\Shared" };
        var result = OrchestratorPSCmdlet
            .GetTargetDriveNamesFromBoundParameters(bound)
            .ToArray();
        Assert.Equal(new[] { "Orch2" }, result);
    }

    [Fact]
    public void BoundParameters_PathAndDestination_YieldsBoth()
    {
        // Full Copy-Orch* shape: source via -Path, destination via -Destination.
        var bound = new Dictionary<string, object>
        {
            ["Path"] = "Orch1:\\Shared",
            ["Destination"] = new[] { "Orch2:\\Shared", "Orch3:\\Backup" },
        };
        var result = OrchestratorPSCmdlet
            .GetTargetDriveNamesFromBoundParameters(bound)
            .ToArray();
        Assert.Equal(new[] { "Orch1", "Orch2", "Orch3" }, result);
    }

    [Fact]
    public void BoundParameters_DestinationLocalFilesystem_YieldsHarmlessC()
    {
        // Export-Orch* uses -Destination for a local fs path. ExtractDriveName
        // returns "C", which the BeginProcessing loop later filters out via
        // EnumAllOrchDrives. The helper itself doesn't filter — that's
        // intentional, so this test locks in the helper-side behavior and the
        // filter responsibility lives at the call site.
        var bound = new Dictionary<string, object>
        {
            ["Destination"] = "C:\\Downloads\\foo.nupkg",
        };
        var result = OrchestratorPSCmdlet
            .GetTargetDriveNamesFromBoundParameters(bound)
            .ToArray();
        Assert.Equal(new[] { "C" }, result);
    }

    [Fact]
    public void BoundParameters_OtherKeys_AreIgnored()
    {
        // The smart default only knows the two convention keys; cmdlets that
        // bind drive-path-shaped values to other names (-SourcePath, -Folder,
        // ...) must override GetTargetDriveNames themselves.
        var bound = new Dictionary<string, object>
        {
            ["SourcePath"] = "Orch1:\\Shared",
            ["Name"] = "MyAsset",
            ["Recurse"] = true,
        };
        var result = OrchestratorPSCmdlet
            .GetTargetDriveNamesFromBoundParameters(bound)
            .ToArray();
        Assert.Empty(result);
    }
}
