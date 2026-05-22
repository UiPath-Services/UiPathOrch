using System.Collections;
using System.Linq;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using Xunit;

namespace UnitTests;

// Behavior tests for the TimeZoneIdCompleter (added in v1.5.3).
// The completer's contract is "emit candidates that are valid input
// values for the -TimeZoneId parameter". A common mistake is to wire
// up TimeZoneCompleter (which emits DisplayName like "(UTC+09:00) ...")
// on a -TimeZoneId parameter, producing candidates the parameter
// itself rejects. These tests verify the new completer emits Id
// values exclusively.
public class TimeZoneIdCompleterTests
{
    private static IEnumerable<System.Management.Automation.CompletionResult> Run(string wordToComplete)
    {
        var completer = new TimeZoneIdCompleter();
        // commandAst is unused by this completer; pass a bare-minimum AST.
        var fakeAst = (CommandAst?)null!;
        return completer.CompleteArgument(
            commandName: "New-OrchTestSetSchedule",
            parameterName: "TimeZoneId",
            wordToComplete: wordToComplete,
            commandAst: fakeAst,
            fakeBoundParameters: new Hashtable());
    }

    [Fact]
    public void EmitsCanonicalTokyoId_NotDisplayName()
    {
        // Search for Tokyo — both `Id` ("Tokyo Standard Time") and
        // `DisplayName` ("(UTC+09:00) Osaka, Sapporo, Tokyo") match the
        // wildcard; either is acceptable for filtering, but the emitted
        // CompletionText must be the Id.
        var results = Run("Tokyo").ToList();
        Assert.NotEmpty(results);
        Assert.Contains(results, r =>
            r.CompletionText.Contains("Tokyo Standard Time", System.StringComparison.OrdinalIgnoreCase));
        // Nothing emitted should start with the DisplayName format `(UTC` —
        // that's the wrong shape for a -TimeZoneId parameter.
        Assert.DoesNotContain(results, r =>
            r.CompletionText.TrimStart('\'').StartsWith("(UTC", System.StringComparison.Ordinal));
    }

    [Fact]
    public void EmittedTexts_AreActualTimeZoneInfoIds()
    {
        // Every completion the completer emits must round-trip through
        // TimeZoneInfo.FindSystemTimeZoneById — i.e., must be a valid Id.
        var validIds = TimeZoneInfo.GetSystemTimeZones().Select(t => t.Id).ToHashSet();
        var results = Run("*").Take(50).ToList();
        Assert.NotEmpty(results);
        foreach (var r in results)
        {
            // CompletionText is the PSText-escaped value (may have a leading '); compare the ListItemText.
            Assert.True(validIds.Contains(r.ListItemText),
                $"TimeZoneIdCompleter emitted '{r.ListItemText}' which is NOT a TimeZoneInfo.Id — would be rejected as -TimeZoneId input.");
        }
    }

    [Fact]
    public void Tooltip_ShowsDisplayNameAndId()
    {
        // For the user's benefit the tooltip should carry the DisplayName
        // alongside the Id so they can pick the right region. The shape
        // matches the existing TimeZoneCompleter tooltip format
        // "<DisplayName> (Id = '<Id>')".
        var results = Run("Tokyo").ToList();
        var tokyo = results.FirstOrDefault(r =>
            r.ListItemText.Equals("Tokyo Standard Time", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(tokyo);
        Assert.Contains("Tokyo Standard Time", tokyo!.ToolTip);
        // DisplayName-style text (UTC offset) should also be in the tooltip.
        Assert.Matches(@"\(UTC[+\-]?\d", tokyo.ToolTip);
    }

    [Fact]
    public void WildcardWord_MatchesViaDisplayName_NotJustId()
    {
        // The completer accepts a wildcard against EITHER Id or
        // DisplayName. "Osaka" doesn't appear in any TimeZone Id but
        // is in the DisplayName "(UTC+09:00) Osaka, Sapporo, Tokyo" —
        // an explicit `*Osaka*` wildcard returns the JST entry by
        // DisplayName match, with Id as the emitted text.
        // (CreateWPFromWordToComplete auto-appends `*` to plain words
        // for prefix matching, so plain "Osaka" wouldn't find anything;
        // the user explicitly wildcards to opt into substring search.)
        var results = Run("*Osaka*").ToList();
        Assert.NotEmpty(results);
        Assert.Contains(results, r =>
            r.ListItemText.Equals("Tokyo Standard Time", System.StringComparison.OrdinalIgnoreCase));
        Assert.All(results, r => Assert.False(string.IsNullOrEmpty(r.ListItemText)));
    }
}
