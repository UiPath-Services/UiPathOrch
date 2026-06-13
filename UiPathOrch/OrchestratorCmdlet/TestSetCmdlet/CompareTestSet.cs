using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare test sets between two folders or Orchestrator instances. Matches by Name and compares
// the test-set configuration. See Compare-OrchAsset for the shared model (SideIndicator,
// name-match vs broadcast, the reference/difference convention).
[Cmdlet(VerbsData.Compare, "OrchTestSet")]
[OutputType(typeof(OrchComparison))]
public class CompareTestSetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestSetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(TestSetNameCompleter))]
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

    private static readonly (string Name, Func<TestSet, object?> Get)[] Comparators =
    [
        ("Description", t => t.Description),
        ("SourceType", t => t.SourceType),
        ("Enabled", t => t.Enabled),
        ("EnableCoverage", t => t.EnableCoverage),
        ("TestCaseCount", t => t.TestCaseCount),
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

        FolderCompare.Run<TestSet>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name,
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.TestSets.Get(folder),
            t => t?.Name,
            t => t!.GetPSPath(),
            Comparators,
            "GetTestSetError",
            WriteObject,
            WriteError);
    }
}
