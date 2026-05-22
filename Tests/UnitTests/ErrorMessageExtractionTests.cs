using System;
using System.Collections.Generic;
using System.IO;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Corpus-driven regression for OrchException.ExtractMessage — the helper that
// turns the many shapes of Orchestrator / Identity error envelopes into a
// readable one-liner. Each case in TestData/ErrorMessageExtraction.tsv is a
// real error body plus a substring that MUST appear in the extracted message.
//
// Growing the corpus: when an API call returns an error you had to read raw,
// add one TAB-separated line to the .tsv (expected-substring <TAB> raw-json).
// No code change needed — this Theory picks it up automatically.
public class ErrorMessageExtractionTests
{
    public static IEnumerable<object[]> ErrorCases()
    {
        var file = LocateTestData("ErrorMessageExtraction.tsv");
        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            int tab = line.IndexOf('\t');
            if (tab < 0) continue; // malformed row — skip rather than fail the whole theory
            string expected = line.Substring(0, tab).Trim();
            string json = line.Substring(tab + 1);
            yield return new object[] { expected, json };
        }
    }

    [Theory]
    [MemberData(nameof(ErrorCases))]
    public void ExtractMessage_SurfacesReadableText(string expected, string json)
    {
        string? actual = OrchException.ExtractMessage(json);

        Assert.False(string.IsNullOrEmpty(actual), "ExtractMessage returned null/empty.");
        Assert.Contains(expected, actual, StringComparison.Ordinal);

        // It must surface a message, not echo the raw envelope back, and must
        // not leak the noise fields (trace id) into the user-facing text.
        Assert.NotEqual(json, actual);
        Assert.DoesNotContain("traceId", actual!, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractMessage_NonJson_PassesThrough()
    {
        // Plain-text (non-JSON) error bodies must round-trip unchanged.
        const string plain = "The remote server returned an error: (502) Bad Gateway.";
        Assert.Equal(plain, OrchException.ExtractMessage(plain));
    }

    private static string LocateTestData(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "TestData", fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(
            $"TestData/{fileName} not found above " + AppContext.BaseDirectory);
    }
}
