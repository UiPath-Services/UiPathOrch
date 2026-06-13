using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Update-OrchTestSetSchedule -- wraps PUT /odata/TestSetSchedules({id}).
//
// Parameter surface mirrors New-OrchTestSetSchedule so the CSV emitted
// by Get-OrchTestSetSchedule -ExportCsv round-trips into either cmdlet.
// Uses dirty-detection: no PUT if every passed-in value already matches
// the server-side state. Use -NewName to rename a schedule.
//
// UNVERIFIED — success path not guaranteed. TestSetSchedule create/modify
// is server-gated: every tenant tested rejects it with errorCode 3234
// ("Test set schedule creation and modification is not allowed for this
// tenant"), even on tenants where the Testing feature is enabled and test
// cases / test sets exist (verified yotsuda 2026-05-22). The restriction is
// tenant-level and independent of the Testing toggle. The cmdlet builds the
// correct payload and surfaces the server's response cleanly, but its
// success path has never been observed and is not guaranteed. Intentionally
// left out of the release notes (CHANGELOG); ships exported but quiet.
[Cmdlet(VerbsData.Update, "OrchTestSetSchedule", SupportsShouldProcess = true)]
[OutputType(typeof(TestSetSchedule))]
public class UpdateTestSetScheduleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestScheduleNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestSetNameCompleter))]
    [SupportsWildcards]
    public string? TestSetName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? CronExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Enabled { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeZoneIdCompleter))]
    [SupportsWildcards]
    public string? TimeZoneId { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string? CalendarName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<TestSetSchedule>? schedules = null;
            try
            {
                schedules = drive.TestSetSchedules.Get(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
            }
            if (schedules is null) continue;

            var targetSchedules = schedules.SelectByNames(s => s?.Name, Name).OrderBy(s => s.Name);

            foreach (var schedule in targetSchedules.WithCancellation(cancelHandler.Token))
            {
                string target = schedule.GetPSPath();

                TestSetSchedule postSchedule = OrchCollectionExtensions.DeepCopy(schedule);

                bool dirty = false;
                dirty |= postSchedule.AssignStringIfNotNull(NewName, schedule, s => s.Name, (s, v) => s.Name = v);
                dirty |= postSchedule.AssignStringIfNotNull(Description, schedule, s => s.Description, (s, v) => s.Description = v);
                dirty |= postSchedule.AssignBoolIfNotNull(Enabled, schedule, s => s.Enabled, (s, v) => s.Enabled = v);
                dirty |= postSchedule.AssignStringIfNotNull(CronExpression, schedule, s => s.CronExpression, (s, v) => s.CronExpression = v);
                dirty |= postSchedule.AssignStringIfNotNull(TimeZoneId, schedule, s => s.TimeZoneId, (s, v) => s.TimeZoneId = v);

                // Resolve TestSetName -> TestSetId (only mark dirty when changed).
                postSchedule.AssignIdFromName(
                    TestSetName,
                    () => drive.TestSets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (schedule.TestSetId != v) { s.TestSetId = v; dirty = true; } },
                    this, target, "TestSet");

                // Resolve CalendarName -> CalendarId (only mark dirty when changed).
                postSchedule.AssignIdFromName(
                    CalendarName,
                    () => drive.Calendars.Get(),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (schedule.CalendarId != v) { s.CalendarId = v; dirty = true; } },
                    this, target, "Calendar");

                if (!dirty)
                {
                    continue;
                }

                if (ShouldProcess(target, "Update TestSetSchedule"))
                {
                    try
                    {
                        drive.OrchAPISession.UpdateTestSetSchedule(folder.Id!.Value, postSchedule);
                        drive.TestSetSchedules.ClearCache(folder);
                        // PUT does not return a body; re-fetch so the cmdlet
                        // emits a fresh entity reflecting the post-update state.
                        var updated = drive.TestSetSchedules.Get(folder).FirstOrDefault(s => s.Id == postSchedule.Id);
                        if (updated is not null)
                        {
                            updated.Path = folder.GetPSPath();
                            WriteObject(updated);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
