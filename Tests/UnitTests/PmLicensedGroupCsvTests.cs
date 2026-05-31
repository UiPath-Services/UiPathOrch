using System.Linq;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Get-OrchPmLicensedGroup -ExportCsv emits one row per (group, license) with the
// License column as the human-readable display name, so the CSV round-trips into
// Add-OrchPmLicenseToPmLicensedGroup (whose -License takes that display name).
// BuildLicenseDisplayNames is the pure core: codes -> display names, ordered,
// with unknown codes falling back to the raw code so nothing is dropped.
public class PmLicensedGroupCsvTests
{
    private static NuLicensedGroup Group(params string[] codes) =>
        new() { name = "G", userBundleLicenses = codes };

    [Fact]
    public void ConvertsCodesToDisplayNames()
    {
        var rows = GetUserLicenseGroup.BuildLicenseDisplayNames(Group("ATTUNU", "TSTNU"));
        Assert.Equal(new[] { "Attended - Named User", "Tester - Named User" }, rows);
    }

    [Fact]
    public void OrdersByDisplayName_NotCodeOrder()
    {
        // Input code order is RPADEVNU, ACCU; display names sort as
        // "Action Center - Multiuser" < "RPA Developer - Named User".
        var rows = GetUserLicenseGroup.BuildLicenseDisplayNames(Group("RPADEVNU", "ACCU"));
        Assert.Equal(new[] { "Action Center - Multiuser", "RPA Developer - Named User" }, rows);
    }

    [Fact]
    public void UnknownCode_FallsBackToRawCode()
    {
        // A bundle code this build doesn't map must survive as-is, not vanish.
        var rows = GetUserLicenseGroup.BuildLicenseDisplayNames(Group("ZZNEWBUNDLE"));
        Assert.Equal(new[] { "ZZNEWBUNDLE" }, rows);
    }

    [Fact]
    public void NoLicenses_ProducesNoRows()
    {
        Assert.Empty(GetUserLicenseGroup.BuildLicenseDisplayNames(Group()));
        Assert.Empty(GetUserLicenseGroup.BuildLicenseDisplayNames(new NuLicensedGroup { name = "G" }));
    }

    [Fact]
    public void OneRowPerLicense()
    {
        // Group×license fan-out: 3 licenses -> 3 rows.
        var rows = GetUserLicenseGroup.BuildLicenseDisplayNames(Group("ATTUNU", "TSTNU", "ACNU"));
        Assert.Equal(3, rows.Count);
    }
}
