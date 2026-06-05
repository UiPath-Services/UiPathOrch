using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// New-OrchTestSetSchedule -- wraps POST /odata/TestSetSchedules.
//
// Minimum-viable surface: Name + TestSetName + CronExpression + Enabled +
// Description + Path. The wrapped server endpoint is POST-only; there is
// no Set-/Update- yet.
[Cmdlet(VerbsCommon.New, "OrchTestSetSchedule", SupportsShouldProcess = true)]
[OutputType(typeof(TestSetSchedule))]
public class NewTestSetScheduleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Name { get; set; }

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestSetNameCompleter))]
    [SupportsWildcards]
    public string? TestSetName { get; set; }

    // Quartz cron expression. Server default is the standard "every
    // minute" expression when omitted, matching New-OrchTrigger.
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

    // Optional calendar binding. Resolves name → CalendarId at submit time.
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

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                var newSchedule = new TestSetSchedule
                {
                    Name = WildcardPattern.Unescape(name),
                };

                newSchedule.AssignStringIfNotNullOrEmpty(Description, (s, v) => s.Description = v);
                newSchedule.AssignBoolIfNotNull(Enabled, (s, v) => s.Enabled = v);
                newSchedule.AssignStringIfNotNullOrEmpty(CronExpression, (s, v) => s.CronExpression = v);
                newSchedule.CronExpression ??= "0 0/1 * 1/1 * ? *";

                newSchedule.AssignStringIfNotNullOrEmpty(TimeZoneId, (s, v) => s.TimeZoneId = v);
                newSchedule.TimeZoneId ??= TimeZoneInfo.Local.Id;

                // Resolve CalendarName -> CalendarId (matches the New-OrchTrigger
                // calendar binding behaviour).
                newSchedule.AssignIdFromName(
                    CalendarName,
                    () => drive.Calendars.Get(),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.CalendarId = v,
                    this, target, "Calendar");

                // Resolve TestSetName -> TestSetId.
                newSchedule.AssignIdFromName(
                    TestSetName,
                    () => drive.TestSets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.TestSetId = v,
                    this, target, "TestSet");

                if (newSchedule.TestSetId is null)
                {
                    WriteError(new ErrorRecord(
                        new OrchException(target, $"TestSet '{TestSetName}' not found in folder '{folder.GetPSPath()}'."),
                        "NewTestSetScheduleTestSetNotFound", ErrorCategory.InvalidOperation, folder));
                    continue;
                }

                if (ShouldProcess(target, "New TestSetSchedule"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateTestSetSchedule(folder.Id!.Value, newSchedule);
                        drive.TestSetSchedules.ClearCache(folder);
                        if (created is not null)
                        {
                            created.Path = folder.GetPSPath();
                            WriteObject(created);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
