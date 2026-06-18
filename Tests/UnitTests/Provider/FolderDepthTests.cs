using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Pins OrchProvider.FolderDepth — the slash-counting primitive the provider's child
// enumeration and HasChildItems logic build on (depth filtering, recurse depth math,
// subfolder detection). Pure, no drive/auth required.
public class FolderDepthTests
{
    [Theory]
    [InlineData("", 0u)]
    [InlineData(null, 0u)]
    [InlineData("Shared", 1u)]
    [InlineData("Shared/Sub", 2u)]
    [InlineData("Shared/Sub/Deep", 3u)]
    public void FolderDepth_CountsSlashesPlusOne(string? orchPath, uint expected)
    {
        Assert.Equal(expected, OrchProvider.FolderDepth(orchPath!));
    }
}
