using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCalendar", SupportsShouldProcess = true)]
public class RemoveCalendarCmdlet : RemoveDriveEntityCmdletBase<ExtendedCalendar>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "Calendar";
    protected override Func<ExtendedCalendar?, string?> GetName => c => c?.Name;
    protected override Func<ExtendedCalendar, string> GetPSPath => c => c.GetPSPath();

    protected override IEnumerable<ExtendedCalendar> GetEntities(OrchDriveInfo drive)
        => drive.GetCalendars() ?? [];

    protected override void Remove(OrchDriveInfo drive, ExtendedCalendar calendar)
    {
        drive.OrchAPISession.RemoveCalendar(calendar.Id ?? 0);
        drive.Calendars.ClearCache();
        drive.CalendarsDetailed.ClearCache();
    }
}
