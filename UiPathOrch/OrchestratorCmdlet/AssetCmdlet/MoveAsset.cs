using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Move, "OrchAsset", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public class MoveAssetCmdlet : MoveOrchEntityCmdletBase<Asset>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string ErrorId => "MoveAssetError";
    protected override string EntityNoun => "Asset";

    protected override ICollection<Asset> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Assets.Get(folder);
    protected override string? GetEntityName(Asset? e) => e?.Name;
    protected override long GetEntityId(Asset e) => e.Id ?? 0;

    protected override void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove)
        => api.ShareAssetsToFolders(srcFolderId, entityIds, toAdd, toRemove);

    protected override void ClearLinkCache(OrchDriveInfo drive, long entityId) => drive.ClearAssetLinkCache(entityId);
    protected override void ClearPerFolderCache(OrchDriveInfo drive, Folder folder) => drive.Assets.ClearCache(folder);
}
