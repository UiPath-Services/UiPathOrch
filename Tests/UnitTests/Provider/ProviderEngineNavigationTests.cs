using Xunit;

namespace UnitTests;

// Tier-2: drives the REAL PowerShell engine globber against a seeded UiPathOrch drive (see
// OrchProviderHarness) — the engine<->override interactions the pure-helper unit tests can't reach:
// wildcard resolution routing through HasChildItems, Split-Path / GetParentPath drive-root
// re-rooting, and Get-Item PSParentPath. Paths are built with the OS separator so the Linux/macOS
// CI legs exercise the same assertions.
public class ProviderEngineNavigationTests : IClassFixture<OrchProviderHarness>
{
    private readonly OrchProviderHarness _h;
    private static readonly string S = System.IO.Path.DirectorySeparatorChar.ToString();

    public ProviderEngineNavigationTests(OrchProviderHarness h)
    {
        _h = h;
        // Tree:  Shared(1) -> Shared/Sub(11)
        //        Empty(2)                       (no children)
        //        Production(3) -> Production/SubA(31)
        _h.Seed(new[]
        {
            OrchProviderHarness.F("Shared", 1, null),
            OrchProviderHarness.F("Shared/Sub", 11, 1),
            OrchProviderHarness.F("Empty", 2, null),
            OrchProviderHarness.F("Production", 3, null),
            OrchProviderHarness.F("Production/SubA", 31, 3),
        });
    }

    private string Str(string script) => (string)_h.Run(script)[0].BaseObject;
    private bool Bool(string script) => (bool)_h.Run(script)[0].BaseObject;

    [Fact]
    public void TestPath_resolves_existing_and_missing_folders_via_ItemExists()
    {
        Assert.True(Bool($@"Test-Path Test:{S}Shared"));
        Assert.True(Bool($@"Test-Path Test:{S}Shared{S}Sub"));
        Assert.True(Bool($@"Test-Path Test:{S}Production{S}SubA"));
        Assert.False(Bool($@"Test-Path Test:{S}Nope"));
        Assert.False(Bool($@"Test-Path Test:{S}Shared{S}Missing"));
    }

    [Fact]
    public void Wildcard_resolution_routes_through_HasChildItems_and_GetChildNames()
    {
        // Top-level wildcard: HasChildItems(root)=true -> GetChildNames lists depth-1 -> filter "Shar*".
        var top = _h.Run($@"(Resolve-Path Test:{S}Shar*).Path");
        Assert.Single(top);
        Assert.EndsWith($"Test:{S}Shared", (string)top[0].BaseObject);

        // Child wildcard: HasChildItems(Shared)=true -> Shared/Sub.
        Assert.EndsWith($"Test:{S}Shared{S}Sub", Str($@"(Resolve-Path Test:{S}Shared{S}*).Path"));

        // Sibling subtree resolves independently (GetChildNames keys off ParentId, not a name prefix).
        Assert.EndsWith($"Test:{S}Production{S}SubA", Str($@"(Resolve-Path Test:{S}Production{S}*).Path"));
    }

    [Fact]
    public void Wildcard_enumerates_a_populated_folder_but_not_a_childless_one()
    {
        // Positive control: a populated folder's children are enumerated (HasChildItems=true).
        Assert.NotEmpty(_h.RunAllowErrors($@"Resolve-Path Test:{S}Shared{S}* | ForEach-Object Path", out _));

        // HasChildItems(Empty)=false -> the globber does NOT enumerate -> zero matches. The
        // constant-false regression (52bea61a) made EVERY folder look childless and broke all wildcard
        // resolution; accurate HasChildItems keeps the empty result scoped to genuinely empty folders.
        Assert.Empty(_h.RunAllowErrors($@"Resolve-Path Test:{S}Empty{S}* | ForEach-Object Path", out _));
    }

    [Fact]
    public void Split_Path_matches_FileSystem_provider_rerooting()
    {
        // A top-level item's parent re-roots to "Test:\" WITH the separator (GetParentPath override),
        // matching FileSystemProvider; a nested parent has no trailing separator.
        Assert.Equal($"Test:{S}", Str($@"Split-Path Test:{S}Shared -Parent"));
        Assert.Equal($"Test:{S}Shared", Str($@"Split-Path Test:{S}Shared{S}Sub -Parent"));
        Assert.Equal("Shared", Str($@"Split-Path Test:{S}Shared -Leaf"));
    }

    [Fact]
    public void Get_Item_PSParentPath_keeps_the_drive_root_separator()
    {
        // The 1.9.x "Directory:" header / PSParentPath regression, end-to-end through the engine:
        // a top-level item's parent must be "Test:\" (WITH separator), not the bare "Test:".
        Assert.EndsWith($"Test:{S}", Str($@"(Get-Item Test:{S}Shared).PSParentPath"));
        Assert.EndsWith($"Test:{S}Shared", Str($@"(Get-Item Test:{S}Shared{S}Sub).PSParentPath"));
    }

    [Fact]
    public void Multi_segment_wildcard_globs_level_by_level()
    {
        // Test:\*\Sub -> only Shared has a child named exactly "Sub" (Production has "SubA", Empty none).
        // Exercises HasChildItems + GetChildNames at each level of the glob.
        var res = _h.Run($@"(Resolve-Path Test:{S}*{S}Sub).Path");
        Assert.Single(res);
        Assert.EndsWith($"Test:{S}Shared{S}Sub", (string)res[0].BaseObject);
    }

    [Fact]
    public void Test_Path_PathType_reflects_container_only_provider()
    {
        // Every item is a container (IsItemContainer == true); there are no leaves.
        Assert.True(Bool($@"Test-Path Test:{S}Shared -PathType Container"));
        Assert.False(Bool($@"Test-Path Test:{S}Shared -PathType Leaf"));
    }

    [Fact]
    public void Get_Item_emits_the_typed_Folder_with_provider_notes()
    {
        Assert.Equal("Shared", Str($@"(Get-Item Test:{S}Shared).DisplayName"));
        Assert.Equal("Shared", Str($@"(Get-Item Test:{S}Shared).FullyQualifiedName"));
        Assert.Equal("Sub", Str($@"(Get-Item Test:{S}Shared{S}Sub).DisplayName"));
        Assert.True(Bool($@"(Get-Item Test:{S}Shared).PSIsContainer"));
        Assert.Equal("Shared", Str($@"(Get-Item Test:{S}Shared).PSChildName"));
    }

    [Fact]
    public void LiteralPath_treats_wildcard_metacharacters_in_a_name_literally()
    {
        // A folder whose NAME contains a wildcard metacharacter ('*') must resolve literally via
        // -LiteralPath (ItemExists resolves the path with no wildcard interpretation) — and a name
        // that would only match it as a wildcard must NOT exist.
        _h.Seed(new[]
        {
            OrchProviderHarness.F("Fin*ce", 5, null),
            OrchProviderHarness.F("Plain", 6, null),
        });
        Assert.True(Bool($@"Test-Path -LiteralPath 'Test:{S}Fin*ce'"));
        Assert.False(Bool($@"Test-Path -LiteralPath 'Test:{S}Fince'"));   // '*' is literal, not "match zero+"
        Assert.False(Bool($@"Test-Path -LiteralPath 'Test:{S}FinXce'"));  // not a wildcard match either
    }
}
