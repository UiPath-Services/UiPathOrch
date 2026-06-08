using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare machines between two Orchestrator instances. Machines are tenant-level, so the
// reference (-Path) and difference (-DifferencePath) are drives, not folders, and there is no
// -Recurse. Matches by Name and compares the machine type and runtime slot configuration --
// machines must exist on the target with matching capacity for jobs to run after a migration.
[Cmdlet(VerbsData.Compare, "OrchMachine")]
[OutputType(typeof(OrchComparison))]
public class CompareMachineCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string? DifferenceName { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<ExtendedMachine, object?> Get)[] Comparators =
    [
        ("Description", m => m.Description),
        ("Type", m => m.Type),
        ("Scope", m => m.Scope),
        ("NonProductionSlots", m => m.NonProductionSlots),
        ("UnattendedSlots", m => m.UnattendedSlots),
        ("HeadlessSlots", m => m.HeadlessSlots),
        ("TestAutomationSlots", m => m.TestAutomationSlots),
        ("AutomationCloudSlots", m => m.AutomationCloudSlots),
        ("AutomationCloudTestAutomationSlots", m => m.AutomationCloudTestAutomationSlots),
        ("AutomationType", m => m.AutomationType),
        ("TargetFramework", m => m.TargetFramework),
        ("Tags", m => EntityComparison.NormalizeTags(m.Tags)),
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

    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        CompareParameterHelper.WarnSecretNotCompared(this, "a confidential machine's client secret (ClientSecret)");
    }

    protected override void ProcessRecord()
    {
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        TenantCompare.Run<ExtendedMachine>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            IncludeEqual.IsPresent,
            only,
            drive => drive.Machines.Get(),
            m => m?.Name,
            Comparators,
            "GetMachineError",
            WriteObject,
            WriteError);
    }
}
