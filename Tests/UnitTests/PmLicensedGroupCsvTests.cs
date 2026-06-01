using System.Linq;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;
using CsvLine = UiPath.PowerShell.Commands.CsvHelper.CsvLine;

namespace UnitTests;

// Get-PmGroupLicense -ExportCsv emits one row per (group, license) with the
// License column as the human-readable display name, so the CSV round-trips into
// Add-PmGroupLicense (whose -License takes that display name).
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

    // ---- Round trip: the row the exporter writes must read back into the same
    // Path/GroupName/License values that Add-PmGroupLicense binds.
    // Column index in CsvHeaders order: Path=0, GroupName=1, License=2.
    private const int Path = 0, GroupName = 1, License = 2;

    private static List<string> RoundTrip(string?[] row)
        => CsvLine.Split(string.Join(",", row));

    [Fact]
    public void PlainRow_RoundTripsToThreeColumns()
    {
        var f = RoundTrip(GetUserLicenseGroup.BuildLicenseCsvRow(
            @"Orch1:", "Developers", "Attended - Named User"));

        Assert.Equal(3, f.Count);
        Assert.Equal(@"Orch1:", f[Path]);
        Assert.Equal("Developers", f[GroupName]);
        Assert.Equal("Attended - Named User", f[License]);
    }

    [Fact]
    public void GroupName_WithComma_RoundTripsIntact_NoColumnShift()
    {
        // A comma in the group name must not push License into a 4th column.
        var f = RoundTrip(GetUserLicenseGroup.BuildLicenseCsvRow(
            @"Orch1:", "Finance, EMEA", "Tester - Named User"));

        Assert.Equal(3, f.Count);
        Assert.Equal("Finance, EMEA", f[GroupName]);
        Assert.Equal("Tester - Named User", f[License]);
    }

    [Fact]
    public void GroupName_WithQuote_RoundTripsIntact()
    {
        var f = RoundTrip(GetUserLicenseGroup.BuildLicenseCsvRow(
            @"Orch1:", "\"VIP\" group", "RPA Developer - Named User"));

        Assert.Equal(3, f.Count);
        Assert.Equal("\"VIP\" group", f[GroupName]);
        Assert.Equal("RPA Developer - Named User", f[License]);
    }

    [Fact]
    public void FullExportToImport_RoundTrip_BindsEveryLicense()
    {
        // End to end: a group's codes -> display names -> CSV rows -> read back.
        // Each parsed row carries the group identity plus exactly one license,
        // which is what Import-Csv | Add-PmGroupLicense consumes.
        var group = new NuLicensedGroup { name = "Ops", userBundleLicenses = ["ATTUNU", "ACNU"] };

        var parsed = GetUserLicenseGroup.BuildLicenseDisplayNames(group)
            .Select(lic => RoundTrip(GetUserLicenseGroup.BuildLicenseCsvRow(@"Orch1:", group.name, lic)))
            .ToList();

        Assert.All(parsed, f => Assert.Equal(3, f.Count));
        Assert.All(parsed, f => Assert.Equal("Ops", f[GroupName]));
        // Both licenses survive, as the display names the -License param accepts.
        Assert.Equal(
            new[] { "Action Center - Named User", "Attended - Named User" },
            parsed.Select(f => f[License]).OrderBy(x => x));
    }
}
