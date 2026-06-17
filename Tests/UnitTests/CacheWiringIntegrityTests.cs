using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests;

// Wiring-integrity guard for OrchDriveInfo's per-drive cache registry. Each cache
// is a `public readonly <...>Cache<...> Foo;` field that MUST be assigned
// (`Foo = new(...)`) in the constructor. C# does not require a readonly reference
// field to be assigned, so a declared-but-unwired cache compiles fine and then
// throws NullReferenceException on first use — a failure the compiler can't catch.
// This test makes "declared a cache field but forgot to wire it" a build failure,
// extending the same forget-a-site safety net as ModuleManifestSanityTests
// (manifest) and CmdletHelpParityTests (help) down to the cache layer.
//
// Investigation (2026-06-17): all 107 cache fields are currently wired, so this
// goes in green. Wirings use several valid shapes — new(this, getter, ...),
// new(this), and new ExplicitType<...>( ... ) — so the "is wired" check matches
// any `Field = new`. Commented-out declarations (e.g. an old superseded field)
// are ignored.
public class CacheWiringIntegrityTests
{
    private static string LocateDriveInfo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "UiPathOrch", "OrchestratorCmdlet", "OrchDriveInfo.cs");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("OrchDriveInfo.cs not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Every_declared_cache_field_is_wired_in_the_constructor()
    {
        // Active (non-comment) source lines only — a commented-out declaration is
        // not a real field, and a commented-out assignment is not real wiring.
        var active = File.ReadAllLines(LocateDriveInfo())
            .Select(l => l.TrimStart())
            .Where(l => !l.StartsWith("//"))
            .ToList();

        // `public readonly <Something>Cache<...> FieldName;`
        var declRx = new Regex(@"public readonly \w+Cache\w*<[^;]+?>\s+(?<name>\w+)\s*;");
        // `FieldName = new` — any constructor shape (new(this,...), new(this), new ExplicitType<...>(...)).
        var wireRx = new Regex(@"(?<name>\w+)\s*=\s*new\b");

        var declared = active
            .Select(l => declRx.Match(l)).Where(m => m.Success)
            .Select(m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var wired = active
            .Select(l => wireRx.Match(l)).Where(m => m.Success)
            .Select(m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        // Sanity: the registry exists and we actually parsed it (guards against a
        // future refactor that moves/renames the fields and silently matches none).
        Assert.True(declared.Count >= 100,
            $"Expected to find the OrchDriveInfo cache registry (>=100 fields) but parsed " +
            $"{declared.Count}. Did the declaration shape change? Update declRx.");

        var unwired = declared.Where(f => !wired.Contains(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();

        Assert.True(unwired.Count == 0,
            "These OrchDriveInfo cache fields are declared but never assigned (`<field> = new(...)`) " +
            "in the constructor. A readonly reference field left unassigned is null at runtime and " +
            "throws NullReferenceException on first use — wire it in the constructor:\n  " +
            string.Join("\n  ", unwired));
    }
}
