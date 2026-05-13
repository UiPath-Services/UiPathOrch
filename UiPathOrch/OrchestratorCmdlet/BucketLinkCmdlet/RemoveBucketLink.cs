using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchBucketLink", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public class RemoveBucketLinkCmdlet : RemoveOrchLinkCmdletBase<Bucket>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LinkedBucketNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketLinkFolderCompleter))]
    [SupportsWildcards]
    public override string[]? Link { get; set; }

    protected override string ErrorId => "RemoveBucketLinkError";
    protected override string LinkNoun => "BucketLink";

    protected override ICollection<Bucket> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Buckets.Get(folder);
    protected override string? GetEntityName(Bucket? e) => e?.Name;
    protected override long GetEntityId(Bucket e) => e.Id ?? 0;

    protected override void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove)
        => api.ShareBucketsToFolders(srcFolderId, entityIds, toAdd, toRemove);

    protected override void ClearLinkCache(OrchDriveInfo drive, long entityId) => drive.ClearBucketLinkCache(entityId);
    protected override void ClearPerFolderCache(OrchDriveInfo drive, Folder folder) => drive.Buckets.ClearCache(folder);
}
