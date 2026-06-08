using System.Management.Automation;
using UiPath.PowerShell.Positional;
using Xunit;

namespace UnitTests;

// Tests for DictionaryItemsExtensions.ResolveKeyOrThrow -- the user-input key
// resolver that replaced the bare `Items[key]` indexer in Get-OrchUserSession
// (-State / -Type / -OrderBy). The indexer threw the opaque KeyNotFoundException
// "The given key 'X' was not present in the dictionary." (reproduced live);
// ResolveKeyOrThrow instead throws a PSArgumentException that names the parameter
// and lists the valid values. The API-response mapping sites were converted to
// GetValueOrDefault(code, code) instead (pass through unknown codes) -- that is
// plain BCL behavior and is not retested here.
public class DictionaryItemsResolveTests
{
    private static readonly Dictionary<string, int> IntDict = new()
    {
        { "Available", 0 }, { "Busy", 1 }, { "Disconnected", 2 }
    };

    private static readonly Dictionary<string, string> StrDict = new()
    {
        { "User", "Robot/User/UserName" }, { "Hostname", "HostMachineName" }
    };

    [Fact]
    public void ReturnsValue_ForKnownKey_IntDict()
        => Assert.Equal(1, IntDict.ResolveKeyOrThrow("Busy", "State"));

    [Fact]
    public void ReturnsValue_ForKnownKey_StringDict()
        => Assert.Equal("HostMachineName", StrDict.ResolveKeyOrThrow("Hostname", "OrderBy"));

    [Fact]
    public void Throws_PSArgumentException_ForUnknownKey()
    {
        var ex = Assert.Throws<PSArgumentException>(
            () => IntDict.ResolveKeyOrThrow("Nope", "State"));

        Assert.Contains("Nope", ex.Message);          // the offending value
        Assert.Contains("State", ex.Message);         // the parameter name
        Assert.Contains("Available", ex.Message);     // valid values are listed...
        Assert.Contains("Busy", ex.Message);
        Assert.Contains("Disconnected", ex.Message);
        Assert.Equal("State", ex.ParamName);
    }

    [Fact]
    public void Throws_NotKeyNotFound_ForUnknownKey()
    {
        // The whole point: callers no longer surface the opaque BCL exception.
        Assert.IsNotType<KeyNotFoundException>(
            Record.Exception(() => StrDict.ResolveKeyOrThrow("Bogus", "OrderBy")));
    }

    [Fact]
    public void Throws_ForNullKey_WithoutNullReference()
    {
        var ex = Assert.Throws<PSArgumentException>(
            () => StrDict.ResolveKeyOrThrow(null, "OrderBy"));
        Assert.Contains("OrderBy", ex.Message);
    }

    [Fact]
    public void IntegratesWithRealLookupDictionaries()
    {
        Assert.Equal("Robot/User/UserName",
            UserSessionOrderableItems.Items.ResolveKeyOrThrow("User", "OrderBy"));
        Assert.Equal(2,
            UserSessionStateItems.Items.ResolveKeyOrThrow("Disconnected", "State"));
        Assert.Throws<PSArgumentException>(
            () => UserSessionOrderableItems.Items.ResolveKeyOrThrow("Bogus", "OrderBy"));
    }
}
