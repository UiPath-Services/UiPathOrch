using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchTestSetSchedule", SupportsShouldProcess = true)]
    public class RemoveTestSetScheduleCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TestScheduleNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                try
                {
                    var schedules = drive.TestSetSchedules.Get(folder);

                    foreach (var schedule in schedules
                        .FilterByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(schedule.GetPSPath(), "Remove TestSetSchedule"))
                        {
                            try
                            {
                                drive.OrchAPISession.RemoveTestSetSchedules(folder.Id ?? 0, schedule.Id ?? 0);
                                drive.TestSetSchedules.ClearCache(folder);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(schedule.GetPSPath(), ex), "RemoveTestSetScheduleError", ErrorCategory.InvalidOperation, schedule));
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
                }
            }
        }

        // マルチスレッド化したバージョン
        // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
        //protected override void ProcessRecord()
        //{
        //    var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        //    var wpName = Name.ConvertToWildcardPatternList();

        //    using var results = OrchThreadPool.RunForEach(drivesFolders,
        //        df => df.folder.GetPSPath(),
        //        df => df.folder,
        //        df => df.drive.GetTestSetSchedules(df.folder));

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var testSetSchedule in entities
        //                .FilterByWildcards(e => e.Name!, wpName)
        //                .OrderBy(e => e.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(testSetSchedule.GetPSPath(), "Remove Test Set Schedule"))
        //                {
        //                    try
        //                    {
        //                        drive.OrchAPISession.RemoveTestSetSchedules(folder.Id!.Value, testSetSchedule.Id!.Value);
        //                        drive._dicTestSetSchedules?.TryRemove(folder.Id.Value, out _);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(testSetSchedule.GetPSPath(), ex), "RemoveTestSetScheduleError", ErrorCategory.InvalidOperation, testSetSchedule));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetTestSetScheduleError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
