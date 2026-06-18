using System;
using System.Reflection;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Regression guard for the `dir` "Directory:" header dropping the drive-root separator
// (it showed "Orch1:" instead of "Orch1:\").
//
// Root cause: GetChildName re-roots a bare drive (Orch1: -> Orch1:\) via
// PathTools.GetChildNameWithDriveRoot, but the symmetric GetParentPath was NOT overridden, so the
// base NavigationCmdletProvider trimmed a top-level item's parent to a bare "Orch1:". PSParentPath
// (and the Folder view's group header) then lost the separator.
//
// The logic lives in PathTools.ParentPathWithDriveRoot (pinned by PathToolsTests). This test pins
// the OTHER half — that the override is actually wired into BOTH providers. If someone deletes an
// override, GetParentPath falls back to NavigationCmdletProvider (DeclaringType changes) and this
// fails, catching the exact regression before it ships.
public class DriveRootParentPathRegressionTests
{
    // OrchProvider is covered directly by the behavioral tests below; the DU/TM shadow providers
    // can't be exercised as cheaply, so guard their wiring structurally (same regression: the
    // override being dropped makes GetParentPath fall back to NavigationCmdletProvider).
    [Fact]
    public void OrchShadowProviderBase_declares_a_GetParentPath_override()
        => AssertDeclaresGetParentPath(typeof(OrchShadowProviderBase<,>));

    private static void AssertDeclaresGetParentPath(Type providerType)
    {
        var method = providerType.GetMethod(
            "GetParentPath",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null);

        Assert.NotNull(method);
        // Declared on the provider itself, not inherited from NavigationCmdletProvider — i.e. the
        // override that routes base.GetParentPath through PathTools.ParentPathWithDriveRoot exists.
        Assert.Equal(providerType, method!.DeclaringType);
    }

    // Exposes the protected GetParentPath so the test can call it directly.
    private sealed class TestableOrchProvider : OrchProvider
    {
        public string ParentOf(string path) => GetParentPath(path, string.Empty);
    }

    // The direct test: the value the provider hands back for a top-level item's parent — which the
    // engine surfaces as PSParentPath and the Folder view's "Directory:" group header displays
    // (the GroupBy ScriptBlock only strips the "provider::" prefix) — must be "Orch1:\", not the
    // bare "Orch1:" the base provider produced before the fix.
    [Fact]
    public void Directory_header_for_a_top_level_item_is_drive_root_with_separator()
    {
        string sep = System.IO.Path.DirectorySeparatorChar.ToString();
        var provider = new TestableOrchProvider();

        string parent = provider.ParentOf($"Orch1:{sep}Shared");

        Assert.Equal($"Orch1:{sep}", parent); // "Orch1:\" — what "Directory:" shows
        Assert.NotEqual("Orch1:", parent);    // the pre-fix bug
    }

    // A nested item's parent is unaffected (no spurious trailing separator).
    [Fact]
    public void Directory_header_for_a_nested_item_is_the_parent_folder_without_trailing_separator()
    {
        string sep = System.IO.Path.DirectorySeparatorChar.ToString();
        var provider = new TestableOrchProvider();

        string parent = provider.ParentOf($"Orch1:{sep}Shared{sep}Sub");

        Assert.Equal($"Orch1:{sep}Shared", parent);
    }

    // End-to-end of New-Item's folder-name extraction: the engine hands NewItem the full path; NewItem
    // takes the parent via GetParentPath and strips it off to get the new folder's name (LeafFromParent).
    // The isolated LeafFromParentTests pin the math against a HARD-CODED parent; this pins the PAIR, so a
    // future change to GetParentPath's drive-root form (the re-rooting that caused the top-level
    // truncation — `New-Item Orch1:\TestFixture_Base` created "estFixture_Base") is caught here even if
    // LeafFromParent itself is left untouched. The top-level rows are the regressing case.
    [Theory]
    [InlineData("Orch1:\\TestFixture_Base", "TestFixture_Base")]  // top-level: parent is the drive root "Orch1:\"
    [InlineData("Orch1:\\Shared", "Shared")]                      // top-level
    [InlineData("Orch1:\\A\\B", "B")]                             // nested: parent "Orch1:\A" (no trailing sep)
    [InlineData("Orch1:\\A\\B\\C", "C")]                          // nested
    public void NewItem_leaf_derived_via_GetParentPath_is_the_folder_name(string path, string expectedLeaf)
    {
        string sep = System.IO.Path.DirectorySeparatorChar.ToString();
        path = path.Replace("\\", sep);
        var provider = new TestableOrchProvider();

        string parent = provider.ParentOf(path);                 // == GetParentPath(path, "")
        Assert.Equal(expectedLeaf, OrchProvider.LeafFromParent(path, parent));
    }
}
