using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchQueueLink", SupportsShouldProcess = true)]
public class AddQueueLinkCmdlet : AddOrchLinkCmdletBase<QueueDefinition>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string ErrorId => "AddQueueLinkError";
    protected override string LinkNoun => "QueueLink";

    protected override ICollection<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Queues.Get(folder);
    protected override string? GetEntityName(QueueDefinition? e) => e?.Name;
    protected override long GetEntityId(QueueDefinition e) => e.Id ?? 0;

    protected override void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove)
        => api.ShareQueuesToFolders(srcFolderId, entityIds, toAdd, toRemove);

    protected override void ClearLinkCache(OrchDriveInfo drive, long entityId) => drive.ClearQueueLinkCache(entityId);
    protected override void ClearPerFolderCache(OrchDriveInfo drive, Folder folder) => drive.Queues.ClearCache(folder);
}
