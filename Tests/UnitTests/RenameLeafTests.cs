using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// PathTools.RenameLeaf normalizes Rename-Item's -NewName (which PowerShell passes verbatim) to a
// leaf name, so `ren .\Shared .\Shared2` renames the folder to "Shared2", not ".\Shared2".
public class RenameLeafTests
{
    [Theory]
    [InlineData(".\\Shared2", "Shared2")]
    [InlineData("./Shared2", "Shared2")]
    [InlineData("Shared2", "Shared2")]
    [InlineData(".\\Shared2\\", "Shared2")]
    [InlineData("sub\\Shared2", "Shared2")]
    [InlineData(".\\My Folder", "My Folder")]
    [InlineData(".config", ".config")]
    [InlineData(".\\.config", ".config")]
    public void ReducesToLeaf(string input, string expected)
        => Assert.Equal(expected, PathTools.RenameLeaf(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(".\\")]
    public void ReturnsNullForNonNames(string? input)
        => Assert.Null(PathTools.RenameLeaf(input));
}
