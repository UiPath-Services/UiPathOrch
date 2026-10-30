using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Builds the JSON Patch bodies the Test Manager PATCH endpoints take (requirements, test cases).
internal static class TmPatch
{
    // Adds a "replace" of `path` when `value` was given AND differs from `current`. A null current
    // counts as "", so passing "" for a field that is already empty sends nothing.
    internal static void ReplaceIfChanged(List<TmPatchOperation> operations, string path, string? value, string? current)
    {
        if (value is not null && value != (current ?? ""))
        {
            operations.Add(new TmPatchOperation { op = "replace", path = path, value = value });
        }
    }

    // Boolean field: a "replace" when `value` was given AND differs from `current` (null = false).
    internal static void ReplaceIfChanged(List<TmPatchOperation> operations, string path, bool? value, bool? current)
    {
        if (value is bool v && v != (current ?? false))
        {
            operations.Add(new TmPatchOperation { op = "replace", path = path, value = v });
        }
    }

    // Parses a true/false parameter (string-typed, as the module's other boolean parameters are,
    // so BoolCompleter can offer the values). Null/empty = not given; anything else must parse.
    internal static bool? ParseBool(string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (bool.TryParse(value, out var result)) return result;
        throw new ArgumentException($"-{parameterName} must be true or false, not '{value}'.", parameterName);
    }
}
