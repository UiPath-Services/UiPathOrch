using System.Data;
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchRole")]
[OutputType(typeof(Entities.Role))]
[OutputType(typeof(Entities.OrchRolePermissionExpanded))]
public class GetRoleCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(RoleNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

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
            WriteCsvLine(writer, line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        if (!ExpandPermission.IsPresent && writer is null)
        {
            foreach (var drive in drives)
            {
                try
                {
                    var targetRoles = drive!.Roles.Get()
                        .FilterByWildcards(role => role?.Name, wpName)
                        .OrderByDescending(role => role.Type)
                        .ThenBy(role => role.Name);

                    WriteObject(targetRoles, true);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                }
            }
            return;
        }

        // ExpandPermission.IsPresent == true
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                foreach (var role in drive!.Roles.Get()
                    .FilterByWildcards(role => role?.Name, wpName)
                    .OrderByDescending(role => role.Type)
                    .ThenBy(role => role.Name))
                {
                    var p = role.Permissions;
                    IEnumerable<Permission> q = null;
                         if (role.Type == "Tenant") { q = p!.Where(p => p.Scope != "Folder"); }
                    else if (role.Type == "Folder") { q = p!.Where(p => p.Scope != "Global"); }
                    else { q = p; }

                    var groupByPermissions = q!.GroupBy(q =>
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

                    var output = new List<OrchRolePermissionExpanded>();
                    foreach (var item in groupByPermissions)
                    {
                        var expanded = new OrchRolePermissionExpanded
                        {
                            //expanded.Name = role.DisplayName;
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
                                 if (s.Name.Contains(".View"))   { expanded.View = s.IsGranted; }
                            else if (s.Name.Contains(".Edit"))   { expanded.Edit = s.IsGranted; }
                            else if (s.Name.Contains(".Create")) { expanded.Create = s.IsGranted; }
                            else if (s.Name.Contains(".Delete")) { expanded.Delete = s.IsGranted; }
                        }
                        output.Add(expanded);
                    }

                    if (writer is not null)
                    {
                        WriteCsvContent(writer, output.OrderBy(p => p.PermissionName).ThenByDescending(p => p.Scope));
                    }
                    else
                    {
                        WriteObject(output.OrderBy(p => p.PermissionName).ThenByDescending(p => p.Scope), true);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
