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

    // A failure that crossed a Task boundary: the body is the inner exception's message, and the
    // AggregateException's own "One or more errors occurred. (...)" must not be what is shown.
    // The body is the one 21.10.4 returned for a folder listing without OR.Folders (2026-10-08).
    private const string AbpUnauthorized =
        "{\"message\":\"You are not authorized!\",\"errorCode\":0,\"result\":null,\"targetUrl\":null,\"success\":false,"
        + "\"error\":{\"code\":0,\"message\":\"You are not authorized!\",\"details\":\"You are not allowed to perform this operation.\",\"validationErrors\":null},"
        + "\"unAuthorizedRequest\":true,\"__abp\":true}";

    [Fact]
    public void ExtractMessage_ReadsThroughAggregateException()
    {
        var ex = new AggregateException(new HttpResponseException(AbpUnauthorized, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)));
        Assert.Equal("You are not authorized! You are not allowed to perform this operation.", OrchException.ExtractMessage(ex));
    }

    [Fact]
    public void OrchException_over_AggregateException_carries_the_readable_text_and_the_note()
    {
        var ex = new AggregateException(new HttpResponseException(AbpUnauthorized, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)));
        var wrapped = new OrchException("op2110:\\", ex, " NOTE.");
        Assert.Equal("\"op2110:\\\": You are not authorized! You are not allowed to perform this operation. NOTE.", wrapped.Message);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Unauthorized)]
    [InlineData(System.Net.HttpStatusCode.Forbidden)]
    public void FindHttpStatus_looks_through_wrappers(System.Net.HttpStatusCode code)
    {
        var inner = new HttpResponseException("{}", new System.Net.Http.HttpResponseMessage(code));
        Assert.Equal(code, OrchException.FindHttpStatus(new AggregateException(new OrchException("x:", inner))));
    }

    [Fact]
    public void FindHttpStatus_is_null_without_an_http_answer()
        => Assert.Null(OrchException.FindHttpStatus(new AggregateException(new TimeoutException())));

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
