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
    public void BugDiscovery_Tier2DoesNotCoerceDstNullsLikeTier1Does()
    {
        // Tier 1 path: src ServiceUserName="u1" vs dst.ServiceUserName=null
        // -> "u1" != "" -> Tier 1 fails -> Tier 2 takes over.
        // Tier 2 checks IsNullOrEmpty(dst.ServiceUserName) which IS true,
        // so this row matches.
        //
        // What about Tier 2's MachineName check though? Tier 2 uses
        // string.Equals(s.MachineName, srcMachineName, ...) WITHOUT
        // null-coercion. If dst.MachineName is null and src is "" they
        // would NOT match (null != "" in string.Equals). But with the
        // null-coercion-only-on-Tier1 asymmetry, a dst session with
        // (null, null, null) fields would:
        //   - Match Tier 1 with src ("", "", "")
        //   - NOT match Tier 2 because string.Equals(null, "", ...) = false
        //   - NOT match Tier 3 same reason
        // So the asymmetry is "Tier 1 is more forgiving with nulls than
        // its fallbacks". Pinned down here; verify with a stricter or
        // looser policy is intentional.

        var dst = new[] { S(null, null, null) };
        var matchesTier1 = ResolveDstSessionPure(dst, "", "", "");
        Assert.NotNull(matchesTier1);
        // The matched entry is the null-null-null row, exactly because
        // Tier 1 coerced its nulls.
        Assert.Null(matchesTier1!.MachineName);

        // If Tier 1 doesn't match (src non-empty), Tiers 2/3 won't catch
        // the null-field row either because they use uncoerced Equals.
        var noMatch = ResolveDstSessionPure(dst, "real-machine", "real-host", "");
        Assert.Null(noMatch);
    }
}
