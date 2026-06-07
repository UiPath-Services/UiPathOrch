using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit tests for the third wave of Compare-Orch* cmdlets (FolderUser, FolderMachine, User,
// ApiTrigger, EventTrigger): the new normalizers (role set, string set) and each cmdlet's
// comparable-property set. Mode dispatch is the shared FolderCompare / TenantCompare engine,
// validated live.

public class NormalizeRolesTests
{
    private static SimpleRole R(string name) => new() { Name = name };

    [Fact]
    public void NullOrEmpty_ReturnsNull()
    {
        Assert.Null(CompareFolderUserCmdlet.NormalizeRoles(null));
        Assert.Null(CompareFolderUserCmdlet.NormalizeRoles([]));
    }

    [Fact]
    public void IsOrderIndependent()
    {
        var a = CompareFolderUserCmdlet.NormalizeRoles([R("Automation User"), R("Folder Administrator")]);
        var b = CompareFolderUserCmdlet.NormalizeRoles([R("Folder Administrator"), R("Automation User")]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DistinguishesDifferentRoleSets()
    {
        var a = CompareFolderUserCmdlet.NormalizeRoles([R("Automation User")]);
        var b = CompareFolderUserCmdlet.NormalizeRoles([R("Automation User"), R("Folder Administrator")]);
        Assert.NotEqual(a, b);
    }
}

public class NormalizeStringSetTests
{
    [Fact]
    public void NullOrEmpty_ReturnsNull()
    {
        Assert.Null(CompareUserCmdlet.NormalizeStringSet(null));
        Assert.Null(CompareUserCmdlet.NormalizeStringSet([]));
    }

    [Fact]
    public void IsOrderIndependentAndDeduped()
    {
        var a = CompareUserCmdlet.NormalizeStringSet(["Administrator", "Robot", "Administrator"]);
        var b = CompareUserCmdlet.NormalizeStringSet(["Robot", "Administrator"]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DistinguishesDifferentSets()
    {
        var a = CompareUserCmdlet.NormalizeStringSet(["Administrator"]);
        var b = CompareUserCmdlet.NormalizeStringSet(["Administrator", "Robot"]);
        Assert.NotEqual(a, b);
    }
}

public class CompareFamily3MetadataTests
{
    [Fact]
    public void FolderUserValidPropertyNames()
    {
        Assert.Contains("Type", CompareFolderUserCmdlet.ValidPropertyNames);
        Assert.Contains("Roles", CompareFolderUserCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void FolderMachineValidPropertyNames()
    {
        foreach (var name in new[] { "Type", "Scope", "UnattendedSlots", "TargetFramework" })
            Assert.Contains(name, CompareFolderMachineCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void UserValidPropertyNames()
    {
        foreach (var name in new[] { "Type", "IsActive", "LicenseType", "RolesList", "EmailAddress" })
            Assert.Contains(name, CompareUserCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ApiTriggerValidPropertyNames()
    {
        foreach (var name in new[] { "Enabled", "Method", "CallingMode", "InputArguments" })
            Assert.Contains(name, CompareApiTriggerCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void EventTriggerValidPropertyNames()
    {
        foreach (var name in new[] { "Enabled", "ConnectorKey", "Operation", "ObjectName", "FilterExpression" })
            Assert.Contains(name, CompareEventTriggerCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ValidPropertyNames_AreCaseInsensitive()
    {
        Assert.Contains("roles", CompareFolderUserCmdlet.ValidPropertyNames);
        Assert.Contains("ROLESLIST", CompareUserCmdlet.ValidPropertyNames);
        Assert.Contains("connectorkey", CompareEventTriggerCmdlet.ValidPropertyNames);
    }
}
