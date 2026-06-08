using System.Management.Automation;

namespace UiPath.PowerShell.Positional;

internal static class DictionaryItemsExtensions
{
    // Resolves a *user-supplied* key against an *Items lookup dictionary.
    //
    // On a miss it throws a PSArgumentException naming the parameter and listing
    // the valid keys, instead of the bare indexer's opaque
    //   "The given key 'X' was not present in the dictionary."
    // Use this for values that come from a cmdlet parameter (e.g. Get-OrchUserSession
    // -OrderBy / -State / -Type), where an unrecognized value is user error and the
    // run should stop with a helpful message.
    //
    // For codes that come from an *API response* (e.g. license bundle codes echoed
    // back by Orchestrator) do NOT use this — prefer Items.GetValueOrDefault(code,
    // code) so a server value the module does not yet know about passes through as
    // the raw code instead of crashing a read. See CopyUser for the prior ad-hoc
    // GetValueOrDefault usage this convention generalizes.
    internal static TValue ResolveKeyOrThrow<TValue>(
        this Dictionary<string, TValue> items, string? key, string parameterName)
    {
        if (key is not null && items.TryGetValue(key, out var value))
            return value;

        string valid = string.Join(", ", items.Keys);
        throw new PSArgumentException(
            $"'{key}' is not a valid value for -{parameterName}. Valid values: {valid}.",
            parameterName);
    }
}
