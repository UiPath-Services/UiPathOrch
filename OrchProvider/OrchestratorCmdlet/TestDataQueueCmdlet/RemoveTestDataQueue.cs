using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestDataQueue", SupportsShouldProcess = true)]
public class RemoveTestDataQueueCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestDataQueueNameCompleter<TPositional>))]
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
                var queues = drive.TestDataQueues.Get(folder);
                foreach (var queue in queues
                        .FilterByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(queue.GetPSPath(), "Remove TestDataQueue"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTestDataQueue(folder.Id ?? 0, queue.Id ?? 0);
                            drive.TestDataQueues.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "RemoveTestDataQueueError", ErrorCategory.InvalidOperation, queue));
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
    //        df => df.drive.GetTestDataQueues(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;
    //            var (drive, folder) = result.Source;

    //            foreach (var entity in entities
    //                .FilterByWildcards(e => e.Name!, wpName)
    //                .OrderBy(e => e.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(entity.GetPSPath(), "Remove TestDataQueue"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveTestDataQueue(folder.Id ?? 0, entity.Id ?? 0);
    //                        drive._dicTestDataQueues?.TryRemove(folder.Id ?? 0, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(entity.GetPSPath(), ex), "RemoveTestDataQueueError", ErrorCategory.InvalidOperation, entity));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "RemoveTestDataQueueError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
