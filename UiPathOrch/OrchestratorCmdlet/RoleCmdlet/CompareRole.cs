using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare roles between two Orchestrator instances (or the same one) and report the
// differences. Roles are tenant-level, so the reference (-Path) and difference (-DifferencePath)
// are drives, not folders, and there is no -Recurse. Matches by Name and compares the role
// shape plus its granted-permission matrix (normalized to an order-independent set of granted
// "Scope:Permission" entries). See Compare-OrchAsset for the shared model (SideIndicator,
// name-match vs broadcast).
[Cmdlet(VerbsData.Compare, "OrchRole")]
[OutputType(typeof(OrchComparison))]
public class CompareRoleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(RoleNameCompleter))]
    [SupportsWildcards]
    public string? DifferenceName { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<Role, object?> Get)[] Comparators =
    [
        ("DisplayName", r => r.DisplayName),
        ("Type", r => r.Type),
        ("Groups", r => r.Groups),
        ("IsStatic", r => r.IsStatic),
        ("IsEditable", r => r.IsEditable),
        ("Permissions", r => NormalizePermissions(r.Permissions)),
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

        TenantCompare.Run<Role>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            IncludeEqual.IsPresent,
            only,
            drive => drive.Roles.Get(),
            r => r?.Name,
            Comparators,
            "GetRoleError",
            WriteObject,
            WriteError);
    }

    // Order-independent normalized form of the granted-permission matrix: only granted
    // permissions, as a sorted set of "Scope:Permission" entries. Captures grant/revoke and
    // scope drift without depending on permission list order.
    internal static string? NormalizePermissions(List<Permission>? permissions)
        => permissions is null || permissions.Count == 0
            ? null
            : string.Join(";", permissions
                .Where(p => p.IsGranted == true)
                .Select(p => $"{p.Scope}:{p.Name}")
                .OrderBy(s => s, StringComparer.Ordinal));
}
