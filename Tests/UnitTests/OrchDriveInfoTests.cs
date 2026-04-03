using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

public class OrchDriveInfoTests
{
    [Theory]
    [InlineData("Shared", "Shared")]
    [InlineData("Shared/Sub/Deep", "Shared")]
    [InlineData("", "")]
    public void GetTopParentPath_ExtractsFirstSegment(string orchPath, string expected)
    {
        Assert.Equal(expected, OrchDriveInfo.GetTopParentPath(orchPath));
    }

    [Theory]
    [InlineData("Orch1:\\Shared", "Orch1")]
    [InlineData("MyDrive:", "MyDrive")]
    [InlineData("NoDrive", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ExtractDriveName_ParsesCorrectly(string? path, string expected)
    {
        Assert.Equal(expected, OrchDriveInfo.ExtractDriveName(path!));
    }

    [Theory]
    [InlineData("Orch1:\\Shared\\Sub", "Shared/Sub")]
    [InlineData("Orch1:\\", "")]
    [InlineData("\\Shared", "Shared")]
    public void PSPathToOrchPath_ConvertsCorrectly(string path, string expected)
    {
        Assert.Equal(expected, OrchDriveInfo.PSPathToOrchPath(path));
    }
}
