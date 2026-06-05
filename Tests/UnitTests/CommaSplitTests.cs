using System.Linq;
using System.Management.Automation;
using UiPath.PowerShell.Commands;
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

    // Every element is split on unescaped commas (previously only the first element
    // was, which made the result position-dependent). A native a,b,c array arrives
    // already split, so each element is a no-op; a comma in any element now splits.
    [Fact]
    public void Split1stValue_splits_every_element_position_independently()
    {
        // A comma in the SECOND element now splits the same as in the first.
        Assert.Equal(new[] { "a", "b", "c" }, new[] { "a", "b,c" }.SplitValuesByUnescapedCommas()!.ToArray());
        Assert.Equal(new[] { "a", "b", "c" }, new[] { "a,b", "c" }.SplitValuesByUnescapedCommas()!.ToArray());
        Assert.Equal(new[] { "a", "b", "c" }, new[] { "a", "b", "c" }.SplitValuesByUnescapedCommas()!.ToArray());

        // Escaped comma stays literal in any position; other elements still split.
        Assert.Equal(new[] { "a,b", "c", "d" }, new[] { "a`,b", "c,d" }.SplitValuesByUnescapedCommas()!.ToArray());

        // Preserving variant: backtick escapes survive, every element split.
        Assert.Equal(new[] { "x`*y", "z", "q" }, new[] { "x`*y,z", "q" }.SplitValuesByUnescapedCommasPreservingEscapes()!.ToArray());
    }

    // A list-attribute element containing a comma (or a wildcard metacharacter) round-trips:
    // export escapes it (WildcardPattern.Escape + the `, comma-escape, CSV-quoted), and import
    // un-splits it so WildcardPattern reads the comma/metachar as a literal.
    [Fact]
    public void List_element_with_comma_or_wildcard_round_trips()
    {
        var cell = OrchestratorPSCmdlet.EscapeCsvValue(new[] { "Admin, Special", "Robot*" }, true);
        // Import-Csv strips the surrounding quotes and un-doubles internal quotes.
        var field = cell.StartsWith("\"") && cell.EndsWith("\"") ? cell[1..^1].Replace("\"\"", "\"") : cell;
        var items = OrchCollectionExtensions.SplitValuesByUnescapedCommasPreservingEscapes(new[] { field })!.ToArray();
        Assert.Equal(2, items.Length);
        Assert.True(new WildcardPattern(items[0], WildcardOptions.IgnoreCase).IsMatch("Admin, Special")); // `, -> literal comma
        Assert.True(new WildcardPattern(items[1], WildcardOptions.IgnoreCase).IsMatch("Robot*"));         // `* -> literal *
        Assert.False(new WildcardPattern(items[1], WildcardOptions.IgnoreCase).IsMatch("RobotZ"));        // not a wildcard
    }

    // New-PmUser -GroupName literal branch: a backtick-escaped metacharacter must be treated as a
    // literal group name (not a wildcard) and unescaped before use, so it round-trips with the
    // WildcardPattern.Escaped export.
    [Fact]
    public void Backtick_escaped_metachar_takes_the_literal_path_and_unescapes()
    {
        var token = OrchCollectionExtensions.SplitValuesByUnescapedCommasPreservingEscapes(new[] { "Sales`*" }).Single();
        Assert.Equal("Sales`*", token);                                  // preserving keeps the escape
        Assert.False(WildcardPattern.ContainsWildcardCharacters(token)); // escaped -> not a wildcard
        Assert.Equal("Sales*", WildcardPattern.Unescape(token));         // literal name after unescape
    }
}
