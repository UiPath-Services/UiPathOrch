using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests;

// Regression guard for the AS routing fix: Automation Suite requires the /orchestrator_/
// service prefix on Orchestrator API paths (/odata/..., /api/...). Cloud accepts both
// forms, so the bug is invisible until someone configures an AS deployment. The fix
// introduced _base_url_orchestrator (which adds /orchestrator_ for AS) and switched all
// orchestrator API call sites away from raw _base_url. This test scans the source for
// any new code that would re-introduce the bug.
public class BaseUrlRoutingTests
{
    private static string FindSourceRoot()
    {
        // Walk up from the test assembly location until we find UiPathOrch/UiPathOrch.csproj.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "UiPathOrch", "UiPathOrch.csproj");
            if (File.Exists(candidate))
            {
                return Path.Combine(dir.FullName, "UiPathOrch");
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate UiPathOrch source root from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void OrchestratorApiPathsMustUseBaseUrlOrchestrator()
    {
        // Pattern explanation:
        //   _base_url\b            — the field, anchored to exclude _base_url_orchestrator etc.
        //                            (a trailing _ is a word char, so \b is not satisfied there)
        //   (?: ... )              — one of two URL composition styles:
        //     \s*\+\s*\$?"/         — string concat:    _base_url + "/...   or _base_url + $"/...
        //     \}\s*/                — interpolation:    $"{_base_url}/...
        //   (odata|api)/            — Orchestrator-shaped service paths
        var bad = new Regex("_base_url\\b\\s*(?:\\+\\s*\\$?\"/|\\}\\s*/)(odata|api)/");

        string srcRoot = FindSourceRoot();

        var hits = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .SelectMany(file =>
            {
                var lines = File.ReadAllLines(file);
                return lines
                    .Select((line, idx) => (file, lineNo: idx + 1, line))
                    .Where(t => !t.line.TrimStart().StartsWith("//"))
                    .Where(t => bad.IsMatch(t.line));
            })
            .Select(t => $"  {t.file}:{t.lineNo}\n    {t.line.Trim()}")
            .ToList();

        Assert.True(hits.Count == 0,
            "Found code composing Orchestrator API URLs from `_base_url` and a literal " +
            "`/odata/...` or `/api/...` path. These must use `_base_url_orchestrator` " +
            "instead — Automation Suite refuses paths that don't start with " +
            "`/orchestrator_/`, while Cloud accepts both forms.\n\n" +
            string.Join("\n", hits));
    }
}
