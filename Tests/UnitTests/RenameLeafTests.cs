using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// PathTools.RenameLeaf normalizes Rename-Item's -NewName (which PowerShell passes verbatim) the
// way the FileSystem provider does: strip a leading "./" / ".\", allow a same-directory full path,
// otherwise require a bare leaf. Rename-Item renames in place, so a -NewName that points elsewhere
// (e.g. "..\sub5") is rejected (returns null) rather than silently reduced.
public class RenameLeafTests
{
    private const string Src = "Orch1:\\src\\sub3"; // the item being renamed

    [Theory]
    [InlineData(".\\Shared2", "Shared2")]
    [InlineData("./Shared2", "Shared2")]
    [InlineData("Shared2", "Shared2")]
    [InlineData(".\\Shared2\\", "Shared2")]
    [InlineData(".\\My Folder", "My Folder")]
    [InlineData(".config", ".config")]
    [InlineData(".\\.config", ".config")]
    [InlineData("Orch1:\\src\\Shared2", "Shared2")] // same directory as the source -> leaf allowed
    public void ReducesToLeaf(string input, string expected)
        => Assert.Equal(expected, PathTools.RenameLeaf(Src, input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(".\\")]
    [InlineData("..\\sub5")]              // parent-directory move attempt -> reject
    [InlineData("../sub5")]
    [InlineData("sub\\Shared2")]          // into a subfolder -> reject
    [InlineData("Orch1:\\other\\Shared2")] // a different folder -> reject
    public void ReturnsNullForNonLeafOrMove(string? input)
        => Assert.Null(PathTools.RenameLeaf(Src, input));
}
