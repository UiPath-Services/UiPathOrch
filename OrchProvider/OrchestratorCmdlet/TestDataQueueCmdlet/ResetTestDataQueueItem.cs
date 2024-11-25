using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Reset, "OrchTestDataQueueItem", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.TestDataQueue))]
    public class ResetTestDataQueueItemCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TestDataQueueNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        public SwitchParameter IsConsumed { get; set; }

        [Parameter]
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
                    var queues = drive.TestDataQueues.Get(folder);
                    foreach (var queue in queues
                        .FilterByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(queue.GetPSPath(), "Rest TestDataQueueItem"))
                        {
                            try
                            {
                                drive.OrchAPISession.SetAllTestDataQueueItemsConsumed(folder.Id ?? 0, queue.Name ?? "", IsConsumed.IsPresent);
                                drive._dicTestDataQueueItems?.TryRemove(folder.Id ?? 0, out _);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "ResetTestDataQueueItemError", ErrorCategory.InvalidOperation, queue));
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
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestDataQueueError", ErrorCategory.InvalidOperation, folder));
                }
            }

            //using var results = OrchThreadPool.RunForEach(drivesFolders,
            //    df => df.folder.GetPSPath(),
            //    df => df.folder,
            //    df => df.drive.GetTestDataQueues(df.folder));

            //using var cancelHandler = new ConsoleCancelHandler();
            //foreach (var result in results)
            //{
            //    try
            //    {
            //        var entities = result.GetResult(cancelHandler.Token);
            //        if (entities == null) continue;
            //        var (drive, folder) = result.Source;

            //        foreach (var testDataQueue in entities)
            //        {
            //            cancelHandler.Token.ThrowIfCancellationRequested();

            //            if (ShouldProcess(testDataQueue.GetPSPath(), "Rest Test Data Queue Item"))
            //            {
            //                try
            //                {
            //                    drive.OrchAPISession.SetAllTestDataQueueItemsConsumed(folder.Id ?? 0, testDataQueue.Name ?? "", IsConsumed.IsPresent);
            //                    drive._dicTestDataQueueItems?.TryRemove(folder.Id.Value, out _);
            //                }
            //                catch (Exception ex)
            //                {
            //                    WriteError(new ErrorRecord(new OrchException(testDataQueue.GetPSPath(), ex), "GetTestDataQueueError", ErrorCategory.InvalidOperation, testDataQueue));
            //                }
            //            }
            //        }
            //    }
            //    catch (OrchException ex)
            //    {
            //        WriteError(new ErrorRecord(ex, "GetTestDataQueueError", ErrorCategory.InvalidOperation, ex.Target));
            //    }
            //}
        }
    }
}
