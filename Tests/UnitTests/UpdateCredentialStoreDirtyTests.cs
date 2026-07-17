using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;
using Inputs = UiPath.PowerShell.Commands.UpdateCredentialStoreCmdlet.CredentialStoreUpdateInputs;

namespace UnitTests;

// Per-field change-detection for Update-OrchCredentialStore (ComputeCredentialStoreUpdate).
// NewName / HostName are diffed in both directions. AdditionalConfiguration is a write-only secret
// (the GET masks it), so only "a supplied value writes / an empty string leaves it" applies — and
// an empty string must NEVER write a blank secret.
public class UpdateCredentialStoreDirtyTests
{
    private static CredentialStore Store() => new() { Id = 5, Name = "store", HostName = "host.example" };

    private static (bool dirty, CredentialStore payload) Run(Inputs input)
    {
        // The cmdlet nulls AdditionalConfiguration on both the source and the copy before diffing
        // (the server returns it masked), so mirror that here.
        var source = Store();
        source.AdditionalConfiguration = null;
        var payload = Store();
        payload.AdditionalConfiguration = null;
        payload.Id = source.Id;

        bool dirty = UpdateCredentialStoreCmdlet.ComputeCredentialStoreUpdate(payload, source, input);
        return (dirty, payload);
    }

    [Fact] public void NothingSpecified_IsNoOp() => Assert.False(Run(new()).dirty);

    [Fact] public void NewName_Same_IsNoOp() => Assert.False(Run(new() { NewName = "store" }).dirty);
    [Fact] public void NewName_Different_Writes() => Assert.True(Run(new() { NewName = "renamed" }).dirty);

    [Fact] public void HostName_Same_IsNoOp() => Assert.False(Run(new() { HostName = "host.example" }).dirty);
    [Fact] public void HostName_Different_Writes() => Assert.True(Run(new() { HostName = "other.example" }).dirty);

    [Fact]
    public void AdditionalConfiguration_NonEmpty_Writes()
    {
        var r = Run(new() { AdditionalConfiguration = "{\"apiKey\":\"secret\"}" });
        Assert.True(r.dirty);
        Assert.Equal("{\"apiKey\":\"secret\"}", r.payload.AdditionalConfiguration);
    }

    [Fact]
    public void AdditionalConfiguration_EmptyString_IsNoOp_NeverWritesBlank()
    {
        var r = Run(new() { AdditionalConfiguration = "" });
        Assert.False(r.dirty);
        Assert.Null(r.payload.AdditionalConfiguration); // stays null, not overwritten with ""
    }
}
