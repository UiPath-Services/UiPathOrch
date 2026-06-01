using System.Linq;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Add-PmGroupLicense resolves a group name against the directory
// Search API, which matches by PREFIX (startsWith) — so "ほえほえグループ" also
// returns "ほえほえグループ2". FilterDirectoryGroupsByName must narrow that to
// the requested name so a Get-PmGroupLicense -ExportCsv row binds back to
// exactly one group. Verified against the live Orch1 directory (the 2-group
// prefix collision is real, 2026-05-31).
public class AddPmLicenseGroupResolutionTests
{
    private static PmDirectoryEntityInfo G(string name, string objectType = "DirectoryGroup") =>
        new() { identityName = name, identifier = name, objectType = objectType };

    private static string[] Names(System.Collections.Generic.IEnumerable<PmDirectoryEntityInfo> r) =>
        r.Select(g => g.identityName!).OrderBy(n => n).ToArray();

    [Fact]
    public void BareName_MatchesExactly_NotPrefixSibling()
    {
        // The exact bug: prefix search returned both; only the exact one binds.
        var dir = new[] { G("ほえほえグループ"), G("ほえほえグループ2") };
        var r = AddPmLicenseToPmLicenseGroup.FilterDirectoryGroupsByName(dir, "ほえほえグループ");
        Assert.Equal(new[] { "ほえほえグループ" }, Names(r));
    }

    [Fact]
    public void BareName_IsCaseInsensitive()
    {
        var dir = new[] { G("Developers") };
        var r = AddPmLicenseToPmLicenseGroup.FilterDirectoryGroupsByName(dir, "developers");
        Assert.Equal(new[] { "Developers" }, Names(r));
    }

    [Fact]
    public void Wildcard_MatchesAllSiblings()
    {
        // [SupportsWildcards] is preserved: an explicit pattern still fans out.
        var dir = new[] { G("ほえほえグループ"), G("ほえほえグループ2"), G("Other") };
        var r = AddPmLicenseToPmLicenseGroup.FilterDirectoryGroupsByName(dir, "ほえほえ*");
        Assert.Equal(new[] { "ほえほえグループ", "ほえほえグループ2" }, Names(r));
    }

    [Fact]
    public void NonGroupObjectTypes_AreExcluded()
    {
        // A user or app whose name happens to match must never be licensed as a group.
        var dir = new[]
        {
            G("Sales", "DirectoryGroup"),
            G("Sales", "DirectoryUser"),
            G("Sales", "Application"),
            G("Sales", "LocalGroup"),
        };
        var r = AddPmLicenseToPmLicenseGroup.FilterDirectoryGroupsByName(dir, "Sales").ToList();
        Assert.Equal(2, r.Count); // DirectoryGroup + LocalGroup only
        Assert.All(r, g => Assert.Contains(g.objectType, new[] { "DirectoryGroup", "LocalGroup" }));
    }

    [Fact]
    public void NoMatch_ReturnsEmpty()
    {
        var dir = new[] { G("Alpha"), G("Beta") };
        Assert.Empty(AddPmLicenseToPmLicenseGroup.FilterDirectoryGroupsByName(dir, "Gamma"));
    }
}
