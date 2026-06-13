using System.Data;
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchRole")]
[OutputType(typeof(Entities.Role))]
[OutputType(typeof(Entities.OrchRolePermissionExpanded))]
public class GetRoleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(RoleNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    //[Parameter]
    //[ValidateSet("Tenant", "Folder", "Mixed")]
    //public string? Type { get; set; }

    [Parameter]
    public SwitchParameter ExpandPermission { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedRoles.csv";

    private static readonly string[] CsvHeaders = [
        "Path", "Name", "Type", "PermissionName", "Scope", "IsEditable", "View", "Edit", "Create", "Delete"
    ];

    private static void WriteCsvContent(StreamWriter writer, IEnumerable<OrchRolePermissionExpanded> output)
    {
        foreach (var permission in output)
        {
            string[] line = [
                EscapeCsvValue(permission.Path, true),
                EscapeCsvValue(permission.Name, true),
                EscapeCsvValue(permission.Type),
                EscapeCsvValue(permission.PermissionName),
                EscapeCsvValue(permission.Scope),
                EscapeCsvValue(permission.IsEditable),
                EscapeCsvValue(permission.View),
                EscapeCsvValue(permission.Edit),
                EscapeCsvValue(permission.Create),
                EscapeCsvValue(permission.Delete)
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        if (!ExpandPermission.IsPresent && writer is null)
        {
            using var simpleResults = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.Roles.Get()
                    .FilterByNames(role => role?.Name, Name)
                    .OrderByDescending(role => role.Type)
                    .ThenBy(role => role.Name)
                    .ToList());

            using var simpleCancel = new ConsoleCancelHandler();
            foreach (var result in simpleResults)
            {
                try
                {
                    var roles = result.GetResult(simpleCancel.Token);
                    if (roles is null) continue;
                    WriteObject(roles, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetRoleError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
            return;
        }

        // ExpandPermission.IsPresent == true (or CSV export). Worker expands per-drive into
        // a list of OrchRolePermissionExpanded; main thread emits or writes CSV.
        using var expandedResults = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive =>
            {
                var output = new List<OrchRolePermissionExpanded>();
                foreach (var role in drive.Roles.Get()
                    .FilterByNames(role => role?.Name, Name)
                    .OrderByDescending(role => role.Type)
                    .ThenBy(role => role.Name))
                {
                    var p = role.Permissions;
                    IEnumerable<Permission> q;
                    if (role.Type == "Tenant") { q = p!.Where(p => p.Scope != "Folder"); }
                    else if (role.Type == "Folder") { q = p!.Where(p => p.Scope != "Global"); }
                    else { q = p!; }

                    var groupByPermissions = q.GroupBy(q =>
                    {
                        var splitName = q.Name?.Split('.') ?? [];
                        if (splitName.Length > 1)
                        {
                            return (q.Scope, Name: splitName[0]);
                        }
                        else
                        {
                            return (q.Scope, q.Name);
                        }
                    });

                    foreach (var item in groupByPermissions)
                    {
                        var expanded = new OrchRolePermissionExpanded
                        {
                            PermissionName = item.Key.Name,
                            Type = role.Type,
                            IsEditable = role.IsEditable,
                            Scope = item.Key.Scope,
                            Path = drive.NameColonSeparator,
                            Name = role.Name,
                            PathName = System.IO.Path.Combine(drive.NameColonSeparator, role.Name!)
                        };
                        foreach (var s in item)
                        {
                            if (string.IsNullOrEmpty(s.Name)) continue;
                            if (s.Name.Contains(".View")) { expanded.View = s.IsGranted; }
                            else if (s.Name.Contains(".Edit")) { expanded.Edit = s.IsGranted; }
                            else if (s.Name.Contains(".Create")) { expanded.Create = s.IsGranted; }
                            else if (s.Name.Contains(".Delete")) { expanded.Delete = s.IsGranted; }
                        }
                        output.Add(expanded);
                    }
                }
                return output.OrderBy(p => p.PermissionName).ThenByDescending(p => p.Scope).ToList();
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in expandedResults)
        {
            try
            {
                var permissions = result.GetResult(cancelHandler.Token);
                if (permissions is null) continue;

                if (writer is not null)
                {
                    WriteCsvContent(writer, permissions);
                }
                else
                {
                    WriteObject(permissions, true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetRoleError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
