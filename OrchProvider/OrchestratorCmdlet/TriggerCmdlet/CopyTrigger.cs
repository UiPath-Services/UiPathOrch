using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Security.Cryptography;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchTrigger", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.ProcessSchedule))]
    public class CopyTriggerCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TriggerNameCompleter<Positional.Name_Destination>))]
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

            string msg = "Copying triggers...";
            using var reporterTriggers = new ProgressReporter(this, 800, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                    //srcDrive._dicTriggers?.TryRemove(srcFolder.Id ?? 0, out _);
                    var srcEntities = srcDrive.GetTriggers(srcFolder).FilterByWildcards(e => e?.Name, wpName);
                    if (!srcEntities.Any()) continue;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, srcFolder));
                    continue;
                }

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null || (srcDrive == dstDrive && srcFolder == dstFolder)) continue;

                // キャッシュをクリアしておく
                dstDrive._dicMachinesAssigned?.TryRemove(dstFolder.Id ?? 0, out var _);

                try
                {
                    Core.OrchProvider.CopyTriggers(this,
                        srcDrive, srcFolder, wpName!,
                        dstDrive, dstFolder, reporterTriggers,
                        cancelHandler.Token, false);
                    dstDrive._dicTriggers?.TryRemove(dstFolder.Id ?? 0, out _);
                    dstDrive._dicProcessSchedules_Exceptions.ClearCache();
                    dstDrive._dicProcessScheduleDetailed?.TryRemove(dstFolder.Id ?? 0, out _);
                    dstDrive._dicProcessScheduleDetailed_Exceptions.ClearCache();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = dstFolder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyQueueError", ErrorCategory.InvalidOperation, dstFolder));
                }
            }
        }
    }
}
