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
public class CompareTriggerCmdlet : OrchestratorPSCmdlet
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

    private static readonly (string Name, Func<ProcessSchedule, object?> Get)[] Comparators =
    [
        ("Enabled", t => t.Enabled),
        ("ReleaseName", t => t.ReleaseName),
        ("EntryPointPath", t => t.EntryPointPath),
        ("JobPriority", t => t.JobPriority),
        ("SpecificPriorityValue", t => t.SpecificPriorityValue),
        ("RuntimeType", t => t.RuntimeType),
        ("StartProcessCron", t => t.StartProcessCron),
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

    protected override void ProcessRecord()
    {
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        FolderCompare.Run<ProcessSchedule>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name,
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.GetTriggers(folder),
            t => t?.Name,
            t => t!.GetPSPath(),
            Comparators,
            "GetTriggerError",
            WriteObject,
            WriteError);
    }
}
