using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchBucketLink")]
[OutputType(typeof(EntityLink))]
public class GetBucketLinkCmdlet : GetOrchLinkCmdletBase<Bucket>
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LinkedBucketNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string ErrorId => "GetBucketLinkError";
    protected override string DefaultCsvName => "ExportedBucketLinks.csv";

    protected override ICollection<Bucket> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Buckets.Get(folder);
    protected override string? GetEntityName(Bucket? e) => e?.Name;
    protected override long GetEntityId(Bucket e) => e.Id ?? 0;

    protected override AccessibleFoldersDto? GetFoldersForEntity(OrchDriveInfo drive, Folder srcFolder, Bucket entity)
        => drive.GetFoldersForBucket(srcFolder, entity);

    protected override EntityLink BuildLink(string srcPath, Bucket entity, string linkFolderPath, long srcFolderId, long linkFolderId)
        => new()
        {
            Path = srcPath,
            Name = entity.Name,
            Link = linkFolderPath,
            Id = entity.Id ?? 0,
            FolderId = srcFolderId,
            LinkFolderId = linkFolderId,
        };
}
