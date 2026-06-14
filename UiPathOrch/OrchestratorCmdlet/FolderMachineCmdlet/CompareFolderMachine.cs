using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare folder machine assignments between two folders or Orchestrator instances. Matches by
// machine name and compares the machine's type, scope, and per-folder runtime slot allocation.
// See Compare-OrchAsset for the shared model (SideIndicator, name-match vs broadcast).
[Cmdlet(VerbsData.Compare, "OrchFolderMachine")]
[OutputType(typeof(OrchComparison))]
public class CompareFolderMachineCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
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

    private static readonly (string Name, Func<MachineFolder, object?> Get)[] Comparators =
    [
        ("Type", m => m.Type),
        ("Scope", m => m.Scope),
        ("NonProductionSlots", m => m.NonProductionSlots),
        ("UnattendedSlots", m => m.UnattendedSlots),
        ("HeadlessSlots", m => m.HeadlessSlots),
        ("TestAutomationSlots", m => m.TestAutomationSlots),
        ("AutomationType", m => m.AutomationType),
        ("TargetFramework", m => m.TargetFramework),
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

        FolderCompare.Run<MachineFolder>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.FolderMachinesAssigned.Get(folder),
            m => m?.Name,
            m => m!.GetPSPath(),
            Comparators,
            "GetFolderMachineError",
            WriteObject,
            WriteError);
    }
}
