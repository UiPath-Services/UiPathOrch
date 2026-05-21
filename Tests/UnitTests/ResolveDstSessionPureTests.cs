using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for ResolveDstSessionPure -- 3-tier session resolver extracted
// from CopyItem.cs's FindDstSession. Pins down the tier ordering and the
// known field-coercion asymmetry between tiers so a future change is
// visible (and we know which one to update).
public class ResolveDstSessionPureTests
{
    private static MachineSessionRuntime S(
        string? machine, string? host, string? user) =>
        new() { MachineName = machine, HostMachineName = host, ServiceUserName = user };

    [Fact]
    public void Tier1_FullTripleMatch_CaseInsensitive()
    {
        var dst = new[]
        {
            S("DOMAIN\\machine", "host1", "service1"),
            S("noise", "noise", "noise"),
        };
        var got = ResolveDstSessionPure(dst, "domain\\MACHINE", "HOST1", "Service1");
        Assert.NotNull(got);
        Assert.Equal("DOMAIN\\machine", got!.MachineName);
    }

    [Fact]
    public void Tier1_PrefersExactMatchOverFallback()
    {
        // Both a full triple match AND a 2-of-3 match exist; Tier 1 wins.
        var exact = S("m1", "h1", "u1");
        var partial = S("m1", "h1", null);
        var dst = new[] { partial, exact };
        var got = ResolveDstSessionPure(dst, "m1", "h1", "u1");
        Assert.Same(exact, got);
    }

    [Fact]
    public void Tier2_FallbackWhenSrcUserPresentButDstUserEmpty()
    {
        // Tier 1 needs ServiceUserName==srcServiceUserName ('u1');
        // dst has null/empty, so Tier 1 fails, Tier 2 catches.
        var dst = new[] { S("m1", "h1", null) };
        var got = ResolveDstSessionPure(dst, "m1", "h1", "u1");
        Assert.NotNull(got);
        Assert.Null(got!.ServiceUserName);
    }

    [Fact]
    public void Tier3_LoosestMatchWhenDstHasMismatchedUser()
    {
        // dst has a non-empty service user that doesn't match src; Tier 2
        // requires dst user to be EMPTY so it fails too. Tier 3 ignores
        // ServiceUserName entirely and matches.
        var dst = new[] { S("m1", "h1", "different-user") };
        var got = ResolveDstSessionPure(dst, "m1", "h1", "u1");
        Assert.NotNull(got);
        Assert.Equal("different-user", got!.ServiceUserName);
    }

    [Fact]
    public void NoMatchAtAnyTier()
    {
        var dst = new[] { S("other-machine", "h1", "u1") };
        var got = ResolveDstSessionPure(dst, "m1", "h1", "u1");
        Assert.Null(got);
    }

    [Fact]
    public void EmptyDstSessions()
    {
        var got = ResolveDstSessionPure(Array.Empty<MachineSessionRuntime>(), "m", "h", "u");
        Assert.Null(got);
    }

    [Fact]
    public void Tier1_CoercesNullDstFieldsToEmpty()
    {
        // src has empty ServiceUserName, dst has null ServiceUserName.
        // Tier 1 with null-coercion treats both as "" and matches.
        var dst = new[] { S("m1", "h1", null) };
        var got = ResolveDstSessionPure(dst, "m1", "h1", "");
        Assert.NotNull(got);
    }

    [Fact]
    public void AllTiersCoerceDstNullsToEmpty()
    {
        // All three tiers now null-coerce dst fields to "" before compare,
        // so a row with (null, null, null) fields is reachable from any
        // tier. The original implementation only coerced on Tier 1, which
        // made the looser tiers unreachable for null-field rows --
        // inconsistent and fixed.

        var dst = new[] { S(null, null, null) };
        var matchesTier1 = ResolveDstSessionPure(dst, "", "", "");
        Assert.NotNull(matchesTier1);
        Assert.Null(matchesTier1!.MachineName);

        // Tier 1 misses because src is non-empty, but Tier 3 still
        // doesn't match this row because MachineName is null vs "real-machine"
        // (real value mismatch is a legitimate miss).
        var noMatch = ResolveDstSessionPure(dst, "real-machine", "real-host", "");
        Assert.Null(noMatch);
    }
}
