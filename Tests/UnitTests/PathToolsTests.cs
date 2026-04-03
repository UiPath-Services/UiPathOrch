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
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("normal", "normal")]
    [InlineData("wild*card", "wild`*card")]
    [InlineData("quest?ion", "quest`?ion")]
    public void EscapePSText2_OnlyEscapesWildcards(string? input, string expected)
    {
        Assert.Equal(expected, PathTools.EscapePSText2(input));
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
}
