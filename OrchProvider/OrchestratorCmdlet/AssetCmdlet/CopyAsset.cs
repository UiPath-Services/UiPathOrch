using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchAsset", SupportsShouldProcess = true)]
public class CopyAssetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(AssetValueTypeCompleter))]
    //[SupportsWildcards]
    //public string[]? ValueType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? UserMappingCsv { get; set; }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(Path);
        var srcDrivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);

        // If the source and destination are the same, do nothing
        if (srcRootFolder == dstRootFolder) return;

        var userMapping = SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrive, UserMappingCsv);

        var wpName = Name.ConvertToWildcardPatternList();

        // It may be better not to clear the cache implicitly..
        //srcDrive._dicExtendedMachines = null;
        //srcDrive._dicAssetLinks = null;
        //srcDrive._dicCredentialStores = null;
        //dstDrive._dicCredentialStores = null;
        //srcDrive._dicUsers = null; // TODO: Do we need to handle AD users?
        //dstDrive._dicUsers = null;
        //srcDrive._dicExtendedMachines = null; // No need to clear dstDrive's cache, since we need to get folder machines.

        using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, "Copying assets...");
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // If there are no entities to copy, there is no need to look up the dstFolder
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
                    false, cancelHandler.Token, userMapping);
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
