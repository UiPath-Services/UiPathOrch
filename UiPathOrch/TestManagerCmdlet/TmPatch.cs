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
}
