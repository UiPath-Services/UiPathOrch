using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using Xunit;

namespace UnitTests;

// Argument completers run on the PSReadLine thread during <Tab>, and most of
// them do blocking network I/O (drive.<cache>.Get) that can re-throw a cached
// OrchException or fault ParallelResults.GroupBy as an AggregateException. The
// OrchArgumentCompleter base wraps the derived CompleteArgumentCore so such a
// failure degrades to "no completions" rather than erroring in the user's
// prompt. These tests pin that contract — including that a failure mid-stream
// still yields what was produced first, and that cancellation is NOT swallowed.
public class CompleterExceptionSafetyTests
{
    private sealed class ThrowsBeforeYield : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName, string parameterName, string wordToComplete,
            CommandAst commandAst, IDictionary fakeBoundParameters)
            => throw new InvalidOperationException("boom (e.g. cached OrchException re-thrown by .Get)");
    }

    private sealed class ThrowsMidEnumeration : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName, string parameterName, string wordToComplete,
            CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            yield return new CompletionResult("first");
            yield return new CompletionResult("second");
            throw new InvalidOperationException("boom mid-stream (e.g. GroupBy AggregateException)");
        }
    }

    private sealed class ThrowsCancellation : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName, string parameterName, string wordToComplete,
            CommandAst commandAst, IDictionary fakeBoundParameters)
            => throw new OperationCanceledException();
    }

    private sealed class Clean : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName, string parameterName, string wordToComplete,
            CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            yield return new CompletionResult("only");
        }
    }

    private static List<CompletionResult> Run(OrchArgumentCompleter completer)
        => completer.CompleteArgument("Cmd", "Param", "", (CommandAst?)null!, new Hashtable()).ToList();

    [Fact]
    public void ThrowBeforeYield_ReturnsEmpty_DoesNotThrow()
    {
        Assert.Empty(Run(new ThrowsBeforeYield()));
    }

    [Fact]
    public void ThrowMidEnumeration_ReturnsResultsBeforeFailure_DoesNotThrow()
    {
        var results = Run(new ThrowsMidEnumeration());
        Assert.Equal(new[] { "first", "second" }, results.Select(r => r.CompletionText));
    }

    [Fact]
    public void Cancellation_IsNotSwallowed()
    {
        Assert.Throws<OperationCanceledException>(() => Run(new ThrowsCancellation()));
    }

    [Fact]
    public void CleanCompleter_YieldsNormally()
    {
        var results = Run(new Clean());
        Assert.Single(results);
        Assert.Equal("only", results[0].CompletionText);
    }
}
