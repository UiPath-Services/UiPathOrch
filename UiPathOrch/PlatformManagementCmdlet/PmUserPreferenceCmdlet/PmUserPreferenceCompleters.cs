using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Shared helpers and argument completers for Get-/Set-/Copy-PmUserPreference.

// Resolves the identity user id of the user behind a drive's connection. The current
// user's Orchestrator `Key` (a GUID) equals the identity `sub`, which is exactly the
// `userId` the Setting API expects. The PmUserPreference cmdlets act on the connected
// user's own preferences only, so a confidential application — which authenticates as
// an app, not a user — has no preferences to read or write; in that case this writes a
// descriptive error and returns null so the caller can skip the drive. Using
// CurrentUser (rather than decoding the access token) also works for PAT connections
// and doesn't depend on the token having been fetched yet.
internal static class PmUserPreferenceCurrentUser
{
    public static string? Resolve(Cmdlet cmdlet, OrchDriveInfo drive)
    {
        try
        {
            var user = drive.CurrentUser.Get();
            if (!string.IsNullOrEmpty(user?.Key)) return user.Key;
        }
        catch (Exception ex)
        {
            cmdlet.WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCurrentUserError", ErrorCategory.InvalidOperation, drive));
            return null;
        }

        cmdlet.WriteError(new ErrorRecord(
            new OrchException(drive.NameColonSeparator,
                "Cannot determine the current user. PmUserPreference cmdlets act on the connected user's own preferences, but this drive is connected with a confidential application (which authenticates as an application, not a user). Connect with a non-confidential application or a personal access token."),
            "NoCurrentUser", ErrorCategory.InvalidOperation, drive));
        return null;
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
    public const string Favorites = "Favorites";

    // The /api/Setting GET returns nothing unless given explicit key filters, so
    // Get-/Copy-PmUserPreference fetch this known set of user-preference keys
    // (matching what the Orchestrator portal requests).
    public static readonly string[] ReadDefaults =
        [Language, LanguageDate, Theme, Accessibility, Favorites];

    public static readonly (string Key, string Friendly)[] All =
    [
        (Theme, "Theme"),
        (Accessibility, "Accessibility (high contrast / OS sync)"),
        (Language, "Language"),
        (LanguageDate, "Language date format"),
        (Favorites, "Favorites"),
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
            // Insert the raw key; show "key — Friendly" in the list. Tooltip is just the
            // key (no value/description) — the friendly is only a list-time hint.
            yield return new CompletionResult(key, $"{key} — {friendly}", CompletionResultType.ParameterValue, key);
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
