using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins AddPmGroupLicenseCmdlet.SuggestableLicenseCodes — the set behind the
// -License completer. Candidates are the union of each matched group's available
// bundles (the available-bundles API returns held bundles too); excluded are
// only the codes EVERY group already holds (intersection of held sets). So a
// single group gives "available minus held", and across several groups a license
// stays suggested while at least one group is missing it. Verified live on Orch1.
public class SuggestableLicenseCodesTests
{
    private static (IEnumerable<string?>, IEnumerable<string?>?) G(string[] available, string?[]? held)
        => (available, held);

    // --- single group: behaves like "available minus held" ---

    [Fact]
    public void SingleGroup_SubtractsHeldFromAvailable()
    {
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", "RPADEVPRONU", "RPADEVNU", "CTZDEVNU" }, new[] { "ATTUNU", "RPADEVPRONU" }),
        });
        Assert.Equal(new HashSet<string> { "RPADEVNU", "CTZDEVNU" }, got);
    }

    [Fact]
    public void SingleGroup_NothingHeld_ReturnsAllAvailable()
    {
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", "CTZDEVNU" }, null),
        });
        Assert.Equal(new HashSet<string> { "ATTUNU", "CTZDEVNU" }, got);
    }

    [Fact]
    public void SingleGroup_AllHeld_ReturnsEmpty()
    {
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", "CTZDEVNU" }, new[] { "ATTUNU", "CTZDEVNU" }),
        });
        Assert.Empty(got);
    }

    // --- multiple groups: exclude ONLY commonly-held; union of available ---

    [Fact]
    public void MultiGroup_ExcludesOnlyCommonlyHeld()
    {
        // Both hold ATTUNU (common) → excluded. CTZDEVNU held by group1 only →
        // still suggested (group2 is missing it). RPADEVNU held by neither.
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", "CTZDEVNU", "RPADEVNU" }, new[] { "ATTUNU", "CTZDEVNU" }),
            G(new[] { "ATTUNU", "CTZDEVNU", "RPADEVNU" }, new[] { "ATTUNU" }),
        });
        Assert.Equal(new HashSet<string> { "CTZDEVNU", "RPADEVNU" }, got);
    }

    [Fact]
    public void MultiGroup_UnionsAvailableAcrossGroups()
    {
        // group1 offers ATTUNU; group2 offers CTZDEVNU. Neither commonly held.
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU" }, null),
            G(new[] { "CTZDEVNU" }, null),
        });
        Assert.Equal(new HashSet<string> { "ATTUNU", "CTZDEVNU" }, got);
    }

    [Fact]
    public void MultiGroup_NoCommonHeld_NothingExcluded()
    {
        // group1 holds ATTUNU, group2 holds CTZDEVNU — nothing held by BOTH, so
        // the whole available union is suggested. (This is the live Orch1 state:
        // ほえほえグループ holds RPADEVPRONU, Automation Express holds AKIT,CTZDEVNU —
        // no overlap — so completion offers the full available set.)
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", "CTZDEVNU" }, new[] { "ATTUNU" }),
            G(new[] { "ATTUNU", "CTZDEVNU" }, new[] { "CTZDEVNU" }),
        });
        Assert.Equal(new HashSet<string> { "ATTUNU", "CTZDEVNU" }, got);
    }

    [Fact]
    public void MultiGroup_AllCommonlyHeld_ReturnsEmpty()
    {
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU" }, new[] { "ATTUNU" }),
            G(new[] { "ATTUNU" }, new[] { "ATTUNU" }),
        });
        Assert.Empty(got);
    }

    [Fact]
    public void CaseInsensitive_OnHeldCodes()
    {
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", "CTZDEVNU" }, new[] { "attunu" }),
        });
        Assert.Equal(new HashSet<string> { "CTZDEVNU" }, got);
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(
            System.Array.Empty<(IEnumerable<string?>, IEnumerable<string?>?)>()));
    }

    [Fact]
    public void NullsInInput_AreIgnored()
    {
        var got = AddPmGroupLicenseCmdlet.SuggestableLicenseCodes(new[]
        {
            G(new[] { "ATTUNU", null!, "CTZDEVNU" }, new[] { (string?)null }),
        });
        Assert.Equal(new HashSet<string> { "ATTUNU", "CTZDEVNU" }, got);
    }
}
