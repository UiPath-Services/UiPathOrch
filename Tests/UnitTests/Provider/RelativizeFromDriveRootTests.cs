using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Pins PathTools.RelativizeFromDriveRoot — the drive-root half of NormalizeRelativePath, shared by
// OrchProvider and the DU/TM shadow providers.
//
// It exists because those providers re-root a top-level item's parent ("Orch1:" -> "Orch1:\", via
// ParentPathWithDriveRoot) so PSParentPath and the `dir` "Directory:" header match FileSystem. That
// re-rooting defeats the base NavigationCmdletProvider's parent-walk relativization, which compares
// against the trimmed "Orch1:" — it gives up and returns the path unrelativized, and tab completion
// renders the result as the unusable ".\Orch1:\Autopilot" instead of ".\Autopilot".
//
// That is exactly how the DU/TM shadow providers regressed: they adopted the re-rooting without the
// compensating relativization. The logic now lives in ONE place both providers call, and these
// tests pin its contract. See ProviderEngineNavigationTests for the OrchProvider end-to-end path.
public class RelativizeFromDriveRootTests
{
    private static readonly char S = System.IO.Path.DirectorySeparatorChar;

    [Fact]
    public void Drive_root_base_yields_the_bare_relative_path()
    {
        // THE REGRESSION CASE. The completer inserts this as ".\Autopilot".
        Assert.Equal("Autopilot", PathTools.RelativizeFromDriveRoot($"Orch1:{S}Autopilot", $"Orch1:{S}"));
        Assert.Equal("MyProject", PathTools.RelativizeFromDriveRoot($"Orch1Du:{S}MyProject", $"Orch1Du:{S}"));
        Assert.Equal("FUGA Proj", PathTools.RelativizeFromDriveRoot($"Orch1Du:{S}FUGA Proj", $"Orch1Du:{S}"));
    }

    [Fact]
    public void Nested_path_against_the_drive_root_keeps_its_full_relative_depth()
        => Assert.Equal($"Production{S}SubA",
            PathTools.RelativizeFromDriveRoot($"Orch1:{S}Production{S}SubA", $"Orch1:{S}"));

    [Fact]
    public void A_nested_base_is_not_ours_to_handle()
    {
        // Only the drive-root case defeats the base provider's walk; everything else relativizes
        // correctly through it (nested -> child, sibling -> "..\leaf"). null = fall through.
        Assert.Null(PathTools.RelativizeFromDriveRoot($"Orch1:{S}Production{S}SubA", $"Orch1:{S}Production"));
        Assert.Null(PathTools.RelativizeFromDriveRoot($"Orch1:{S}Shared", $"Orch1:{S}Production"));
    }

    [Theory]
    [InlineData(null, @"Orch1:\")]   // no path
    [InlineData("", @"Orch1:\")]
    [InlineData(@"Orch1:\Shared", null)]   // the engine really does pass a null basePath
    [InlineData(@"Orch1:\Shared", "")]     // (e.g. Remove-Item -Recurse from a location on the drive)
    public void Missing_path_or_base_falls_through(string? path, string? basePath)
        => Assert.Null(PathTools.RelativizeFromDriveRoot(path, basePath));

    [Fact]
    public void The_drive_root_itself_relativizes_to_empty()
        => Assert.Equal("", PathTools.RelativizeFromDriveRoot($"Orch1:{S}", $"Orch1:{S}"));
}
