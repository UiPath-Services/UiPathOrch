using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// The selected test sets, with their coverage setting, labels and test case assignments, into
// another project. The assigned test cases are not copied: each is matched at the destination by
// its key (name and foreign reference, used once on each side; TmCopy.MapTestCasesByKey), so copy
// the test cases before or after (Copy-TmTestCase adds them to the test sets). The stages are
// shared with Copy-Item (see TmCopy).
[Cmdlet(VerbsCommon.Copy, "TmTestSet", SupportsShouldProcess = true)]
public class CopyTmTestSetCmdlet : CopyTmEntityCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestSetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    public string? Destination { get; set; }

    protected override string[]? NamePatterns => Name;
    protected override string? DestinationPath => Destination;

    internal override void Copy(TmCopyContext ctx, List<WildcardPattern>? wpName)
    {
        using (var reporter = new ProgressReporter(this, null, "Test sets")) TmCopy.CopyTestSets(ctx, wpName, reporter);
        if (ctx.TestSets.Count == 0) return;
        TmCopy.MapTestCasesByKey(ctx);
        using (var reporter = new ProgressReporter(this, null, "Assignments")) TmCopy.CopyAssignments(ctx, TmTestSetAssignmentKind.Instance, ctx.TestSets, reporter);
        using (var reporter = new ProgressReporter(this, null, "Labels")) TmCopy.CopyLabels(ctx, reporter);
    }
}
