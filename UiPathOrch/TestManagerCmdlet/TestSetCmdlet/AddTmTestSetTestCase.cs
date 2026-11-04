using System.Management.Automation;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

// Assigns test cases to test sets; see TmTestCaseAssignment.cs.
[Cmdlet(VerbsCommon.Add, "TmTestSetTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class AddTmTestSetTestCaseCmdlet : ChangeTmTestCaseAssignmentCmdletBase
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
    protected override bool Adding => true;
}
