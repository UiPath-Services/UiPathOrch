using System.Collections;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using Xunit;

namespace UnitTests;

public class ResolveDepthTests
{
    [Fact]
    public void NullDictionary_ReturnsZero()
    {
        Assert.Equal(0u, OrchArgumentCompleter.ResolveDepth(null));
    }

    [Fact]
    public void EmptyDictionary_ReturnsZero()
    {
        Assert.Equal(0u, OrchArgumentCompleter.ResolveDepth(new Hashtable()));
    }

    [Fact]
    public void DepthAbsent_ReturnsZero()
    {
        var dict = new Hashtable { ["Recurse"] = new SwitchParameter(true) };
        Assert.Equal(0u, OrchArgumentCompleter.ResolveDepth(dict));
    }

    [Fact]
    public void DepthAsUint_Roundtrips()
    {
        var dict = new Hashtable { ["Depth"] = 5u };
        Assert.Equal(5u, OrchArgumentCompleter.ResolveDepth(dict));
    }

    [Fact]
    public void DepthAsInt_Coerced()
    {
        var dict = new Hashtable { ["Depth"] = 7 };
        Assert.Equal(7u, OrchArgumentCompleter.ResolveDepth(dict));
    }

    [Fact]
    public void DepthAsString_Parsed()
    {
        var dict = new Hashtable { ["Depth"] = "12" };
        Assert.Equal(12u, OrchArgumentCompleter.ResolveDepth(dict));
    }

    [Fact]
    public void DepthAsUnparseableString_ReturnsZero()
    {
        var dict = new Hashtable { ["Depth"] = "abc" };
        Assert.Equal(0u, OrchArgumentCompleter.ResolveDepth(dict));
    }

    [Fact]
    public void DepthAsNegativeInt_ReturnsZero()
    {
        // uint can't be negative; helper should not throw/wrap
        var dict = new Hashtable { ["Depth"] = -3 };
        Assert.Equal(0u, OrchArgumentCompleter.ResolveDepth(dict));
    }

    [Fact]
    public void DepthAsNullValue_ReturnsZero()
    {
        var dict = new Hashtable { ["Depth"] = null };
        Assert.Equal(0u, OrchArgumentCompleter.ResolveDepth(dict));
    }
}

public class ResolveSwitchParameterTests
{
    [Fact]
    public void NullDictionary_ReturnsFalse()
    {
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(null, "Recurse"));
    }

    [Fact]
    public void EmptyDictionary_ReturnsFalse()
    {
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(new Hashtable(), "Recurse"));
    }

    [Fact]
    public void ParameterAbsent_ReturnsFalse()
    {
        var dict = new Hashtable { ["Path"] = "x" };
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void SwitchParameterPresent_ReturnsTrue()
    {
        var dict = new Hashtable { ["Recurse"] = new SwitchParameter(true) };
        Assert.True(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void SwitchParameterNotPresent_ReturnsFalse()
    {
        var dict = new Hashtable { ["Recurse"] = new SwitchParameter(false) };
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void BoolTrue_ReturnsTrue()
    {
        var dict = new Hashtable { ["Recurse"] = true };
        Assert.True(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void BoolFalse_ReturnsFalse()
    {
        var dict = new Hashtable { ["Recurse"] = false };
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void NullValue_ReturnsFalse()
    {
        var dict = new Hashtable { ["Recurse"] = null };
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void StringTrue_ReturnsTrue()
    {
        // PowerShell sometimes hands us strings during completion when binding hasn't fully resolved
        var dict = new Hashtable { ["Recurse"] = "true" };
        Assert.True(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void StringFalse_ReturnsFalse()
    {
        var dict = new Hashtable { ["Recurse"] = "false" };
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }

    [Fact]
    public void NonsenseString_ReturnsFalse()
    {
        var dict = new Hashtable { ["Recurse"] = "garbage" };
        Assert.False(OrchArgumentCompleter.ResolveSwitchParameter(dict, "Recurse"));
    }
}
