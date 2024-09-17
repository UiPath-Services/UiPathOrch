using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchRole")]
    [OutputType(typeof(Entities.Role))]
    [OutputType(typeof(Entities.OrchRolePermissionExpanded))]
    public class GetRoleCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(RoleNameCompleter<Positional.Name>))]
        public string[]? Name { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        //[Parameter]
        //[ValidateSet("Tenant", "Folder", "Mixed")]
        //public string? Type { get; set; }

        [Parameter]
        public SwitchParameter ExpandPermission { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();

            if (!ExpandPermission.IsPresent)
            {
                foreach (var drive in drives)
                {
                    try
                    {
                        WriteObject(drive!.GetRoles()
                            .FilterByWildcards(role => role?.Name, wpName)
                            .OrderByDescending(role => role.Type)
                            .ThenBy(role => role.Name),
                            true);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                    }
                }
                return;
            }

            // ExpandPermission.IsPresent == true
            foreach (var drive in drives)
            {
                try
                {
                    foreach (var role in drive!.GetRoles()
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
                            int idx = q.Name!.IndexOf(".");
                            if (idx != -1)
                            {
                                return (q.Scope, Name: q.Name.Substring(0, idx));
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
                                Name = item.Key.Name,
                                Scope = item.Key.Scope,
                                Type = role.Type,
                                Path = System.IO.Path.Combine(drive.NameColon, role.Name!)
                            };
                            foreach (var s in item)
                            {
                                if (s.Name!.Contains(".View")) { expanded.View = true; }
                                else if (s.Name.Contains(".Edit")) { expanded.Edit = true; }
                                else if (s.Name.Contains(".Create")) { expanded.Create = true; }
                                else if (s.Name.Contains(".Delete")) { expanded.Delete = true; }
                            }
                            output.Add(expanded);
                        }

                        WriteObject(output.OrderBy(p => p.Name).ThenByDescending(p => p.Scope), true);
                    }
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive);
                    WriteError(errorRecord);
                }
            }
        }
    }
}
