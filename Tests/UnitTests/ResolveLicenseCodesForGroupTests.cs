using System.Management.Automation;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup — the pure
// -License resolver for groups. The available-bundles API returns codes only
// (its bundle `name` is empty on the live server, verified on Orch1 2026-05-31),
// so resolution must go through the static catalog: match a pattern against the
// friendly name OR the raw code, restricted to the codes the group can actually
// be offered. This is what makes Get-PmGroupLicense -ExportCsv (License =
// friendly name) round-trip into Add-PmGroupLicense.
public class ResolveLicenseCodesForGroupTests
{
    private static List<WildcardPattern> P(params string[] patterns) =>
        patterns.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

    // The exact round-trip case: the CSV emits the friendly name; it must resolve
    // back to the code, given that code is available to the group.
    [Fact]
    public void FriendlyName_RoundTripsToCode_WhenAvailable()
    {
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("Attended - Named User"), new[] { "ATTUNU", "RPADEVPRONU" });
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    [Fact]
    public void RawCode_AlsoMatches()
    {
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("RPADEVPRONU"), new[] { "ATTUNU", "RPADEVPRONU" });
        Assert.Equal(new HashSet<string> { "RPADEVPRONU" }, got);
    }

    [Fact]
    public void OnlyAvailableCodesAreReturned()
    {
        // Pattern matches the catalog, but the bundle isn't offered to this group.
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("Tester - Named User"), new[] { "ATTUNU" });
        Assert.Empty(got);
    }

    [Fact]
    public void Wildcard_MatchesAllAvailableMatches()
    {
        // "Automation Developer*" matches RPADEVPRONU (Automation Developer -
        // Named User) and RPADEVPROCU (… Multiuser); only the available one returns.
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("Automation Developer*"), new[] { "RPADEVPRONU", "ATTUNU" });
        Assert.Equal(new HashSet<string> { "RPADEVPRONU" }, got);
    }

    [Fact]
    public void CaseInsensitive_FriendlyName()
    {
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("attended - named user"), new[] { "ATTUNU" });
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    [Fact]
    public void UncataloguedAvailableCode_MatchableByRawCode()
    {
        // A future bundle the static catalog doesn't map can still be added by its
        // exact code (its friendly name falls back to the raw code).
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("ZZNEWBUNDLE"), new[] { "ZZNEWBUNDLE", "ATTUNU" });
        Assert.Equal(new HashSet<string> { "ZZNEWBUNDLE" }, got);
    }

    [Fact]
    public void NoMatch_ReturnsEmpty_NotNull()
    {
        var got = AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(
            P("NoSuchBundle*"), new[] { "ATTUNU" });
        Assert.NotNull(got);
        Assert.Empty(got);
    }

    [Fact]
    public void NullArgs_ReturnEmpty()
    {
        Assert.Empty(AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(null, new[] { "ATTUNU" }));
        Assert.Empty(AddPmLicenseToPmLicenseGroup.ResolveLicenseCodesForGroup(P("*"), null));
    }
}
