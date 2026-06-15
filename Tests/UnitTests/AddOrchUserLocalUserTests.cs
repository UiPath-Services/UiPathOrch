using System.Linq;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

public class AddOrchUserLocalUserTests
{
    [Fact]
    public void UnspecifiedTypeMeansAllSupportedTypes()
    {
        var types = AddUserCmdlet.ResolveSpecifiedTypes(null);

        Assert.Equal(new[] { 0, 1, 3, 4 }, types.OrderBy(t => t).ToArray());
    }

    [Fact]
    public void LocalPmUserMapsToDirectoryUserPayloadIdentity()
    {
        var localUser = new DirectoryUser()
        {
            objectType = "DirectoryUser",
            source = "local",
            identifier = "cf3ccc24-2cd5-4009-aad9-8008157f5151",
            name = "auiongpin",
            displayName = "Pin Auiong"
        };

        var directoryObject = AddUserCmdlet.CreateLocalUserDirectoryObject(localUser);

        Assert.NotNull(directoryObject);
        Assert.Equal(0, directoryObject!.type);
        Assert.Equal("local", directoryObject.source);
        Assert.Equal("autogen", directoryObject.domain);
        Assert.Equal("cf3ccc24-2cd5-4009-aad9-8008157f5151", directoryObject.identifier);
        Assert.Equal("auiongpin", directoryObject.identityName);
        Assert.Equal("Pin Auiong", directoryObject.displayName);
    }

    [Fact]
    public void NonLocalPmUserDoesNotBypassDirectoryService()
    {
        var directoryUser = new DirectoryUser()
        {
            objectType = "DirectoryUser",
            source = "directory",
            identifier = "b8f96a7b-c15d-4612-85bb-603392b32a33",
            name = "auiongpin"
        };

        Assert.Null(AddUserCmdlet.CreateLocalUserDirectoryObject(directoryUser));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("autogen", true)]
    [InlineData("AUTOGEN", true)]
    [InlineData("frc", false)]
    public void OnlyAutogenDomainUsesLocalUserResolution(string? domain, bool expected)
    {
        Assert.Equal(expected, AddUserCmdlet.IsLocalUserDomain(domain));
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
