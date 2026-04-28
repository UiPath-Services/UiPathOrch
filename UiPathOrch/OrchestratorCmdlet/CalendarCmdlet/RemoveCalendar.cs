using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCalendar", SupportsShouldProcess = true)]
public class RemoveCalendarCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
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
                WriteError(new ErrorRecord(ex, "GetCalendarError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
