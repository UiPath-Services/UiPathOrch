using System.Linq;
using UiPath.PowerShell.Commands.CsvHelper;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Tests for UiPathOrchConfig.ShortenScope — the helper that collapses a
// full OAuth scope string ("OR.Assets OR.Assets.Read OR.Assets.Write")
// down to its parent scopes ("OR.Assets") when the parent grants a
// superset of the .Read/.Write children.
//
// This replaces the former Test-OrchShortenScope diagnostic cmdlet
// (UiPathOrch/CommonCmdlet/TestShortenScope.cs), which was never exported
// in the manifest and existed only as a manual harness driven by
// `Import-Csv ShortenScopeTestCases.csv | Test-OrchShortenScope`. The
// same CSV (TestData/ShortenScopeTestCases.csv) is now the data source
// for these xUnit cases, so the coverage is preserved but runs in CI
// instead of needing a hand-run with the cmdlet temporarily made public.
public class ShortenScopeTests
{
    public static System.Collections.Generic.IEnumerable<object[]> ScopeCases()
    {
        var csv = LocateTestData("ShortenScopeTestCases.csv");
        // RFC4180 split via the same helper the production user-mapping
        // CSV parser uses. Header row: Id,Input,Expected.
        var lines = System.IO.File.ReadAllLines(csv);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var fields = CsvLine.Split(lines[i]);
            // Fields: [0]=Id, [1]=Input, [2]=Expected. Missing trailing
            // fields (e.g. the empty-scope row "1,,") read back as "".
            string id = fields.Count > 0 ? fields[0] : "";
            string? input = fields.Count > 1 ? NullIfEmpty(fields[1]) : null;
            string? expected = fields.Count > 2 ? NullIfEmpty(fields[2]) : null;
            yield return new object[] { id, input!, expected! };
        }
    }

    [Theory]
    [MemberData(nameof(ScopeCases))]
    public void ShortenScope_MatchesExpected(string id, string input, string expected)
    {
        var actual = UiPathOrchConfig.ShortenScope(input);
        Assert.True(string.Equals(expected, actual, System.StringComparison.Ordinal),
            $"Case {id}: ShortenScope('{input}') => '{actual}', expected '{expected}'.");
    }

    [Fact]
    public void ShortenScope_Null_ReturnsNull()
    {
        Assert.Null(UiPathOrchConfig.ShortenScope(null));
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    private static string LocateTestData(string fileName)
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "TestData", fileName);
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new System.IO.FileNotFoundException(
            $"TestData/{fileName} not found above " + System.AppContext.BaseDirectory);
    }
}
