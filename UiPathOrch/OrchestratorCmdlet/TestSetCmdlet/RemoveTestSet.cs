using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestSet", SupportsShouldProcess = true)]
public class RemoveTestSetCommand : RemoveFolderEntityCmdletBase<TestSet>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestSetNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "TestSet";
    protected override Func<TestSet?, string?> GetName => t => t?.Name;
    protected override Func<TestSet, string> GetPSPath => t => t.GetPSPath();
    protected override bool ExcludePersonalWorkspace => true;

    protected override IEnumerable<TestSet> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestSets.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, TestSet testSet)
    {
        drive.OrchAPISession.RemoveTestSet(folder.Id ?? 0, testSet.Id ?? 0);
        drive.TestSets.ClearCache(folder);
    }
}
