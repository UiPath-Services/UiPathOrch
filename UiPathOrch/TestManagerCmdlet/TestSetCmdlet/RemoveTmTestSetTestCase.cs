using System.Management.Automation;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

// Unassigns test cases from test sets (the test cases themselves stay); see TmTestCaseAssignment.cs.
[Cmdlet(VerbsCommon.Remove, "TmTestSetTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmTestSetTestCaseCmdlet : ChangeTmTestCaseAssignmentCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestSetNameCompleter))]
    [SupportsWildcards]
    public string[]? TestSet { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    internal override TmTestCaseAssignmentKind Kind => TmTestSetAssignmentKind.Instance;
    protected override string[]? ContainerPatterns => TestSet;
    protected override string[]? TestCasePatterns => TestCase;
    protected override bool Adding => false;
}
