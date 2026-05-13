using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestCase", SupportsShouldProcess = true)]
public class RemoveTestCaseCmdlet : RemoveFolderEntityCmdletBase<TestCaseDefinition>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestCaseNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "TestCase";
    protected override Func<TestCaseDefinition?, string?> GetName => t => t?.Name;
    protected override Func<TestCaseDefinition, string> GetPSPath => t => t.GetPSPath();
    protected override bool ExcludePersonalWorkspace => true;

    protected override IEnumerable<TestCaseDefinition> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestCases.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, TestCaseDefinition testCase)
    {
        drive.OrchAPISession.RemoveTestCases(folder.Id ?? 0, [testCase.Id ?? 0]);
        drive.TestCases.ClearCache(folder);
    }
}
