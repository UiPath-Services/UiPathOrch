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
        var wpName = Name.ConvertToWildcardPatternList();

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

            var targetSchedules = schedules.SelectByWildcards(s => s?.Name, wpName).OrderBy(s => s.Name);

            foreach (var schedule in targetSchedules
                .WithProgressBar(this, $"Updating test schedules in {folder.GetPSPath()}", s => s.Name)
                .WithCancellation(cancelHandler.Token))
            {
                string target = schedule.GetPSPath();

                TestSetSchedule postSchedule = OrchCollectionExtensions.DeepCopy(schedule);

                // TestSet/Calendar names resolve to ids here (they need the live lists); the
                // write/no-write decision is the pure, unit-tested ComputeTestSetScheduleUpdate.
                // AssignIdFromName's return is intentionally ignored (a bad name never aborts the
                // record) but it still WriteErrors on a 0/multi-match name as before.
                long? resolvedTestSetId = null;
                _ = postSchedule.AssignIdFromName(
                    TestSetName, () => drive.TestSets.Get(folder), e => e.Name!, e => e.Id!,
                    (_, v) => resolvedTestSetId = v, this, target, "TestSet");

                long? resolvedCalendarId = null;
                _ = postSchedule.AssignIdFromName(
                    CalendarName, () => drive.Calendars.Get(), e => e.Name!, e => e.Id!,
                    (_, v) => resolvedCalendarId = v, this, target, "Calendar");

                bool dirty = ComputeTestSetScheduleUpdate(postSchedule, schedule, new TestSetScheduleUpdateInputs
                {
                    NewName = NewName,
                    Description = Description,
                    Enabled = Enabled,
                    CronExpression = CronExpression,
                    TimeZoneId = TimeZoneId,
                    TestSetCleared = TestSetName == "",
                    ResolvedTestSetId = resolvedTestSetId,
                    CalendarCleared = CalendarName == "",
                    ResolvedCalendarId = resolvedCalendarId,
                });

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

    internal sealed class TestSetScheduleUpdateInputs
    {
        public string? NewName { get; init; }
        public string? Description { get; init; }
        public string? Enabled { get; init; }
        public string? CronExpression { get; init; }
        public string? TimeZoneId { get; init; }
        // TestSet/Calendar: names are resolved to ids by the cmdlet (they need live lists) and
        // passed in. Cleared = the name was "" (an explicit clear); ResolvedId = the single-match
        // id (null when unspecified, cleared, or unresolvable).
        public bool TestSetCleared { get; init; }
        public long? ResolvedTestSetId { get; init; }
        public bool CalendarCleared { get; init; }
        public long? ResolvedCalendarId { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> (a deep copy of the current
    /// schedule) and returns whether anything differs from <paramref name="source"/>, so the caller
    /// can skip the PUT on a no-op. The scalar fields are diffed; TestSetId/CalendarId follow
    /// AssignIdFromName + a set-only-when-changed setter (an empty-string clear writes null when a
    /// value was set; a resolved id writes when different; anything else leaves the current id). No
    /// API access — unit-testable in isolation.
    /// </summary>
    internal static bool ComputeTestSetScheduleUpdate(TestSetSchedule payload, TestSetSchedule source, TestSetScheduleUpdateInputs input)
    {
        bool dirty = false;
        dirty |= payload.AssignStringIfNotNull(input.NewName, source, s => s.Name, (s, v) => s.Name = v);
        dirty |= payload.AssignStringIfNotNull(input.Description, source, s => s.Description, (s, v) => s.Description = v);
        dirty |= payload.AssignBoolIfNotNull(input.Enabled, source, s => s.Enabled, (s, v) => s.Enabled = v);
        dirty |= payload.AssignStringIfNotNull(input.CronExpression, source, s => s.CronExpression, (s, v) => s.CronExpression = v);
        dirty |= payload.AssignStringIfNotNull(input.TimeZoneId, source, s => s.TimeZoneId, (s, v) => s.TimeZoneId = v);

        if (ResolveIdChange(input.TestSetCleared, input.ResolvedTestSetId, source.TestSetId, out long? newTestSetId))
        {
            payload.TestSetId = newTestSetId;
            dirty = true;
        }
        if (ResolveIdChange(input.CalendarCleared, input.ResolvedCalendarId, source.CalendarId, out long? newCalendarId))
        {
            payload.CalendarId = newCalendarId;
            dirty = true;
        }
        return dirty;
    }

    // True (with newId) when the id should change: an empty-string clear writes null if a value was
    // set; a resolved id writes when it differs; unspecified / unresolvable leaves the current id.
    private static bool ResolveIdChange(bool cleared, long? resolvedId, long? currentId, out long? newId)
    {
        newId = currentId;
        if (cleared)
        {
            if (currentId is not null) { newId = null; return true; }
            return false;
        }
        if (resolvedId is not null && resolvedId != currentId) { newId = resolvedId; return true; }
        return false;
    }
}
