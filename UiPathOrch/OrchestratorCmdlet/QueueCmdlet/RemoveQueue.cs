using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchQueue", SupportsShouldProcess = true)]
public class RemoveQueueCmdlet : RemoveFolderEntityCmdletBase<QueueDefinition>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "Queue";
    protected override Func<QueueDefinition?, string?> GetName => q => q?.Name;
    protected override Func<QueueDefinition, string> GetPSPath => q => q.GetPSPath();

    protected override IEnumerable<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Queues.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, QueueDefinition queue)
    {
        drive.OrchAPISession.RemoveQueue(folder.Id ?? 0, queue.Id ?? 0);
        drive.Queues.ClearCache(folder);
    }
}
