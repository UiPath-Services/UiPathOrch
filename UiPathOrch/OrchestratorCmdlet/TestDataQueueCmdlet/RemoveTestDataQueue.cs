using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestDataQueue", SupportsShouldProcess = true)]
public class RemoveTestDataQueueCommand : RemoveFolderEntityCmdletBase<TestDataQueue>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestDataQueueNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "TestDataQueue";
    protected override Func<TestDataQueue?, string?> GetName => q => q?.Name;
    protected override Func<TestDataQueue, string> GetPSPath => q => q.GetPSPath();
    protected override bool ExcludePersonalWorkspace => true;

    protected override IEnumerable<TestDataQueue> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestDataQueues.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, TestDataQueue queue)
    {
        drive.OrchAPISession.RemoveTestDataQueue(folder.Id ?? 0, queue.Id ?? 0);
        drive.TestDataQueues.ClearCache(folder);
    }
}
