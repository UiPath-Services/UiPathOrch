using System.Management.Automation;
using System.Reflection.Metadata;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchAsset", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Asset))]
public class CopyAssetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(AssetValueTypeCompleter<Positional>))]
    //[SupportsWildcards]
    //public string[]? ValueType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
        var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

        // コピー元とコピー先が同じなら、何もしない
        if (srcRootFolder == dstRootFolder) return;

        var wpName = Name.ConvertToWildcardPatternList();

        // キャッシュは暗黙にクリアしない方が良いか。。
        //srcDrive._dicExtendedMachines = null;
        //srcDrive._dicAssetLinks = null;
        //srcDrive._dicCredentialStores = null;
        //dstDrive._dicCredentialStores = null;
        //srcDrive._dicUsers = null; // TODO: AD ユーザーに対応する必要があるのではないか？
        //dstDrive._dicUsers = null;
        //srcDrive._dicExtendedMachines = null; // dstDrive のキャッシュはクリア不要。フォルダマシンを取得するため。

        using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, "Copying assets...");
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                //srcDrive._dicAssets?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.Assets.Get(srcFolder).FilterByWildcards(e => e?.Name, wpName);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetAssetError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                Core.OrchProvider.CopyAssets(this,
                    srcDrive, srcFolder, wpName,
                    dstDrive, dstFolder, reporterAssets,
                    false, cancelHandler.Token);
                dstDrive.Assets.ClearCache(dstFolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyAssetError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
