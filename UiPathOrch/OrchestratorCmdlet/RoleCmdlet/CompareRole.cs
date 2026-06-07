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
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter]
    public string? DifferenceName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
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
        var srcDrive = SessionState.GetOrchDrive(EffectivePath(Path, LiteralPath));
        var dstDrive = SessionState.GetOrchDrive(DifferencePath);
        var wpName = Name.ConvertToWildcardPatternList();
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        List<Role> refRoles;
        try
        {
            refRoles = srcDrive.Roles.Get().FilterByWildcards(r => r?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }

        // Broadcast mode: every reference role vs the single named target role.
        if (!string.IsNullOrEmpty(DifferenceName))
        {
            Role? target;
            try
            {
                target = dstDrive.Roles.Get().FirstOrDefault(r => string.Equals(r?.Name, DifferenceName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, dstDrive));
                return;
            }
            if (target is null)
            {
                WriteError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator, $"DifferenceName '{DifferenceName}' was not found in '{dstDrive.NameColonSeparator}'."),
                    "DifferenceNameNotFound", ErrorCategory.ObjectNotFound, DifferenceName));
                return;
            }

            foreach (var role in refRoles)
            {
                if (role is null) continue;
                EmitComparison(role, target, srcDrive, dstDrive, only);
            }
            return;
        }

        // Name-match mode.
        List<Role> diffRoles;
        try
        {
            diffRoles = dstDrive.Roles.Get().FilterByWildcards(r => r?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, dstDrive));
            return;
        }

        var diffByName = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in diffRoles)
            if (role?.Name is { } n) diffByName[n] = role;

        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in refRoles)
        {
            if (role is null) continue;
            if (role.Name is { } name && diffByName.TryGetValue(name, out var diffRole))
            {
                matched.Add(name);
                EmitComparison(role, diffRole, srcDrive, dstDrive, only);
            }
            else
            {
                EmitOnly(role, srcDrive, EntityComparison.ReferenceOnly);
            }
        }

        foreach (var kv in diffByName)
        {
            if (matched.Contains(kv.Key)) continue;
            EmitOnly(kv.Value, dstDrive, EntityComparison.DifferenceOnly);
        }
    }

    private void EmitComparison(Role reference, Role difference, OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, HashSet<string>? only)
    {
        var diffs = EntityComparison.DiffProperties(reference, difference, Comparators, only);
        if (diffs.Count == 0)
        {
            if (IncludeEqual.IsPresent)
            {
                WriteObject(new OrchComparison
                {
                    SideIndicator = EntityComparison.Equal,
                    Name = reference.Name,
                    Path = RolePath(srcDrive, reference),
                    DifferencePath = RolePath(dstDrive, difference),
                    ReferenceObject = reference,
                    DifferenceObject = difference,
                });
            }
            return;
        }

        WriteObject(new OrchComparison
        {
            SideIndicator = EntityComparison.Different,
            Name = reference.Name,
            Path = RolePath(srcDrive, reference),
            DifferencePath = RolePath(dstDrive, difference),
            Differences = diffs,
            ReferenceObject = reference,
            DifferenceObject = difference,
        });
    }

    private void EmitOnly(Role role, OrchDriveInfo drive, string side)
    {
        bool isReference = side == EntityComparison.ReferenceOnly;
        WriteObject(new OrchComparison
        {
            SideIndicator = side,
            Name = role.Name,
            Path = isReference ? RolePath(drive, role) : null,
            DifferencePath = isReference ? null : RolePath(drive, role),
            ReferenceObject = isReference ? role : null,
            DifferenceObject = isReference ? null : role,
        });
    }

    private static string RolePath(OrchDriveInfo drive, Role role)
        => System.IO.Path.Combine(drive.NameColonSeparator, role.Name ?? "");

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
