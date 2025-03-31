using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchTestSetSchedule", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.TestSetSchedule))]
public class CopyTestSetScheduleCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestScheduleNameCompleter<TPositional>))]
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

        string msg = "Copying test schedules...";
        using var reporterTestSchedules = new ProgressReporter(this, 1200, Int32.MaxValue, msg, msg);
        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                //srcDrive._dicTestSetSchedules?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.TestSetSchedules.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || (srcDrive == dstDrive && srcFolder == dstFolder)) continue;

            try
            {
                Core.OrchProvider.CopyTestSetSchedules(this,
                    srcDrive, srcFolder, wpName,
                    dstDrive, dstFolder, reporterTestSchedules,
                    false, cancelHandler.Token);
                dstDrive.TestSetSchedules.ClearCache(dstFolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyTestScheduleError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
