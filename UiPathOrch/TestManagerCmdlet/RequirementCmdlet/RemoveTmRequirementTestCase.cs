using System.Management.Automation;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

// Unassigns test cases from requirements (the test cases themselves stay); see TmTestCaseAssignment.cs.
[Cmdlet(VerbsCommon.Remove, "TmRequirementTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmRequirementTestCaseCmdlet : ChangeTmTestCaseAssignmentCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmRequirementNameCompleter))]
    [SupportsWildcards]
    public string[]? Requirement { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    internal override TmTestCaseAssignmentKind Kind => TmRequirementAssignmentKind.Instance;
    protected override string[]? ContainerPatterns => Requirement;
    protected override string[]? TestCasePatterns => TestCase;
    protected override bool Adding => false;
}
