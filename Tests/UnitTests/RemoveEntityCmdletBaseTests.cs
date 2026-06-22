using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Pins the behaviour of RemoveEntityCmdletBase.RemoveMatching — the per-entity loop shared by
// RemoveFolderEntityCmdletBase and RemoveDriveEntityCmdletBase (extracted so the two scopes can't
// drift). Driven directly with a fake ICommandRuntime (no runspace), the loop must:
//   * delete only the entities whose Name matches the -Name wildcards, in sorted order;
//   * gate every delete behind ShouldProcess (skip the delete when it returns false);
//   * turn a failing delete into a non-terminating error (Remove{Noun}Error / target = the entity)
//     and KEEP GOING with the remaining entities.
public class RemoveEntityCmdletBaseTests
{
    private sealed class Widget
    {
        public string WidgetName = "";
    }

    // Minimal concrete cmdlet over the shared base; exposes the protected loop for direct calls.
    private sealed class TestRemoveCmdlet : RemoveEntityCmdletBase<Widget>
    {
        protected override string EntityNoun => "Widget";
        protected override Func<Widget?, string?> GetName => w => w?.WidgetName;
        protected override Func<Widget, string> GetPSPath => w => $"Test:\\{w.WidgetName}";

        public void Run(IEnumerable<Widget> entities, IReadOnlyList<WildcardPattern>? wpName, Action<Widget> remove)
            => RemoveMatching(entities, wpName, "Test:", remove, CancellationToken.None);
    }

    private static (TestRemoveCmdlet cmdlet, RecordingCommandRuntime runtime) NewCmdlet()
    {
        var runtime = new RecordingCommandRuntime();
        var cmdlet = new TestRemoveCmdlet { CommandRuntime = runtime };
        return (cmdlet, runtime);
    }

    private static Widget[] Widgets(params string[] names)
    {
        var result = new Widget[names.Length];
        for (int i = 0; i < names.Length; i++) result[i] = new Widget { WidgetName = names[i] };
        return result;
    }

    [Fact]
    public void Removes_only_name_matches_in_sorted_order()
    {
        var (cmdlet, _) = NewCmdlet();
        var removed = new List<string>();
        var entities = Widgets("gamma", "alpha", "beta", "zeta");
        var wpName = new[] { "a*", "b*", "g*" }.ConvertToWildcardPatternList();

        cmdlet.Run(entities, wpName, w => removed.Add(w.WidgetName));

        // "zeta" filtered out (no pattern matches); the rest deleted in sorted order.
        Assert.Equal(new[] { "alpha", "beta", "gamma" }, removed);
    }

    [Fact]
    public void Null_name_patterns_remove_all_in_sorted_order()
    {
        var (cmdlet, _) = NewCmdlet();
        var removed = new List<string>();
        var entities = Widgets("b", "a", "c");

        cmdlet.Run(entities, null, w => removed.Add(w.WidgetName));

        Assert.Equal(new[] { "a", "b", "c" }, removed);
    }

    [Fact]
    public void ShouldProcess_false_skips_the_delete_and_writes_no_error()
    {
        var (cmdlet, runtime) = NewCmdlet();
        runtime.ShouldProcessResult = false;
        var removed = new List<string>();
        var entities = Widgets("a", "b");

        cmdlet.Run(entities, null, w => removed.Add(w.WidgetName));

        Assert.Empty(removed);                                                  // nothing deleted
        Assert.Empty(runtime.Errors);                                           // no error raised
        Assert.Equal(new[] { "Test:\\a", "Test:\\b" }, runtime.ShouldProcessTargets); // each consulted, in order
    }

    [Fact]
    public void Failing_delete_is_nonterminating_and_processing_continues()
    {
        var (cmdlet, runtime) = NewCmdlet();
        var attempted = new List<string>();
        var entities = Widgets("a", "boom", "c");   // sorted: a < boom < c
        var boom = entities[1];

        cmdlet.Run(entities, null, w =>
        {
            attempted.Add(w.WidgetName);
            if (w.WidgetName == "boom") throw new InvalidOperationException("kaboom");
        });

        // "c" was still attempted after "boom" threw -> the loop continued past the failure.
        Assert.Equal(new[] { "a", "boom", "c" }, attempted);

        var err = Assert.Single(runtime.Errors);
        Assert.StartsWith("RemoveWidgetError", err.FullyQualifiedErrorId);
        Assert.Equal(ErrorCategory.InvalidOperation, err.CategoryInfo.Category);
        Assert.Same(boom, err.TargetObject);                 // the offending entity is the target
        Assert.IsType<OrchException>(err.Exception);         // wrapped, not the raw InvalidOperationException
    }
}

// Records WriteError/ShouldProcess and lets a test pick the ShouldProcess outcome, so cmdlet logic
// can be exercised without a runspace. Everything else is a no-op (the loop under test never calls
// it). Host is null on purpose: RendersWideProgress reads Host?.Version inside a try/catch.
internal sealed class RecordingCommandRuntime : ICommandRuntime
{
    public bool ShouldProcessResult { get; set; } = true;
    public List<ErrorRecord> Errors { get; } = new();
    public List<string> ShouldProcessTargets { get; } = new();

    public PSHost Host => null!;
    public PSTransactionContext CurrentPSTransaction => null!;

    public bool ShouldProcess(string? target) { ShouldProcessTargets.Add(target ?? ""); return ShouldProcessResult; }
    public bool ShouldProcess(string? target, string? action) { ShouldProcessTargets.Add(target ?? ""); return ShouldProcessResult; }
    public bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption) => ShouldProcessResult;
    public bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption, out ShouldProcessReason shouldProcessReason)
    {
        shouldProcessReason = ShouldProcessReason.None;
        return ShouldProcessResult;
    }

    public bool ShouldContinue(string? query, string? caption) => true;
    public bool ShouldContinue(string? query, string? caption, ref bool yesToAll, ref bool noToAll) => true;
    public bool TransactionAvailable() => false;

    [DoesNotReturn]
    public void ThrowTerminatingError(ErrorRecord errorRecord)
        => throw errorRecord.Exception ?? new InvalidOperationException(errorRecord.ToString());

    public void WriteError(ErrorRecord errorRecord) => Errors.Add(errorRecord);

    public void WriteCommandDetail(string text) { }
    public void WriteDebug(string text) { }
    public void WriteObject(object? sendToPipeline) { }
    public void WriteObject(object? sendToPipeline, bool enumerateCollection) { }
    public void WriteProgress(ProgressRecord progressRecord) { }
    public void WriteProgress(long sourceId, ProgressRecord progressRecord) { }
    public void WriteVerbose(string text) { }
    public void WriteWarning(string text) { }
}
