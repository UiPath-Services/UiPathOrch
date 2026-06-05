using System.Linq;
using System.Management.Automation;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// OrchCollectionExtensions.SplitByUnescapedCommas resolves backtick escapes (for
// literal values), while SplitByUnescapedCommasPreservingEscapes keeps them so a
// downstream WildcardPattern -- whose own escape character is also the backtick --
// can treat `* `[ ... as literal metacharacters instead of active wildcards.
public class CommaSplitTests
{
    [Theory]
    [InlineData("a,b,c", new[] { "a", "b", "c" })]
    [InlineData(" a , b ", new[] { "a", "b" })]   // trimmed
    [InlineData("a`,b,c", new[] { "a,b", "c" })]  // escaped comma -> literal comma, backtick removed
    public void SplitByUnescapedCommas_resolves_escapes(string input, string[] expected)
        => Assert.Equal(expected, OrchCollectionExtensions.SplitByUnescapedCommas(input).ToArray());

    [Theory]
    [InlineData("a,b,c", new[] { "a", "b", "c" })]
    [InlineData("a`,b,c", new[] { "a`,b", "c" })]          // escaped comma not split, backtick PRESERVED
    [InlineData("machine`*01", new[] { "machine`*01" })]   // wildcard escape preserved
    public void SplitByUnescapedCommasPreservingEscapes_keeps_backticks(string input, string[] expected)
        => Assert.Equal(expected, OrchCollectionExtensions.SplitByUnescapedCommasPreservingEscapes(input).ToArray());

    // Regression guard for the bug where the comma splitter stripped the backtick that
    // WildcardPattern needs: a backtick-escaped wildcard metacharacter in a name must
    // reach WildcardPattern still escaped, so it matches the literal character.
    [Fact]
    public void Preserving_split_lets_WildcardPattern_match_a_literal_asterisk()
    {
        var token = OrchCollectionExtensions.SplitByUnescapedCommasPreservingEscapes("machine`*01").Single();
        var pattern = new WildcardPattern(token, WildcardOptions.IgnoreCase);
        Assert.True(pattern.IsMatch("machine*01"));    // `* -> literal *
        Assert.False(pattern.IsMatch("machineZ01"));   // not treated as a wildcard

        // Contrast: the resolving variant strips the backtick, turning * into a wildcard.
        var stripped = OrchCollectionExtensions.SplitByUnescapedCommas("machine`*01").Single();
        var wildcard = new WildcardPattern(stripped, WildcardOptions.IgnoreCase);
        Assert.True(wildcard.IsMatch("machineZ01"));    // demonstrates the pre-fix behavior
    }

    [Fact]
    public void Split1stValue_variants_only_split_the_first_element()
    {
        var resolved = new[] { "a`,b", "c,d" }.Split1stValueByUnescapedCommas()!.ToArray();
        Assert.Equal(new[] { "a,b", "c,d" }, resolved);   // 1st: escaped comma kept literal; 2nd: as-is

        var preserved = new[] { "x`*y", "z" }.Split1stValueByUnescapedCommasPreservingEscapes()!.ToArray();
        Assert.Equal(new[] { "x`*y", "z" }, preserved);   // escape preserved; 2nd: as-is
    }
}
