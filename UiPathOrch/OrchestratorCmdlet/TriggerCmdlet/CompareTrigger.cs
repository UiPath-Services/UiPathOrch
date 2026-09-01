using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare time/queue triggers (process schedules) between two folders or Orchestrator
// instances. Matches by Name and compares the schedule and execution settings. See
// Compare-OrchAsset for the shared model (SideIndicator, name-match vs broadcast).
[Cmdlet(VerbsData.Compare, "OrchTrigger")]
[OutputType(typeof(OrchComparison))]
public class CompareTriggerCmdlet : CompareOrchCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string? DifferenceName { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    // A queue trigger fires on queue items, not on a clock, and the web UI offers it no cron at
    // all. ProcessScheduleDto carries StartProcessCron for it anyway, and the value there is the
    // SERVER's, not the caller's. Measured on Automation Cloud 26.3: creating a queue trigger with
    // "13 7/29 * 1/1 * ? *" reads back "39 3/30 * * * ? *", and a second one created straight
    // after reads back "40 3/30 * * * ? *" -- the server assigns its own, and consecutive triggers
    // get consecutive values. Posting the DTO directly, bypassing New-OrchTrigger, is rewritten
    // the same way, so it is the server and not this module. A TIME trigger's cron survives
    // untouched on the same server, which is why only the queue case is suppressed.
    //
    // It is server-VERSION dependent: the same raw POST against Automation Suite 24.10.11 stored
    // the posted cron verbatim. So the two sides of a migration can hold values neither user chose
    // and neither side can control -- which is the reported case, "0 0/30 * 1/1 * ? *" at an MSI
    // source against "33 20/30 * * * ? *" at an Automation Suite destination (2026-08-31).
    //
    // This also settles what Copy-Item can do about it: nothing. CopyTriggers already sends the
    // source cron -- StartProcessCron is not among the fields it nulls -- and the destination
    // server overrides it anyway.
    internal static bool IsQueueTrigger(ProcessSchedule t) => t.QueueDefinitionId.GetValueOrDefault() != 0;

    // True when suppressing StartProcessCron is hiding a real difference, i.e. when the "==" row
    // would otherwise be read as "the cron matched". Pure so the decision is unit-testable: the
    // equal case cannot be built live, because two queue triggers created separately are given
    // different crons by the server.
    internal static bool HidesACronDifference(ProcessSchedule reference, ProcessSchedule difference)
        => (IsQueueTrigger(reference) || IsQueueTrigger(difference))
           && !EntityComparison.ValueEquals(reference.StartProcessCron, difference.StartProcessCron);

    internal static readonly (string Name, Func<ProcessSchedule, object?> Get)[] Comparators =
    [
        ("Enabled", t => t.Enabled),
        ("ReleaseName", t => t.ReleaseName),
        ("EntryPointPath", t => t.EntryPointPath),
        ("JobPriority", t => EntityComparison.EffectiveJobPriority(t.JobPriority, t.SpecificPriorityValue)),
        ("SpecificPriorityValue", t => t.SpecificPriorityValue),
        ("RuntimeType", t => t.RuntimeType),
        // Read as null on both sides for a queue trigger; QueueDefinitionName carries the real
        // difference when one side is a queue trigger and the other is not.
        ("StartProcessCron", t => IsQueueTrigger(t) ? null : t.StartProcessCron),
        ("StartStrategy", t => t.StartStrategy),
        ("StopStrategy", t => t.StopStrategy),
        ("TimeZoneId", t => t.TimeZoneId),
        ("UseCalendar", t => t.UseCalendar),
        ("CalendarName", t => t.CalendarName),
        ("InputArguments", t => t.InputArguments),
        ("QueueDefinitionName", t => t.QueueDefinitionName),
        ("ActivateOnJobComplete", t => t.ActivateOnJobComplete),
        ("Description", t => t.Description),
        ("Tags", t => EntityComparison.NormalizeTags(t.Tags)),
    ];

    internal static readonly HashSet<string> ValidPropertyNames =
        new(Comparators.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

    // Queue triggers whose StartProcessCron actually differs, reported once at the end.
    //
    // The notice exists so an "==" row is not read as "the cron matched" -- but that misreading
    // only has consequences when the value really does differ. When the two crons are identical
    // the row is equal in every sense and nothing is being withheld, so saying anything is noise,
    // and a migration pass over hundreds of matching triggers would train the reader to skip the
    // warnings that do carry something.
    //
    // This is where the comparison parts company with WarnSecretNotCompared, which fires on
    // presence alone: a secret's value is never returned, so the module cannot know whether it
    // drifted. Here both values are in hand.
    private readonly List<string> _queueTriggerCronDiffs = [];

    private void NoteQueueTriggerCron(ProcessSchedule reference, ProcessSchedule difference)
        => AddCronDiff(_queueTriggerCronDiffs, reference, difference);

    // Pure, so "one notice per run" and "counted by path" are pinned by unit tests. They cannot be
    // pinned live: a queue trigger's cron is assigned by the server and cannot be set at create or
    // update (measured on Automation Cloud 26.3 -- Update-OrchTrigger -StartProcessCron leaves it
    // as it was), so whether two of them differ is a matter of when each happened to be created.
    internal static void AddCronDiff(List<string> accumulated, ProcessSchedule reference, ProcessSchedule difference)
    {
        if (!HidesACronDifference(reference, difference)) return;

        // Keyed on the reference's PATH, not its name. With -Recurse the same trigger name recurs
        // in every mirrored folder -- the shape a migration produces -- so deduplicating by name
        // would report "1 queue trigger" when several differ and name only the first folder's.
        // A key is still needed: ProcessRecord runs once per piped item, so the same pair can
        // arrive more than once and must count once.
        var key = reference.GetPSPath();
        if (!string.IsNullOrEmpty(key) && !accumulated.Contains(key, StringComparer.OrdinalIgnoreCase))
            accumulated.Add(key);
    }

    // One notice for the whole run, naming enough of the triggers to be checked without turning
    // into the wall of text a per-trigger warning would have been.
    internal static string ComposeCronNotice(IReadOnlyList<string> paths)
    {
        const int show = 5;
        var named = string.Join(", ", paths.Take(show));
        if (paths.Count > show) named += $", and {paths.Count - show} more";

        return $"StartProcessCron differs on {paths.Count} queue trigger(s) and was deliberately NOT reported: " +
               $"{named}. A queue trigger fires on queue items and the web UI offers it no cron, so that value is " +
               "assigned by the server rather than chosen by either side. Nothing else about these triggers was skipped.";
    }

    protected override void EndProcessing()
    {
        if (_queueTriggerCronDiffs.Count > 0) WriteWarning(ComposeCronNotice(_queueTriggerCronDiffs));
        base.EndProcessing();
    }

    protected override IEnumerable<string> GetTargetDriveNames()
    {
        foreach (var n in base.GetTargetDriveNames()) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("DifferencePath", out var dp))
            foreach (var n in ExtractDriveNamesFromBoundPath(dp)) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("LiteralPath", out var lp))
            foreach (var n in ExtractDriveNamesFromBoundPath(lp)) yield return n;
    }

    protected override string DefaultCsvName => "ComparedTriggers.csv";

    protected override void ProcessRecord()
    {
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        FolderCompare.Run<ProcessSchedule>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.GetTriggers(folder),
            t => t?.Name,
            t => t!.GetPSPath(),
            Comparators,
            "GetTriggerError",
            CsvOrPipeline(),
            WriteError,
            NoteQueueTriggerCron);
    }
}
