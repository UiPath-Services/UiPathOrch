using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Move, "OrchBucket", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public class MoveBucketCmdlet : MoveOrchEntityCmdletBase<Bucket>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string ErrorId => "MoveBucketError";
    protected override string EntityNoun => "Bucket";

    protected override ICollection<Bucket> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Buckets.Get(folder);
    protected override string? GetEntityName(Bucket? e) => e?.Name;
    protected override long GetEntityId(Bucket e) => e.Id ?? 0;

    protected override void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove)
        => api.ShareBucketsToFolders(srcFolderId, entityIds, toAdd, toRemove);

    protected override void ClearLinkCache(OrchDriveInfo drive, long entityId) => drive.ClearBucketLinkCache(entityId);
    protected override void ClearPerFolderCache(OrchDriveInfo drive, Folder folder) => drive.Buckets.ClearCache(folder);
}
