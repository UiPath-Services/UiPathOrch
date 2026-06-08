using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare deployed processes (releases) between two folders or Orchestrator instances and
// report the differences. Matches by Name and compares migration-relevant release settings;
// see Compare-OrchAsset for the shared model (SideIndicator, name-match vs broadcast, the
// reference/difference path convention). Volatile fields (Id, Key, timestamps, numeric
// EnvironmentId) are ignored — EnvironmentName is compared instead so it survives a tenant move.
[Cmdlet(VerbsData.Compare, "OrchProcess")]
[OutputType(typeof(OrchComparison))]
public class CompareProcessCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
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

    private static readonly (string Name, Func<Release, object?> Get)[] Comparators =
    [
        ("ProcessKey", r => r.ProcessKey),
        ("ProcessVersion", r => r.ProcessVersion),
        ("Description", r => r.Description),
        ("EntryPointPath", r => r.EntryPointPath),
        ("InputArguments", r => r.InputArguments),
        ("EnvironmentName", r => r.EnvironmentName),
        ("ProcessType", r => r.ProcessType),
        ("JobPriority", r => r.JobPriority),
        ("SpecificPriorityValue", r => r.SpecificPriorityValue),
        ("TargetFramework", r => r.TargetFramework),
        ("IsAttended", r => r.IsAttended),
        ("RequiresUserInteraction", r => r.RequiresUserInteraction),
        ("RemoteControlAccess", r => r.RemoteControlAccess),
        ("RetentionAction", r => r.RetentionAction),
        ("RetentionPeriod", r => r.RetentionPeriod),
        ("Tags", r => EntityComparison.NormalizeTags(r.Tags)),
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

        FolderCompare.Run<Release>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.Releases.Get(folder),
            r => r?.Name,
            r => r!.GetPSPath(),
            Comparators,
            "GetProcessError",
            WriteObject,
            WriteError);
    }
}
