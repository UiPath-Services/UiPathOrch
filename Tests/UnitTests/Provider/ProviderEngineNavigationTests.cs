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
    private System.Collections.Generic.List<string> Strs(string script) =>
        System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(_h.Run(script), o => (string)o.BaseObject));

    [Fact]
    public void Get_ChildItem_lists_only_top_level_folders()
    {
        // dir returns FOLDERS at depth 1 only (no nested folders, no non-folder entities).
        var names = Strs($@"(Get-ChildItem Test:{S}).DisplayName");
        Assert.Contains("Shared", names);
        Assert.Contains("Empty", names);
        Assert.Contains("Production", names);
        Assert.DoesNotContain("Sub", names);    // Shared/Sub is nested — not listed without -Recurse
        Assert.DoesNotContain("SubA", names);
    }

    [Fact]
    public void Get_ChildItem_Recurse_includes_nested_folders()
    {
        var fqns = Strs($@"(Get-ChildItem Test:{S} -Recurse).FullyQualifiedName");
        Assert.Contains("Shared", fqns);
        Assert.Contains("Shared/Sub", fqns);
        Assert.Contains("Production/SubA", fqns);
    }

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
    public void Resolution_is_case_insensitive_and_yields_the_catalog_cased_object()
    {
        // The resolved PATH STRING keeps the typed casing (a PowerShell engine detail the provider
        // does not control), but resolution itself is case-insensitive and the resolved Folder
        // OBJECT carries the catalog's canonical casing — which is what callers actually consume.
        Assert.True(Bool($@"Test-Path Test:{S}shared"));
        Assert.True(Bool($@"Test-Path Test:{S}SHARED{S}sub"));
        Assert.Equal("Shared", Str($@"(Get-Item Test:{S}SHARED).FullyQualifiedName"));
        Assert.Equal("Sub", Str($@"(Get-Item Test:{S}shared{S}SUB).DisplayName"));
    }

    [Fact]
    public void NormalizeRelativePath_relativizes_a_top_level_item_against_the_drive_root()
    {
        // Regression: the tab completer relativizes a child's absolute path against the current
        // location via NormalizeRelativePath. At the DRIVE ROOT this must return the bare leaf
        // ("Shared"), not the full drive-qualified path ("Test:\Shared") — otherwise `cd <tab>` at
        // the root completes to ".\Test:\Shared" instead of ".\Shared". (The drive-root GetParentPath
        // re-rooting broke the base NavigationCmdletProvider's parent-walk relativization; nested base
        // paths were unaffected.) Mirrors FileSystemProvider: `C:\Windows` vs `C:\` -> `Windows`.
        Assert.Equal("Shared",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}Shared','Test:{S}')"));
        Assert.Equal($"Production{S}SubA",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}Production{S}SubA','Test:{S}')"));
        // A nested base path already relativized correctly and must stay correct.
        Assert.Equal("SubA",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}Production{S}SubA','Test:{S}Production')"));
    }

    [Fact]
    public void NormalizeRelativePath_canonicalizes_casing_from_the_catalog_like_FileSystem()
    {
        // FileSystemProvider canonicalizes a mis-cased path to the on-disk casing (C:\WINDOWS vs C:\
        // -> "Windows"). Mirror that from the catalog, keyed off the FULL path so the canonical
        // casing of every segment is applied.
        Assert.Equal("Shared",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}SHARED','Test:{S}')"));
        Assert.Equal($"Production{S}SubA",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}PRODUCTION{S}suba','Test:{S}')"));
        Assert.Equal("SubA",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}production{S}SUBA','Test:{S}Production')"));
    }

    [Fact]
    public void NormalizeRelativePath_tolerates_a_null_basePath()
    {
        // Regression: the engine passes a NULL basePath in some contexts (e.g. Remove-Item -Recurse
        // while the current location is on the drive). NormalizeRelativePath must not NullReference —
        // the full-path casing change called PSPathToOrchPath(basePath) without guarding null, which
        // threw out of path normalization and silently broke Remove-Item -Recurse on the drive.
        // With no base to relativize against, the full path is returned — exactly as FileSystemProvider
        // does (NormalizeRelativePath('C:\Windows', $null) -> 'C:\Windows').
        Assert.Equal($"Test:{S}Shared",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}Shared', $null)"));
    }

    [Fact]
    public void NormalizeRelativePath_self_case_canonicalizes_the_leaf_casing_like_FileSystem()
    {
        // path == basePath relativizes to "..\<leaf>" (both providers). FileSystemProvider
        // canonicalizes the leaf casing there (C:\WINDOWS vs C:\WINDOWS -> "..\Windows"); mirror it
        // so a mis-cased self path yields the catalog casing, not the typed casing.
        Assert.Equal($"..{S}Shared",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}SHARED','Test:{S}SHARED')"));
        Assert.Equal($"..{S}SubA",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}production{S}suba','Test:{S}PRODUCTION{S}SUBA')"));
    }

    [Fact]
    public void NormalizeRelativePath_nested_leaf_is_not_clobbered_by_a_same_named_top_level_folder()
    {
        // Regression: relativizing "Production\Sub" against "Production" must yield the nested leaf
        // "Sub" — NOT be rewritten to an unrelated top-level folder that merely shares the leaf's
        // name case-insensitively (here a lower-cased "sub"). The old casing-canonicalization block
        // re-looked-up the RELATIVE result as if it were a full path and returned "sub".
        _h.Seed(new[]
        {
            OrchProviderHarness.F("sub", 9, null),            // an unrelated top-level folder "sub"
            OrchProviderHarness.F("Production", 3, null),
            OrchProviderHarness.F("Production/Sub", 33, 3),   // the nested "Sub" being relativized
        });
        Assert.Equal("Sub",
            Str($@"$ExecutionContext.SessionState.Path.NormalizeRelativePath('Test:{S}Production{S}Sub','Test:{S}Production')"));
    }

    [Fact]
    public void Tab_completion_of_cd_at_drive_root_yields_drive_relative_paths()
    {
        // The user-facing symptom: `cd <tab>` at the drive root must offer ".\Shared", not the
        // drive-qualified ".\Test:\Shared".
        var texts = Strs($@"Push-Location; Set-Location Test:{S}; " +
            $@"(TabExpansion2 -inputScript 'cd ' -cursorColumn 3).CompletionMatches | ForEach-Object CompletionText; " +
            $@"Pop-Location");
        Assert.Contains($".{S}Shared", texts);
        Assert.DoesNotContain(texts, t => t.Contains($"Test:{S}"));
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

    [Fact]
    public void PSPath_round_trips_through_LiteralPath_for_a_plain_name()
    {
        // Baseline: an item's emitted PSPath rebinds to the same item via -LiteralPath.
        string back = Str($@"$i = Get-Item Test:{S}Shared; (Get-Item -LiteralPath $i.PSPath).FullyQualifiedName");
        Assert.Equal("Shared", back);
    }

    [Fact]
    public void PSPath_round_trips_through_LiteralPath_for_a_wildcard_named_folder()
    {
        // The real guard: a name containing a wildcard metacharacter ('*') must survive the
        // emit -> PSPath -> -LiteralPath rebind round-trip. The provider must emit the PSPath RAW
        // while -LiteralPath binding re-applies WildcardPattern.Escape (EffectivePath); if the emit
        // side pre-escapes, the name double-escapes and the rebind fails to resolve.
        _h.Seed(new[] { OrchProviderHarness.F("Fin*ce", 5, null) });
        string back = Str($@"$i = Get-Item -LiteralPath 'Test:{S}Fin*ce'; (Get-Item -LiteralPath $i.PSPath).FullyQualifiedName");
        Assert.Equal("Fin*ce", back);
    }
}
