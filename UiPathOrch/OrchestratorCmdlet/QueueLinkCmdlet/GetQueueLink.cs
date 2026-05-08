using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchQueueLink")]
[OutputType(typeof(QueueLink))]
public class GetQueueLinkCommand : GetOrchLinkCmdletBase<QueueDefinition, QueueLink>
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LinkedQueueNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string ErrorId => "GetQueueLinkError";

    protected override ICollection<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Queues.Get(folder);
    protected override string? GetEntityName(QueueDefinition? e) => e?.Name;
    protected override long GetEntityId(QueueDefinition e) => e.Id ?? 0;

    protected override AccessibleFoldersDto? GetFoldersForEntity(OrchDriveInfo drive, Folder srcFolder, QueueDefinition entity)
        => drive.GetFoldersForQueue(srcFolder, entity);

    protected override QueueLink BuildLink(string srcPath, QueueDefinition entity, string linkFolderPath, long srcFolderId, long linkFolderId)
        => new()
        {
            Path = srcPath,
            Name = entity.Name,
            Link = linkFolderPath,
            QueueId = entity.Id ?? 0,
            FolderId = srcFolderId,
            LinkFolderId = linkFolderId,
        };
}
