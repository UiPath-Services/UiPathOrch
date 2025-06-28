using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchApiTrigger", SupportsShouldProcess = true)]
public class RemoveApiTriggerCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter<TPositional>))]
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
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var triggers = drive.ApiTriggers.Get(folder);

                foreach (var trigger in triggers
                    .FilterByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(trigger.GetPSPath(), "Remove API Trigger"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveHttpTrigger(folder.Id ?? 0, trigger.Id!);
                            drive.ApiTriggers.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "RemoveApiTriggerError", ErrorCategory.NotSpecified, trigger));
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
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "RemoveApiTriggerError", ErrorCategory.NotSpecified, folder));
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
    //        df => df.drive.GetHttpTriggers(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var triggers = result.GetResult(cancelHandler.Token);
    //            if (triggers is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var trigger in triggers
    //                .FilterByWildcards(t => t.Name!, wpName)
    //                .OrderBy(t => t.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(trigger.GetPSPath(), "Remove API Trigger"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveHttpTrigger(folder.Id ?? 0, trigger.Id!);
    //                        drive._dicHttpTriggers?.TryRemove(folder.Id ?? 0, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "RemoveApiTriggerError", ErrorCategory.NotSpecified, trigger));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "RemoveApiTriggerError", ErrorCategory.NotSpecified, ex.Target));
    //        }
    //    }
    //}
}
