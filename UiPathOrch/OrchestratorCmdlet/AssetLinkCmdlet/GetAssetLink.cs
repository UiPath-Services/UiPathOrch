using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAssetLink")]
[OutputType(typeof(AssetLink))]
public class GetAssetLinkCmdlet : GetOrchLinkCmdletBase<Asset, AssetLink>
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LinkedAssetNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string ErrorId => "GetAssetLinkError";

    protected override ICollection<Asset> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Assets.Get(folder);
    protected override string? GetEntityName(Asset? e) => e?.Name;
    protected override long GetEntityId(Asset e) => e.Id ?? 0;

    protected override AccessibleFoldersDto? GetFoldersForEntity(OrchDriveInfo drive, Folder srcFolder, Asset entity)
        => drive.GetFoldersForAsset(srcFolder, entity);

    protected override AssetLink BuildLink(string srcPath, Asset entity, string linkFolderPath, long srcFolderId, long linkFolderId)
        => new()
        {
            Path = srcPath,
            Name = entity.Name,
            Link = linkFolderPath,
            AssetId = entity.Id ?? 0,
            FolderId = srcFolderId,
            LinkFolderId = linkFolderId,
        };
}
