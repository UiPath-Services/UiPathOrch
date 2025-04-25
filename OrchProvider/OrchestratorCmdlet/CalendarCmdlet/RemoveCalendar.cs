using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCalendar", SupportsShouldProcess = true)]
[OutputType(typeof(ExtendedCalendar))]
public class RemoveCalendarCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var calendars = drive.GetCalendars();

                foreach (var calendar in calendars?.FilterByWildcards(c => c?.Name, wpName) ?? [])
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = calendar.GetPSPath();
                    if (ShouldProcess(target, "Remove Calendar"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveCalendar(calendar.Id ?? 0);
                            drive._dicCalendars = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveCalendarError", ErrorCategory.InvalidOperation, target));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    // cap はドライブ毎にするから、マルチスレッドのままでも良い気もするが。。
    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drives,
    //        drive => drive.NameColonSeparator,
    //        drive => drive,
    //        drive => drive.GetCalendars());

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var drive = result.Source;

    //            foreach (var calendar in entities.FilterByWildcards(c => c.Name!, wpName))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                string target = calendar.GetPSPath();
    //                if (ShouldProcess(target, "Remove Calendar"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveCalendar(calendar.Id ?? 0);
    //                        drive._dicCalendars = null;
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveCalendarError", ErrorCategory.InvalidOperation, target));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetCalendarError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
