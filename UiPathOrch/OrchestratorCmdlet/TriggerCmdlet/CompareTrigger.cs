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
    // all. ProcessScheduleDto carries StartProcessCron for it anyway, and whatever sits in that
    // field is left over from whoever wrote the trigger last -- this module defaults it to
    // "0 0/1 * 1/1 * ? *" when one is missing, and the web UI's builder writes its own form. It is
    // not a setting either side's user chose, so a difference in it is one nobody can act on.
    //
    // Reported from an MSI-to-Automation-Suite migration (2026-08-31): a correctly copied queue
    // trigger compared "0 0/30 * 1/1 * ? *" at the source against "33 20/30 * * * ? *" at the
    // destination. The destination server is NOT what changed it -- measured on Automation Suite
    // 24.10, POSTing a queue trigger stores the cron verbatim, even when StartProcessCronDetails
    // describes a different schedule. Where that particular expression came from is unresolved;
    // its format is the web UI builder's, not this module's. The field is skipped on its own
    // merits rather than on a theory of who wrote it.
    internal static bool IsQueueTrigger(ProcessSchedule t) => t.QueueDefinitionId.GetValueOrDefault() != 0;

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
            WriteError);
    }
}
