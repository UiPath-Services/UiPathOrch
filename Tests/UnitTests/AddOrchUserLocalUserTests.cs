using System.Linq;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

public class AddOrchUserLocalUserTests
{
    [Fact]
    public void UnspecifiedTypeDoesNotImplicitlySearchAllSupportedTypes()
    {
        var types = AddUserCmdlet.ResolveSpecifiedTypes(null);

        Assert.Empty(types);
    }

    [Fact]
    public void TypeWildcardResolvesSupportedTypes()
    {
        var types = AddUserCmdlet.ResolveSpecifiedTypes(["DirectoryUser", "DirectoryExternal*"]);

        Assert.Equal(new[] { 0, 4 }, types.OrderBy(t => t).ToArray());
    }

    [Theory]
    [InlineData("DirectoryUser", "User")]
    [InlineData("DirectoryGroup", "Group")]
    [InlineData("DirectoryExternalApplication", "Application")]
    [InlineData("DirectoryRobot", "DirectoryRobot")]
    public void ConvertsDirectoryTypesToPlatformManagementKinds(string type, string expectedKind)
    {
        Assert.Equal(expectedKind, AddUserCmdlet.ConvertToKind(type));
    }

    [Fact]
    public void BulkResolvedLocalPmUserMapsToDirectoryUserPayloadIdentity()
    {
        var localUser = new DirectoryUser()
        {
            objectType = "DirectoryUser",
            source = "local",
            identifier = "cf3ccc24-2cd5-4009-aad9-8008157f5151",
            name = "auiongpin",
            displayName = "Pin Auiong"
        };

        var directoryObject = AddUserCmdlet.CreateDirectoryObject(localUser, 0);

        Assert.NotNull(directoryObject);
        Assert.Equal(0, directoryObject!.type);
        Assert.Equal("local", directoryObject.source);
        Assert.Null(directoryObject.domain);
        Assert.Equal("cf3ccc24-2cd5-4009-aad9-8008157f5151", directoryObject.identifier);
        Assert.Equal("auiongpin", directoryObject.identityName);
        Assert.Equal("Pin Auiong", directoryObject.displayName);
    }

    [Fact]
    public void BulkResolvedDirectoryPmUserMapsWithoutSourceGate()
    {
        var directoryUser = new DirectoryUser()
        {
            objectType = "DirectoryUser",
            source = "directory",
            identifier = "b8f96a7b-c15d-4612-85bb-603392b32a33",
            name = "auiongpin"
        };

        var directoryObject = AddUserCmdlet.CreateDirectoryObject(directoryUser, 0);

        Assert.NotNull(directoryObject);
        Assert.Equal("directory", directoryObject!.source);
        Assert.Equal("b8f96a7b-c15d-4612-85bb-603392b32a33", directoryObject.identifier);
        Assert.Equal("auiongpin", directoryObject.identityName);
    }

    [Theory]
    [InlineData(null, null, "autogen")]
    [InlineData(null, "frc", "frc")]
    [InlineData("root", "frc", "root")]
    [InlineData("autogen", "frc", "autogen")]
    public void PostingDomainPrefersExplicitThenResolvedThenAutogen(string? explicitDomain, string? resolvedDomain, string expected)
    {
        Assert.Equal(expected, AddUserCmdlet.ResolvePostingDomain(explicitDomain, resolvedDomain));
    }
}
