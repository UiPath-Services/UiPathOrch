using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// The selected test cases, with their steps, pre-/post-conditions and labels, into another
// project, and into the destination's counterparts (same key) of the test sets and requirements
// they belong to in the source. The stages are shared with Copy-Item (see TmCopy).
[Cmdlet(VerbsCommon.Copy, "TmTestCase", SupportsShouldProcess = true)]
public class CopyTmTestCaseCmdlet : CopyTmEntityCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    public string? Destination { get; set; }

    protected override string[]? NamePatterns => Name;
    protected override string? DestinationPath => Destination;

    internal override void Copy(TmCopyContext ctx, List<WildcardPattern>? wpName)
    {
        using (var reporter = new ProgressReporter(this, null, "Test cases")) TmCopy.CopyTestCases(ctx, wpName, reporter);
        using (var reporter = new ProgressReporter(this, null, "Test steps")) TmCopy.CopyTestSteps(ctx, reporter);

        // The copied test cases join the destination's counterparts (same key, used once on each
        // side) of the test sets and requirements they belong to in the source, so this cmdlet and
        // Copy-TmTestSet / Copy-TmRequirement give the same result whichever runs first.
        if (ctx.TestCases.Count > 0)
        {
            using var reporter = new ProgressReporter(this, null, "Assignments");
            TmCopy.CopyAssignments(ctx, TmTestSetAssignmentKind.Instance, TmCopy.MapContainersByKey(ctx, TmTestSetAssignmentKind.Instance), reporter, copiedTestCasesOnly: true);
            TmCopy.CopyAssignments(ctx, TmRequirementAssignmentKind.Instance, TmCopy.MapContainersByKey(ctx, TmRequirementAssignmentKind.Instance), reporter, copiedTestCasesOnly: true);
        }

        using (var reporter = new ProgressReporter(this, null, "Labels")) TmCopy.CopyLabels(ctx, reporter);
    }
}
