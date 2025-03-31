using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchApiTrigger", SupportsShouldProcess = true)]
//[OutputType(typeof(Entities.ProcessSchedule))]
public class CopyApiTriggerCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

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
        if (srcDrive == dstDrive && srcRootFolder == dstRootFolder) return;

        var wpName = Name.ConvertToWildcardPatternList();

        string msg = "Copying API triggers...";
        using var reporterApiTriggers = new ProgressReporter(this, 900, Int32.MaxValue, msg, msg);
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                //srcDrive._dicHttpTriggers?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.ApiTriggers.Get(srcFolder).FilterByWildcards(e => e?.Name, wpName);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetApiTriggerError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || (srcDrive == dstDrive && srcFolder == dstFolder)) continue;

            //srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);
            //dstDrive.Robots.ClearCache();
            //dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

            try
            {
                Core.OrchProvider.CopyApiTriggers(this,
                    srcDrive, srcFolder, wpName!,
                    dstDrive, dstFolder, reporterApiTriggers,
                    false, cancelHandler.Token);
                dstDrive.ApiTriggers.ClearCache(dstFolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyApiTriggerError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
