using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Regression guard for the top-level New-Item name truncation: New-Item Orch1:\TestFixture_Base
// created a folder named "estFixture_Base" (first character dropped). Root cause was
// Substring(parentPath.Length + 1) assuming the parent never ends in a separator — but the
// 1.9.x GetParentPath drive-root re-rooting made the ROOT parent end in one ("Orch1:\"), so the
// +1 skipped the leaf's first character. LeafFromParent strips a boundary separator only when present.
public class NewItemLeafFromParentTests
{
    private static string P(string s) => s.Replace('\\', System.IO.Path.DirectorySeparatorChar);

    [Theory]
    // Top-level: parent is the drive root WITH a trailing separator — the regressing case.
    [InlineData("Orch1:\\TestFixture_Base", "Orch1:\\", "TestFixture_Base")]
    [InlineData("Orch1:\\Shared", "Orch1:\\", "Shared")]
    // Nested: parent has NO trailing separator — the previously-working case must stay working.
    [InlineData("Orch1:\\TestFixture_Base\\Development", "Orch1:\\TestFixture_Base", "Development")]
    [InlineData("Orch1:\\A\\B\\C", "Orch1:\\A\\B", "C")]
    // Legacy bare-separator root representation.
    [InlineData("\\Foo", "\\", "Foo")]
    public void LeafFromParent_ReturnsLeaf_WhetherOrNotParentHasTrailingSeparator(
        string path, string parent, string expected)
    {
        Assert.Equal(expected, OrchProvider.LeafFromParent(P(path), P(parent)));
    }

    [Fact]
    public void LeafFromParent_DoesNotDropFirstCharacterOfTopLevelName()
    {
        // The exact reported regression.
        Assert.Equal("TestFixture_Base",
            OrchProvider.LeafFromParent(P("Orch1:\\TestFixture_Base"), P("Orch1:\\")));
    }
}
