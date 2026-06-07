using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit tests for the parts of the Compare-OrchProcess / Compare-OrchQueue / Compare-OrchRole
// cmdlets that carry real logic: the shared tag normalizer, the role permission-matrix
// normalizer, and each cmdlet's comparable-property set. The mode dispatch and "<=" / "=>"
// enumeration are the shared FolderCompare / EntityComparison engine (covered elsewhere) and
// were validated live against a tenant.

public class NormalizeTagsTests
{
    [Fact]
    public void NullOrEmpty_ReturnsNull()
    {
        Assert.Null(EntityComparison.NormalizeTags(null));
        Assert.Null(EntityComparison.NormalizeTags([]));
    }

    [Fact]
    public void IsOrderIndependent()
    {
        Tag[] a = [new() { Name = "env", Value = "prod" }, new() { Name = "team", Value = "fin" }];
        Tag[] b = [new() { Name = "team", Value = "fin" }, new() { Name = "env", Value = "prod" }];
        Assert.Equal(EntityComparison.NormalizeTags(a), EntityComparison.NormalizeTags(b));
    }

    [Fact]
    public void DistinguishesDifferentValues()
    {
        Tag[] a = [new() { Name = "env", Value = "prod" }];
        Tag[] b = [new() { Name = "env", Value = "dev" }];
        Assert.NotEqual(EntityComparison.NormalizeTags(a), EntityComparison.NormalizeTags(b));
    }
}

public class NormalizePermissionsTests
{
    private static Permission Granted(string scope, string name) => new() { Scope = scope, Name = name, IsGranted = true };
    private static Permission Denied(string scope, string name) => new() { Scope = scope, Name = name, IsGranted = false };

    [Fact]
    public void NullOrEmpty_ReturnsNull()
    {
        Assert.Null(CompareRoleCmdlet.NormalizePermissions(null));
        Assert.Null(CompareRoleCmdlet.NormalizePermissions([]));
    }

    [Fact]
    public void IgnoresNonGrantedPermissions()
    {
        var onlyGranted = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Assets.View")]);
        var withDenied = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Assets.View"), Denied("Global", "Assets.Edit")]);
        Assert.Equal(onlyGranted, withDenied);
    }

    [Fact]
    public void IsOrderIndependent()
    {
        var a = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Assets.View"), Granted("Global", "Queues.View")]);
        var b = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Queues.View"), Granted("Global", "Assets.View")]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GrantingAPermissionChangesTheNormalizedForm()
    {
        var before = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Assets.View")]);
        var after = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Assets.View"), Granted("Global", "Assets.Edit")]);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void ScopeIsPartOfTheIdentity()
    {
        var global = CompareRoleCmdlet.NormalizePermissions([Granted("Global", "Assets.View")]);
        var folder = CompareRoleCmdlet.NormalizePermissions([Granted("Folder", "Assets.View")]);
        Assert.NotEqual(global, folder);
    }
}

public class CompareFamilyMetadataTests
{
    [Fact]
    public void ProcessValidPropertyNames_IncludeKeyReleaseFields()
    {
        foreach (var name in new[] { "ProcessKey", "ProcessVersion", "EnvironmentName", "InputArguments", "Tags" })
            Assert.Contains(name, CompareProcessCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void QueueValidPropertyNames_IncludeSchemaAndRetention()
    {
        foreach (var name in new[] { "SpecificDataJsonSchema", "Encrypted", "MaxNumberOfRetries", "RetentionAction", "Tags" })
            Assert.Contains(name, CompareQueueCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void RoleValidPropertyNames_IncludePermissionsAndShape()
    {
        foreach (var name in new[] { "Permissions", "DisplayName", "Type", "Groups" })
            Assert.Contains(name, CompareRoleCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ValidPropertyNames_AreCaseInsensitive()
    {
        Assert.Contains("processkey", CompareProcessCmdlet.ValidPropertyNames);
        Assert.Contains("ENCRYPTED", CompareQueueCmdlet.ValidPropertyNames);
        Assert.Contains("permissions", CompareRoleCmdlet.ValidPropertyNames);
    }
}
