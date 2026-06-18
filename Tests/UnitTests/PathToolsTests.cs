using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

public class PathToolsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("simple", "simple")]
    [InlineData("has space", "'has space'")]
    [InlineData("has,comma", "'has,comma'")]
    [InlineData("wild*card", "'wild`*card'")]    // * escaped with backtick, then quoted
    [InlineData("quest?ion", "'quest`?ion'")]
    [InlineData("brack[et", "'brack`[et'")]
    [InlineData("brack]et", "'brack`]et'")]
    [InlineData("back`tick", "'back``tick'")]
    [InlineData("single'quote", "'single''quote'")]
    public void EscapePSText_EscapesCorrectly(string? input, string expected)
    {
        Assert.Equal(expected, PathTools.EscapePSText(input));
    }

    [Theory]
    [InlineData(null, "''")]
    [InlineData("", "''")]
    [InlineData("simple", "'simple'")]
    [InlineData("single'quote", "'single''quote'")]
    public void EscapeNonWildcardText_QuotesAndEscapesSingleQuotes(string? input, string expected)
    {
        Assert.Equal(expected, PathTools.EscapeNonWildcardText(input));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("x", false)]
    [InlineData("'quoted'", true)]
    [InlineData("'x'", true)]
    [InlineData("''", true)]
    public void IsEscapedPSText_DetectsQuotedStrings(string? input, bool expected)
    {
        Assert.Equal(expected, PathTools.IsEscapedPSText(input));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("unquoted", "unquoted")]
    [InlineData("'quoted'", "quoted")]
    [InlineData("'has''quote'", "has'quote")]
    public void UnescapePSText_RoundTrips(string? input, string expected)
    {
        Assert.Equal(expected, PathTools.UnescapePSText(input));
    }

    // A bare drive parent keeps its separator ("Orch1:" -> "Orch1:\"), matching FileSystemProvider;
    // everything else is returned unchanged. Uses the OS separator so the expectation holds on the
    // Linux/macOS CI legs too.
    [Fact]
    public void ParentPathWithDriveRoot_reroots_only_a_bare_drive()
    {
        string sep = System.IO.Path.DirectorySeparatorChar.ToString();

        Assert.Equal("Orch1:" + sep, PathTools.ParentPathWithDriveRoot("Orch1:"));
        Assert.Equal("Du1:" + sep, PathTools.ParentPathWithDriveRoot("Du1:"));
        // Non-root parents and empties are untouched.
        Assert.Equal("Orch1:" + sep + "Shared", PathTools.ParentPathWithDriveRoot("Orch1:" + sep + "Shared"));
        Assert.Equal("", PathTools.ParentPathWithDriveRoot(""));
    }

    // Convert the readable '\' separators in the InlineData to the running OS separator so the
    // expectations hold on the Linux/macOS CI legs too.
    private static string P(string s) => s.Replace('\\', System.IO.Path.DirectorySeparatorChar);

    // The leaf side of the drive-root re-rooting (symmetric to ParentPathWithDriveRoot). A bare
    // drive re-roots ("Orch1:" -> "Orch1:\") so Split-Path X:\ -Leaf yields "X:\", not "X:";
    // every other case returns the last segment with any trailing separator trimmed.
    [Theory]
    [InlineData("Orch1:\\Shared", "Shared")]
    [InlineData("Orch1:\\A\\B", "B")]
    [InlineData("Orch1:\\Shared\\", "Shared")]   // trailing separator trimmed first
    [InlineData("Orch1:\\", "Orch1:\\")]          // drive root re-rooted
    [InlineData("Orch1:", "Orch1:\\")]            // bare drive re-rooted
    public void GetChildNameWithDriveRoot_returnsLeaf_andRerootsBareDrive(string path, string expected)
    {
        Assert.Equal(P(expected), PathTools.GetChildNameWithDriveRoot(P(path)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetChildNameWithDriveRoot_throwsOnNullOrEmpty(string? path)
    {
        Assert.Throws<System.ArgumentException>(() => PathTools.GetChildNameWithDriveRoot(path!));
    }

    // Syntactic-only validity (NOT existence): null/empty are invalid, the drive root is valid, a
    // normal folder path is valid, and a control character (which can never appear in a real folder
    // name) is rejected.
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Orch1:\\", true)]            // drive root
    [InlineData("Orch1:\\Shared", true)]
    [InlineData("Orch1:\\Shared\\Deep", true)]
    public void IsValidProviderPath_acceptsWellFormedPaths(string? path, bool expected)
    {
        Assert.Equal(expected, PathTools.IsValidProviderPath(path is null ? null! : P(path)));
    }

    [Fact]
    public void IsValidProviderPath_rejectsControlCharactersInAName()
    {
        // A bell (U+0007) inside a folder name is a control char -> invalid.
        string path = P("Orch1:\\Foo") + (char)7 + "Bar";
        Assert.False(PathTools.IsValidProviderPath(path));
    }
}
