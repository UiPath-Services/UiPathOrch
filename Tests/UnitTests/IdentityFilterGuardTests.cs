using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests;

// Regression guard for the literal-first -Name migration.
//
// Identity-name parameters (-Name/-UserName/-FullName/-DisplayName/-OwnerName/-GroupName/
// -Email/-Title) must match literal-first via the FilterByNames family, NOT via the wildcard
// matchers (FilterByWildcards / SelectByWildcards / *Any). The migration kept slipping because
// the wildcard list was built in several hand-rolled forms, so a value-by-value inventory missed
// sites. This test scans the source directly: it fails if any ProcessRecord / helper (i.e. NOT an
// argument-completer) applies a wildcard matcher to an identity-field selector.
//
// Completer paths legitimately stay on wildcards (they match what the user is typing), so completer
// classes and completer-dedicated files are excluded. Role-name selection (-Roles) is intentionally
// out of scope (role names are not one of the identity params) and is allow-listed by its wpRoles arg.
//
// Scope: this guards the FilterByWildcards / SelectByWildcards (+ *Any) matcher family — the dominant
// form. The rarer inline "wpX.Any(p => p.IsMatch(entity.Field))" form is not detected here (it has no
// matcher-method anchor and would false-positive on -Roles/-License); those are kept literal-first by
// convention/review.
public class IdentityFilterGuardTests
{
    private static readonly string[] IdentityFields =
        ["Name", "UserName", "FullName", "DisplayName", "OwnerName", "GroupName", "EmailAddress", "Title"];

    private static readonly Regex MatcherCall = new(
        @"\.(FilterByWildcards|SelectByWildcards|FilterByWildcardsAny|SelectByWildcardsAny)\b",
        RegexOptions.Compiled);

    private static string FindSourceDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "UiPathOrch", "OrchestratorCmdlet");
            if (Directory.Exists(candidate)) return Path.Combine(dir.FullName, "UiPathOrch");
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the UiPathOrch source directory from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void NoMainPathIdentityFilterUsesWildcards()
    {
        var srcDir = FindSourceDir();
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            // Completer-dedicated files and the matcher definitions themselves are exempt.
            if (fileName.Contains("Completer")) continue;
            if (fileName == "OrchExtensions.cs") continue;

            var lines = File.ReadAllLines(file);
            int depth = 0;
            bool inCompleter = false;
            bool pendingCompleter = false;
            int completerExitDepth = int.MaxValue;

            foreach (var rawLine in lines)
            {
                // Strip a trailing line comment so commented-out code never trips the guard.
                int comment = rawLine.IndexOf("//", StringComparison.Ordinal);
                var line = comment >= 0 ? rawLine[..comment] : rawLine;

                if (!inCompleter && Regex.IsMatch(line, @":\s*OrchArgumentCompleter\b"))
                    pendingCompleter = true;

                // Check for a violation BEFORE updating depth: a matcher call never sits on the
                // line that opens/closes the completer class, so the current state is accurate.
                if (!inCompleter && MatcherCall.IsMatch(line)
                    && IdentityFields.Any(f => Regex.IsMatch(line, $@"\?\.{f}\b"))
                    && !line.Contains("wpRoles"))   // -Roles is intentionally out of scope
                {
                    violations.Add($"{Path.GetRelativePath(srcDir, file)}: {rawLine.Trim()}");
                }

                foreach (var c in line)
                {
                    if (c == '{')
                    {
                        depth++;
                        if (pendingCompleter) { inCompleter = true; completerExitDepth = depth - 1; pendingCompleter = false; }
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (inCompleter && depth <= completerExitDepth) { inCompleter = false; completerExitDepth = int.MaxValue; }
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Main-path identity filters must use the FilterByNames family, not wildcard matchers. Offending sites:\n"
            + string.Join("\n", violations));
    }
}
