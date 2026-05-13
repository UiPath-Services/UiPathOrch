using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchAssetLink", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public class RemoveAssetLinkCmdlet : RemoveOrchLinkCmdletBase<Asset>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LinkedAssetNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetLinkFolderCompleter))]
    [SupportsWildcards]
    public override string[]? Link { get; set; }

    protected override string ErrorId => "RemoveAssetLinkError";
    protected override string LinkNoun => "AssetLink";

    protected override ICollection<Asset> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Assets.Get(folder);
    protected override string? GetEntityName(Asset? e) => e?.Name;
    protected override long GetEntityId(Asset e) => e.Id ?? 0;

    protected override void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove)
        => api.ShareAssetsToFolders(srcFolderId, entityIds, toAdd, toRemove);

    protected override void ClearLinkCache(OrchDriveInfo drive, long entityId) => drive.ClearAssetLinkCache(entityId);
    protected override void ClearPerFolderCache(OrchDriveInfo drive, Folder folder) => drive.Assets.ClearCache(folder);
}
