using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchCalendar", SupportsShouldProcess = true)]
public class CopyCalendarCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string? Path { get; set; }

    internal static void CopyCalendars(
        IWritableHost _this,
        OrchDriveInfo srcDrive,
        List<WildcardPattern>? wpName,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
        srcDrive.Calendars.ClearCache();
        srcDrive.CalendarsDetailed.ClearCache();

        // This implementation is fine as is.
        ICollection<ExtendedCalendar>? srcCalendars;
        try
        {
            srcCalendars = srcDrive.GetCalendars();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        if (srcCalendars is null) return;

        using var reporter = new ProgressReporter(_this, 1, 100, "Copying calendars");

        int index = 0;
        reporter.TotalNum = dstDrives.Count * srcCalendars.Count;

        foreach (var dstDrive in dstDrives)
        {
            foreach (var srcCalendar in srcCalendars
                .FilterByWildcards(c => c?.Name, wpName)
                .OrderBy(c => c.Name))
            {
                string item = srcCalendar.GetPSPath();
                string destination = dstDrive.NameColonSeparator;

                cancelToken.ThrowIfCancellationRequested();

                reporter.WriteProgress(++index, $"{srcCalendar.GetPSPath()} to {dstDrive.NameColonSeparator}");

                if (shouldProcess || _this.ShouldProcess($"Item: {item} Destination: {destination}", "Copy Calendar"))
                {
                    try
                    {
                        var srcDetailedCalendar = srcDrive.GetCalendar(srcCalendar);
                        if (srcDetailedCalendar is null)
                        {
                            continue;
                        }
                        var newCalendar = OrchCollectionExtensions.DeepCopy(srcDetailedCalendar);
                        newCalendar.TimeZoneId = null;
                        newCalendar.Key = null;
                        newCalendar.Id = null;
                        //newCalendar.Path = null; // Not needed since it has the JsonIgnore attribute
                        var createdCalendar = dstDrive.OrchAPISession.PostCalendar(newCalendar);
                        if (createdCalendar is not null)
                        {
                            //createdCalendar.Path = dstDrive.NameColonSeparator;
                            //WriteObject(createdCalendar);
                            dstDrive.Calendars.ClearCache();
                            dstDrive.CalendarsDetailed.ClearCache();
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(item, ex), "CreateCalendarError", ErrorCategory.InvalidOperation, destination));
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetOrchDrive(Path!);
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        CopyCalendars(this, srcDrive, wpName, dstDrives, false, cancelHandler.Token);
    }
}
