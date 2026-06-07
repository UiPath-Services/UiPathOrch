using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare event (connector) triggers between two folders or Orchestrator instances. Matches by
// Name and compares the connector/operation/filter settings and execution config. Connection
// and event ids are tenant-local and are not compared (ConnectorKey/Operation/ObjectName are).
// See Compare-OrchAsset for the shared model.
[Cmdlet(VerbsData.Compare, "OrchEventTrigger")]
[OutputType(typeof(OrchComparison))]
public class CompareEventTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(EventTriggerNameCompleter))]
    public string? DifferenceName { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EventTriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<ApiTrigger, object?> Get)[] Comparators =
    [
        ("Enabled", t => t.Enabled),
        ("ApiTriggerType", t => t.ApiTriggerType),
        ("ConnectorKey", t => t.ConnectorKey),
        ("Operation", t => t.Operation),
        ("ObjectName", t => t.ObjectName),
        ("FilterExpression", t => t.FilterExpression),
        ("JobPriority", t => t.JobPriority),
        ("RuntimeType", t => t.RuntimeType),
        ("InputArguments", t => t.InputArguments),
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

        FolderCompare.Run<ApiTrigger>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.EventTriggers.Get(folder),
            t => t?.Name,
            t => t!.GetPSPath(),
            Comparators,
            "GetEventTriggerError",
            WriteObject,
            WriteError);
    }
}
