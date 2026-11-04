using System.Management.Automation;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

// Assigns test cases to requirements; see TmTestCaseAssignment.cs.
[Cmdlet(VerbsCommon.Add, "TmRequirementTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class AddTmRequirementTestCaseCmdlet : ChangeTmTestCaseAssignmentCmdletBase
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
    protected override bool Adding => true;
}
