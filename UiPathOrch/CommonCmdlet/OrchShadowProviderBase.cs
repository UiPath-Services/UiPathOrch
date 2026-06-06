using System.Management.Automation.Provider;

namespace UiPath.PowerShell.Core;

// Shared base for the flat "shadow" providers (Document Understanding, Test Manager) that mirror
// their parent Orchestrator drive with a single level of projects. Both expose only containers
// (projects, no leaf items) and share identical path normalization; the ONLY per-provider
// difference is which cache + key field canonicalizes a project name's casing — supplied by the
// CanonicalizeProjectName hook. The hierarchical OrchProvider deliberately does NOT derive from
// this (its folder model differs); keep it standalone.
public abstract class OrchShadowProviderBase : NavigationCmdletProvider
{
    // These providers only ever return containers, and every path that resolves to a project is
    // valid; there is no leaf/file concept to validate.
    protected override bool IsValidPath(string path) => true;

    protected override bool IsItemContainer(string path) => true;

    protected override string MakePath(string parent, string child)
    {
        string result = base.MakePath(parent, child);
        // Trim a trailing separator (but keep the drive root "X:\").
        if (result.EndsWith(System.IO.Path.DirectorySeparatorChar) && result.Length > 1 && result[^2] != ':')
            result = result[..^1];
        return result;
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        string result = base.NormalizeRelativePath(path, basePath);
        if (result.StartsWith(System.IO.Path.DirectorySeparatorChar) && result.Length > 1)
            result = result[1..];

        // Canonicalize the single-segment project name's casing from cache. Passive read — it
        // must NOT trigger a fetch (this runs on every path op, including tab completion).
        if (!string.IsNullOrEmpty(result))
        {
            string? canonical = CanonicalizeProjectName(result);
            if (canonical is not null)
                result = canonical;
        }

        return result ?? "";
    }

    // Returns the cache-cased project name matching `name` case-insensitively, or null when the
    // cache is cold or there is no match. MUST be a passive cache read (no server fetch).
    protected abstract string? CanonicalizeProjectName(string name);
}
