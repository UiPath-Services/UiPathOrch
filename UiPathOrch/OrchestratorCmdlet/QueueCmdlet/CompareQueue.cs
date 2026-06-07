using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare queue definitions between two folders or Orchestrator instances and report the
// differences. Matches by Name and compares migration-relevant queue settings, including the
// JSON schemas and retention/retry configuration. See Compare-OrchAsset for the shared model
// (SideIndicator, name-match vs broadcast, the reference/difference path convention).
[Cmdlet(VerbsData.Compare, "OrchQueue")]
[OutputType(typeof(OrchComparison))]
public class CompareQueueCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter]
    public string? DifferenceName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<QueueDefinition, object?> Get)[] Comparators =
    [
        ("Description", q => q.Description),
        ("MaxNumberOfRetries", q => q.MaxNumberOfRetries),
        ("AcceptAutomaticallyRetry", q => q.AcceptAutomaticallyRetry),
        ("RetryAbandonedItems", q => q.RetryAbandonedItems),
        ("EnforceUniqueReference", q => q.EnforceUniqueReference),
        ("Encrypted", q => q.Encrypted),
        ("SpecificDataJsonSchema", q => q.SpecificDataJsonSchema),
        ("OutputDataJsonSchema", q => q.OutputDataJsonSchema),
        ("AnalyticsDataJsonSchema", q => q.AnalyticsDataJsonSchema),
        ("SlaInMinutes", q => q.SlaInMinutes),
        ("RiskSlaInMinutes", q => q.RiskSlaInMinutes),
        ("RetentionAction", q => q.RetentionAction),
        ("RetentionPeriod", q => q.RetentionPeriod),
        ("RetentionBucketName", q => q.RetentionBucketName),
        ("Tags", q => EntityComparison.NormalizeTags(q.Tags)),
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

        FolderCompare.Run<QueueDefinition>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.Queues.Get(folder),
            q => q?.Name,
            q => q!.GetPSPath(),
            Comparators,
            "GetQueueError",
            WriteObject,
            WriteError);
    }
}
