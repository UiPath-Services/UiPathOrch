using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Shared argument completers for Get-/Set-/Copy-PmUserPreference.

// Suggests organization users by userName for -UserName.
internal class PmUserPreferenceUserNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);
        var wpSelf = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive => drive.PmUsers.Get());
        foreach (var result in results)
        {
            foreach (var user in result
                .Where(u => u is not null && !string.IsNullOrEmpty(u.userName))
                .Where(u => wp.IsMatch(u!.userName))
                .ExcludeByWildcards(u => u!.userName!, wpSelf)
                .OrderBy(u => u!.userName))
            {
                yield return new CompletionResult(PathTools.EscapePSText(user!.userName), user.userName!, CompletionResultType.ParameterValue, user.userName!);
            }
        }
    }
}

// Known identity setting keys used by the portal, each with a friendly label.
// The endpoint is generic (any key is accepted), so these only drive completion.
internal static class PmUserPreferenceKeys
{
    public const string Theme = "UserTheme.Theme";
    public const string Accessibility = "UserAccessibility.Accessibility";
    public const string Language = "UserLanguage.Language";
    public const string LanguageDate = "UserLanguage.Date";

    public static readonly (string Key, string Friendly)[] All =
    [
        (Theme, "Theme"),
        (Accessibility, "Accessibility (high contrast / OS sync)"),
        (Language, "Language"),
        (LanguageDate, "Language date format"),
    ];

    // Suggested values per key, each with a friendly label. The completion text
    // is always the raw Value (e.g. "ja") so nothing friendly ever leaks into the
    // command line or CSV; the Friendly label is display-only (list item + tooltip).
    public static readonly Dictionary<string, (string Value, string Friendly)[]> ValuesByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        [Theme] =
        [
            ("light", "Light"),
            ("dark", "Dark"),
            ("dark-hc", "High-contrast dark"),
        ],
        [Accessibility] =
        [
            ("true", "High-contrast light"),
            ("false", "Sync with OS settings"),
        ],
        [Language] =
        [
            ("en", "English"),
            ("ja", "日本語"),
            ("de", "Deutsch"),
            ("es", "Español"),
            ("es-mx", "Español (México)"),
            ("fr", "Français"),
            ("ko", "한국어"),
            ("pt", "Português"),
            ("pt-br", "Português (Brasil)"),
            ("ru", "Русский"),
            ("tr", "Türkçe"),
            ("zh-cn", "中文 (简体)"),
            ("zh-tw", "中文 (繁體)"),
        ],
    };
}

internal class PmPreferenceKeyCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wp = CreateWPFromWordToComplete(wordToComplete);
        foreach (var (key, friendly) in PmUserPreferenceKeys.All.Where(k => wp.IsMatch(k.Key)))
        {
            // Insert the raw key; show "key — Friendly" in the list, Friendly as tooltip.
            yield return new CompletionResult(key, $"{key} — {friendly}", CompletionResultType.ParameterValue, friendly);
        }
    }
}

// -Value completer that adapts to the -Key already bound on the command line:
// e.g. -Key UserLanguage.Language lists ja (日本語), en (English), ...
internal class PmPreferenceValueCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        string? key = null;
        if (fakeBoundParameters.Contains("Key"))
        {
            key = fakeBoundParameters["Key"] switch
            {
                string s => s,
                IEnumerable e and not string => e.Cast<object?>().FirstOrDefault()?.ToString(),
                var o => o?.ToString()
            };
        }
        if (string.IsNullOrEmpty(key) || !PmUserPreferenceKeys.ValuesByKey.TryGetValue(key, out var values))
        {
            yield break;
        }

        var wp = CreateWPFromWordToComplete(wordToComplete);
        foreach (var (value, friendly) in values.Where(v => wp.IsMatch(v.Value)))
        {
            // Insert the raw value (e.g. "ja"); show "ja — 日本語" in the list,
            // the friendly name as tooltip.
            yield return new CompletionResult(value, $"{value} — {friendly}", CompletionResultType.ParameterValue, friendly);
        }
    }
}
