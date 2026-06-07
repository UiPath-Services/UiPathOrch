using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare users between two Orchestrator instances. Users are tenant-level, so the reference
// (-Path) and difference (-DifferencePath) are drives, not folders. Matches by user name and
// compares type, status, license, and the assigned tenant-role set. For cross-tenant compares
// where user names differ, pass -UserMappingCsv (same CSV as Copy-OrchAsset) to translate the
// reference user names to their difference-side equivalents before matching.
[Cmdlet(VerbsData.Compare, "OrchUser")]
[OutputType(typeof(OrchComparison))]
public class CompareUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    public string? DifferenceName { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public string? UserMappingCsv { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<User, object?> Get)[] Comparators =
    [
        ("Type", u => u.Type),
        ("IsActive", u => u.IsActive),
        ("LicenseType", u => u.LicenseType),
        ("ProvisionType", u => u.ProvisionType),
        ("EmailAddress", u => u.EmailAddress),
        ("RolesList", u => NormalizeStringSet(u.RolesList)),
        ("MayHaveUserSession", u => u.MayHaveUserSession),
        ("MayHaveUnattendedSession", u => u.MayHaveUnattendedSession),
        ("MayHavePersonalWorkspace", u => u.MayHavePersonalWorkspace),
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

        // Optional cross-tenant user-name remap for the match key.
        Func<string?, string?>? nameMap = null;
        if (!string.IsNullOrEmpty(UserMappingCsv))
        {
            var srcDrive = SessionState.GetOrchDrive(EffectivePath(Path, LiteralPath));
            var dstDrive = SessionState.GetOrchDrive(DifferencePath);
            var mapping = SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrive, UserMappingCsv);
            if (mapping is not null)
            {
                nameMap = n => (n is not null && mapping.TryGetValue(n, out var d) && !string.IsNullOrEmpty(d)) ? d : n;
            }
        }

        TenantCompare.Run<User>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            IncludeEqual.IsPresent,
            only,
            drive => drive.Users.Get(),
            u => u?.UserName,
            Comparators,
            "GetUserError",
            WriteObject,
            WriteError,
            nameMap);
    }

    // Order-independent normalized form of a string set (e.g. RolesList).
    internal static string? NormalizeStringSet(string[]? values)
        => values is null || values.Length == 0
            ? null
            : string.Join(";", values.Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
}
