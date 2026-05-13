using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchQueueLink", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public class RemoveQueueLinkCmdlet : RemoveOrchLinkCmdletBase<QueueDefinition>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LinkedQueueNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueLinkFolderCompleter))]
    [SupportsWildcards]
    public override string[]? Link { get; set; }

    protected override string ErrorId => "RemoveQueueLinkError";
    protected override string LinkNoun => "QueueLink";

    protected override ICollection<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Queues.Get(folder);
    protected override string? GetEntityName(QueueDefinition? e) => e?.Name;
    protected override long GetEntityId(QueueDefinition e) => e.Id ?? 0;

    protected override void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove)
        => api.ShareQueuesToFolders(srcFolderId, entityIds, toAdd, toRemove);

    protected override void ClearLinkCache(OrchDriveInfo drive, long entityId) => drive.ClearQueueLinkCache(entityId);
    protected override void ClearPerFolderCache(OrchDriveInfo drive, Folder folder) => drive.Queues.ClearCache(folder);
}
