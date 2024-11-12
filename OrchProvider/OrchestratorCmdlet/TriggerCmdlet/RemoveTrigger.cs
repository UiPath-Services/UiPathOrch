using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchTrigger", SupportsShouldProcess = true)]
    public class RemoveTriggerCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TriggerNameCompleter<Positional.Name>))]
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
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                try
                {
                    var triggers = drive.GetTriggers(folder);

                    foreach (var trigger in triggers
                        .FilterByWildcards(t => t?.Name, wpName)
                        .OrderBy(t => t.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(trigger.GetPSPath(), "Remove Trigger"))
                        {
                            try
                            {
                                drive.OrchAPISession.DeleteProcessSchedule(folder.Id ?? 0, trigger.Id ?? 0);
                                drive._dicTriggers?.TryRemove(folder.Id ?? 0, out _);
                                drive._dicTriggers_Exceptions.ClearCache();
                                drive._dicTriggersDetailed?.TryRemove(folder.Id ?? 0, out _);
                                drive._dicTriggersDetailed_Exceptions.ClearCache();
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "RemoveTriggerError", ErrorCategory.InvalidOperation, trigger));
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
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, folder));
                    continue;
                }
            }
        }

        // マルチスレッド化したバージョン
        // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
        //protected override void ProcessRecord()
        //{
        //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        //    var wpName = Name.ConvertToWildcardPatternList();

        //    using var results = OrchThreadPool.RunForEach(drivesFolders,
        //        df => df.folder.GetPSPath(),
        //        df => df.folder,
        //        df => df.drive.GetProcessSchedule(df.folder)
        //    );

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var trigger in entities
        //                .FilterByWildcards(t => t.Name!, wpName)
        //                .OrderBy(t => t.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(trigger.GetPSPath(), "Remove Trigger"))
        //                {
        //                    try
        //                    {
        //                        drive.OrchAPISession.RemoveProcessSchedule(folder.Id ?? 0, trigger.Id ?? 0);
        //                        drive._dicProcessSchedule?.TryRemove(folder.Id ?? 0, out List<ProcessSchedule>? _);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "RemoveTriggerError", ErrorCategory.InvalidOperation, trigger));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetTriggerError", ErrorCategory.InvalidOperation, ex.Target));
        //            continue;
        //        }
        //    }
        //}
    }
}
