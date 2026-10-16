using System.Management.Automation;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Remove-PmGroupLicense resolves -License against what the GROUP holds, through the same
// helper Remove-PmUserLicense uses for a user's own bundles. These tests pin the two things
// that were wrong when it resolved against the static catalog instead:
//
//   * it matched the catalog's friendly NAME only, so a raw bundle code never matched and the
//     command removed nothing, silently -- the one cmdlet in the family that would not take a
//     code;
//   * the catalog lists fifteen bundles, and a live organisation can hold codes it has never
//     heard of (every App Testing SKU), which were therefore unremovable.
//
// Shares ResolveLicenseCodesToRemove with the user cmdlet deliberately: two copies of this
// rule would drift, and the drift is invisible until someone cannot remove a license.
public class RemovePmGroupLicenseResolutionTests
{
    private static List<WildcardPattern> Patterns(params string[] patterns) =>
        patterns.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

    // What a group holds, including a bundle the catalog knows and one it does not.
    private static readonly string[] GroupHolds = ["ATTUNU", "APPTESTDEVNU", "SOMENEWSKU"];

    [Fact]
    public void RawCode_Matches()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
            Patterns("ATTUNU"), GroupHolds);
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    [Fact]
    public void FriendlyName_Matches()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
            Patterns("Attended - Named User"), GroupHolds);
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    // The regression: an App Testing code the catalog did not list.
    [Fact]
    public void AppTestingCode_Matches()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
            Patterns("APPTESTDEVNU"), GroupHolds);
        Assert.Equal(new HashSet<string> { "APPTESTDEVNU" }, got);
    }

    // A code with no friendly name at all still matches by code, because the group holds it.
    [Fact]
    public void CodeUnknownToCatalog_Matches_ByCode()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
            Patterns("SOMENEWSKU"), GroupHolds);
        Assert.Equal(new HashSet<string> { "SOMENEWSKU" }, got);
    }

    [Fact]
    public void Star_StripsEverythingHeld()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
            Patterns("*"), GroupHolds);
        Assert.Equal(new HashSet<string>(GroupHolds), got);
    }

    // Not held: nothing to remove, and no complaint. Removal is idempotent by design.
    [Fact]
    public void NotHeld_ResolvesEmpty()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
            Patterns("CTZDEVNU"), GroupHolds);
        Assert.Empty(got);
    }

    [Fact]
    public void NullHoldings_ResolvesEmpty()
    {
        var got = RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(Patterns("ATTUNU"), null);
        Assert.NotNull(got);
        Assert.Empty(got);
    }
}
