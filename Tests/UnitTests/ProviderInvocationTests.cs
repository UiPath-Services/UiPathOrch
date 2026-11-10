using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// ProviderInvocation reads PowerShell internals (CmdletProvider.Context and its MyInvocation) by
// reflection to tell one Copy-Item's paths from the next command. If a PowerShell version moves
// them, Copy-Item quietly falls back to treating an array -Path as one copy per element; this
// test makes that visible on every OS CI runs on.
public class ProviderInvocationTests
{
    [Fact]
    public void The_PowerShell_internals_it_reads_are_present()
        => Assert.True(ProviderInvocation.CanReadInvocation);

    [Fact]
    public void State_is_shared_per_token_and_first_only_once()
    {
        var token = new object();
        var a = ProviderInvocation.StateFor<Holder>(token, out bool first1);
        var b = ProviderInvocation.StateFor<Holder>(token, out bool first2);
        Assert.True(first1);
        Assert.False(first2);
        Assert.Same(a, b);
    }

    [Fact]
    public void A_different_token_gets_its_own_state()
    {
        var a = ProviderInvocation.StateFor<Holder>(new object(), out _);
        var b = ProviderInvocation.StateFor<Holder>(new object(), out bool first);
        Assert.True(first);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void No_token_is_always_a_first_path()
    {
        ProviderInvocation.StateFor<Holder>(null, out bool first1);
        ProviderInvocation.StateFor<Holder>(null, out bool first2);
        Assert.True(first1);
        Assert.True(first2);
    }

    private sealed class Holder { }
}
