using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// The 3.2 consolidation folded the shared Set-Orch{Credential,Secret}Asset flow into
// SetCredentialLikeAssetCmdletBase and isolated the per-type value logic behind hooks.
// Before the refactor this logic lived inline in each cmdlet's UpdateAssetInMemory /
// EndProcessing and had no unit coverage (only live Pester exercised it). These tests
// pin the production hooks directly — via thin testable subclasses — so the
// Credential/Secret divergence the refactor had to preserve stays correct. The single
// most important divergence: on an empty per-robot row Credential REMOVES the existing
// UserValue (AllowPerRobotRemoval == true) while Secret KEEPS it (false).
public class SetAssetHookTests
{
    // Expose the protected hooks on the real cmdlets. Constructing a cmdlet without a
    // runspace is fine: the value hooks never touch SessionState, the API, or pipeline I/O.
    private sealed class CredHooks : SetCredentialAssetCmdlet
    {
        public string Kind => ValueType;
        public bool Removal => AllowPerRobotRemoval;
        public bool HasCreate(SetCredentialAssetCommandParameter p) => HasCreateValue(p);
        public void InitNew(Asset a) => InitializeNewAsset(a);
        public bool Global(Asset a, SetCredentialAssetCommandParameter p) => ApplyGlobalValue(a, p);
        public bool PerRobotEmpty(SetCredentialAssetCommandParameter p) => IsPerRobotValueEmpty(p);
        public bool PerRobot(AssetUserValue uv, SetCredentialAssetCommandParameter p) => ApplyPerRobotValue(uv, p);
        public void Normalize(Asset a) => NormalizeBeforeFlush(a);
    }

    private sealed class SecretHooks : SetSecretAssetCmdlet
    {
        public string Kind => ValueType;
        public bool Removal => AllowPerRobotRemoval;
        public bool HasCreate(SetSecretAssetCommandParameter p) => HasCreateValue(p);
        public void InitNew(Asset a) => InitializeNewAsset(a);
        public bool Global(Asset a, SetSecretAssetCommandParameter p) => ApplyGlobalValue(a, p);
        public bool PerRobotEmpty(SetSecretAssetCommandParameter p) => IsPerRobotValueEmpty(p);
        public bool PerRobot(AssetUserValue uv, SetSecretAssetCommandParameter p) => ApplyPerRobotValue(uv, p);
        public void Normalize(Asset a) => NormalizeBeforeFlush(a);
    }

    // ---------------- Credential ----------------

    [Fact]
    public void Cred_ValueType_And_RemovalPolicy()
    {
        var c = new CredHooks();
        Assert.Equal("Credential", c.Kind);
        Assert.True(c.Removal); // Credential clears the per-robot entry on an empty row
    }

    [Theory]
    [InlineData(null, null, false)]   // nothing to set -> do not create
    [InlineData("", "", false)]       // both blank -> do not create
    [InlineData("pw", null, true)]    // password present
    [InlineData(null, "ext", true)]   // external name present
    public void Cred_HasCreateValue(string? password, string? externalName, bool expected)
    {
        var c = new CredHooks();
        var p = new SetCredentialAssetCommandParameter { CredentialPassword = password, ExternalName = externalName };
        Assert.Equal(expected, c.HasCreate(p));
    }

    [Fact]
    public void Cred_InitializeNewAsset_SeedsEmptyUsername()
    {
        var a = new Asset();
        new CredHooks().InitNew(a);
        Assert.Equal("", a.CredentialUsername);
    }

    [Fact]
    public void Cred_Global_ExternalName_ClearsUserPassAndSetsDefault()
    {
        var a = new Asset { CredentialUsername = "u", CredentialPassword = "pw" };
        var p = new SetCredentialAssetCommandParameter { ExternalName = "vault-key" };

        Assert.True(new CredHooks().Global(a, p));
        Assert.Equal("vault-key", a.ExternalName);
        Assert.Null(a.CredentialUsername);
        Assert.Null(a.CredentialPassword);
        Assert.True(a.HasDefaultValue);
    }

    [Fact]
    public void Cred_Global_UsernameAndPassword_AreSet()
    {
        var a = new Asset { CredentialUsername = "old" };
        var p = new SetCredentialAssetCommandParameter { CredentialUsername = "new", CredentialPassword = "pw" };

        Assert.True(new CredHooks().Global(a, p));
        Assert.Equal("new", a.CredentialUsername);
        Assert.Equal("pw", a.CredentialPassword);
        Assert.True(a.HasDefaultValue);
    }

    [Fact]
    public void Cred_Global_EmptyPassword_IsTreatedAsNotSpecified()
    {
        // "" means "not specified" (e.g. CSV export masks the password). No change, not dirty.
        var a = new Asset { CredentialUsername = "u", HasDefaultValue = false };
        var p = new SetCredentialAssetCommandParameter { CredentialPassword = "" };

        Assert.False(new CredHooks().Global(a, p));
        Assert.Null(a.CredentialPassword);
        Assert.False(a.HasDefaultValue);
    }

    [Theory]
    [InlineData(null, "", null, true)]   // no username, password blanked -> empty row
    [InlineData("", null, "", true)]     // no username, external blanked -> empty row
    [InlineData("u", null, null, false)] // username present -> not empty
    [InlineData(null, "pw", null, false)]// password present (not "") -> not empty
    public void Cred_IsPerRobotValueEmpty(string? user, string? pass, string? ext, bool expected)
    {
        var c = new CredHooks();
        var p = new SetCredentialAssetCommandParameter { CredentialUsername = user, CredentialPassword = pass, ExternalName = ext };
        Assert.Equal(expected, c.PerRobotEmpty(p));
    }

    [Fact]
    public void Cred_PerRobot_ExternalName_ClearsUserAndPass()
    {
        var uv = new AssetUserValue { CredentialUsername = "u", CredentialPassword = "pw" };
        var p = new SetCredentialAssetCommandParameter { ExternalName = "vault-key" };

        Assert.True(new CredHooks().PerRobot(uv, p));
        Assert.Equal("vault-key", uv.ExternalName);
        Assert.Null(uv.CredentialUsername);
        Assert.Null(uv.CredentialPassword);
    }

    [Fact]
    public void Cred_PerRobot_UsernameAndPassword_AreSet()
    {
        var uv = new AssetUserValue { CredentialUsername = "old" };
        var p = new SetCredentialAssetCommandParameter { CredentialUsername = "new", CredentialPassword = "pw" };

        Assert.True(new CredHooks().PerRobot(uv, p));
        Assert.Equal("new", uv.CredentialUsername);
        Assert.Equal("pw", uv.CredentialPassword);
    }

    [Fact]
    public void Cred_Normalize_EmptyUsernameToNull_AndBlankPasswordDropsStoreLink()
    {
        var a = new Asset { CredentialUsername = "", CredentialPassword = null, CredentialStoreId = 5 };
        new CredHooks().Normalize(a);
        Assert.Null(a.CredentialUsername);
        Assert.Null(a.CredentialStoreId); // blank password -> store link dropped
    }

    [Fact]
    public void Cred_Normalize_KeepsStoreLinkWhenPasswordPresent()
    {
        var a = new Asset { CredentialUsername = "u", CredentialPassword = "pw", CredentialStoreId = 5 };
        new CredHooks().Normalize(a);
        Assert.Equal("u", a.CredentialUsername);
        Assert.Equal(5, a.CredentialStoreId);
    }

    // ---------------- Secret ----------------

    [Fact]
    public void Secret_ValueType_And_RemovalPolicy()
    {
        var s = new SecretHooks();
        Assert.Equal("Secret", s.Kind);
        Assert.False(s.Removal); // Secret leaves the per-robot entry in place on an empty row
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", "", false)]
    [InlineData("sv", null, true)]
    [InlineData(null, "ext", true)]
    public void Secret_HasCreateValue(string? secretValue, string? externalName, bool expected)
    {
        var s = new SecretHooks();
        var p = new SetSecretAssetCommandParameter { SecretValue = secretValue, ExternalName = externalName };
        Assert.Equal(expected, s.HasCreate(p));
    }

    [Fact]
    public void Secret_InitializeNewAsset_IsNoOp()
    {
        var a = new Asset();
        new SecretHooks().InitNew(a);
        Assert.Null(a.CredentialUsername); // unlike Credential, no empty-username seeding
    }

    [Fact]
    public void Secret_Global_ExternalName_ClearsValueAndSetsDefault()
    {
        var a = new Asset { SecretValue = "old" };
        var p = new SetSecretAssetCommandParameter { ExternalName = "vault-key" };

        Assert.True(new SecretHooks().Global(a, p));
        Assert.Equal("vault-key", a.ExternalName);
        Assert.Null(a.SecretValue);
        Assert.True(a.HasDefaultValue);
    }

    [Fact]
    public void Secret_Global_SecretValue_IsSet()
    {
        var a = new Asset();
        var p = new SetSecretAssetCommandParameter { SecretValue = "s3cr3t" };

        Assert.True(new SecretHooks().Global(a, p));
        Assert.Equal("s3cr3t", a.SecretValue);
        Assert.True(a.HasDefaultValue);
    }

    [Fact]
    public void Secret_Global_Nothing_IsNotDirty()
    {
        var a = new Asset { HasDefaultValue = false };
        var p = new SetSecretAssetCommandParameter();

        Assert.False(new SecretHooks().Global(a, p));
        Assert.Null(a.SecretValue);
        Assert.False(a.HasDefaultValue);
    }

    [Theory]
    [InlineData(null, null, true)]   // both empty -> empty row
    [InlineData("", "", true)]
    [InlineData("sv", null, false)]  // secret present
    [InlineData(null, "ext", false)] // external present
    public void Secret_IsPerRobotValueEmpty(string? secretValue, string? ext, bool expected)
    {
        var s = new SecretHooks();
        var p = new SetSecretAssetCommandParameter { SecretValue = secretValue, ExternalName = ext };
        Assert.Equal(expected, s.PerRobotEmpty(p));
    }

    [Fact]
    public void Secret_PerRobot_ExternalName_ClearsValue()
    {
        var uv = new AssetUserValue { SecretValue = "old" };
        var p = new SetSecretAssetCommandParameter { ExternalName = "vault-key" };

        Assert.True(new SecretHooks().PerRobot(uv, p));
        Assert.Equal("vault-key", uv.ExternalName);
        Assert.Null(uv.SecretValue);
    }

    [Fact]
    public void Secret_PerRobot_SecretValue_IsSet()
    {
        var uv = new AssetUserValue();
        var p = new SetSecretAssetCommandParameter { SecretValue = "s3cr3t" };

        Assert.True(new SecretHooks().PerRobot(uv, p));
        Assert.Equal("s3cr3t", uv.SecretValue);
    }

    [Fact]
    public void Secret_Normalize_PreservesStoreLink()
    {
        // Secret deliberately does NOT drop the store link: the API masks the value, so on
        // update we must keep the CredentialStoreId copied from the existing asset.
        var a = new Asset { SecretValue = null, CredentialStoreId = 5 };
        new SecretHooks().Normalize(a);
        Assert.Equal(5, a.CredentialStoreId);
    }
}
