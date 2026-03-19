using System.Data;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Set, "OrchRole", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Role))]
public class SetRoleCommand : OrchestratorPSCmdlet
{
    private class PermissionParams(string? scope, string? view, string? edit, string? create, string? delete)
    {
        // PermissionName is managed as a dictionary key
        public string? Scope { get; set; } = scope;
        public bool? View   { get; set; } = view.ToNullableBool();
        public bool? Edit   { get; set; } = edit.ToNullableBool();
        public bool? Create { get; set; } = create.ToNullableBool();
        public bool? Delete { get; set; } = delete.ToNullableBool();
    }

    private class RoleParams(string? type)
    {
        public string? Type { get; set; } = type;
        public Dictionary<string, PermissionParams> Permissions { get; set; } = [];
    }

    // Key: Name
    private Dictionary<(OrchDriveInfo drive, string name), RoleParams>? _parameterSet;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(RoleNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Type { get; set; }

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? PermissionName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Scope { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? View { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Edit { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Create { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Delete { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter ExpandPermission { get; set; }

    protected override void ProcessRecord()
    {
        _parameterSet ??= [];
        var drives = SessionState.EnumOrchDrives(Path);

        foreach (var drive in drives)
        {
            foreach (var name in Name!)
            {
                if (!_parameterSet.TryGetValue((drive, name), out var roleParams))
                {
                    roleParams = new(Type);
                    _parameterSet[(drive, name)] = roleParams;
                }

                string target = System.IO.Path.Combine(drive.NameColonSeparator, name);

                if (roleParams.Type != Type)
                {
                    WriteWarning($"{target}: Type were specified multiple times. Using '{roleParams.Type}'.");
                }

                if (roleParams.Permissions.ContainsKey(PermissionName!))
                {
                    WriteWarning($"{target}: Permission '{PermissionName}' were specified multiple times. Using the one specified first.");
                }
                else
                {
                    var permission = new PermissionParams(Scope, View, Edit, Create, Delete);
                    roleParams.Permissions[PermissionName!] = permission;
                }
            }
        }
    }

    // Returns isDirty
    private bool SetPermission(string target, Role postingRole, string permissionFullName, PermissionParams permission, bool? newValue)
    {
        bool isDirty = false;
        var existingPermission = postingRole.Permissions?.FirstOrDefault(p => string.Compare(p.Name, permissionFullName, true) == 0);
        if (existingPermission is not null)
        {
            if (existingPermission.Scope != permission.Scope)
            {
                WriteWarning($"{System.IO.Path.Combine(target, permissionFullName)}: Scope mismatch. Existing scope is '{existingPermission.Scope}' but '{permission.Scope}' was specified. Ignoring {permissionFullName}.");
            }
            else
            {
                if (existingPermission.IsGranted != (newValue ?? false))
                {
                    existingPermission.IsGranted = newValue ?? false;
                    isDirty = true;
                }
            }
        }
        else if (newValue is not null)
        {
            postingRole.Permissions ??= [];
            postingRole.Permissions.Add(new Permission()
            {
                Name = permissionFullName,
                IsGranted = newValue,
                Scope = permission.Scope
            });
            isDirty = true;
        }
        return isDirty;
    }

    private static Role RoleParamsToRole(string nameUnescaped, RoleParams roleParams)
    {
        Role postingRole = new()
        {
            Name = nameUnescaped,
            DisplayName = nameUnescaped,
            Type = roleParams.Type,
            Permissions = []
        };
        foreach (var p in roleParams.Permissions)
        {
            if (p.Value.View is not null)
            {
                postingRole.Permissions.Add(
                     new Permission()
                     {
                         Name = p.Key + ".View",
                         IsGranted = p.Value.View,
                         Scope = p.Value.Scope
                     });
            }
            if (p.Value.Edit is not null)
            {
                postingRole.Permissions.Add(
                     new Permission()
                     {
                         Name = p.Key + ".Edit",
                         IsGranted = p.Value.Edit,
                         Scope = p.Value.Scope
                     });
            }
            if (p.Value.Create is not null)
            {
                postingRole.Permissions.Add(
                     new Permission()
                     {
                         Name = p.Key + ".Create",
                         IsGranted = p.Value.Create,
                         Scope = p.Value.Scope
                     });
            }
            if (p.Value.Delete is not null)
            {
                postingRole.Permissions.Add(
                     new Permission()
                     {
                         Name = p.Key + ".Delete",
                         IsGranted = p.Value.Delete,
                         Scope = p.Value.Scope
                     });
            }
        }
        return postingRole;
    }

    protected override void EndProcessing()
    {
        if (_parameterSet is null) return;

        foreach (var parameterSet in _parameterSet)
        {
            var (drive, name) = parameterSet.Key;
            var roleParams = parameterSet.Value;

            var existingRoles = drive.Roles.Get();

            var wpName = new WildcardPattern(name);

            var targetRoles = existingRoles
                //.Where(r => r.IsEditable.GetValueOrDefault()) // Adding this condition causes it to try creating new roles instead..
                .Where(r => wpName.IsMatch(r.Name))
                .ToList();

            if (targetRoles.Count != 0) // Update existing roles
            {
                foreach (var role in targetRoles.OrderBy(r => r.Name))
                {
                    string target = role.GetPSPath();

                    if (roleParams.Type != role.Type)
                    {
                        WriteWarning($"{target}: Type mismatch. Existing type is '{role.Type}', but '{Type}' was specified. Skipping the update.");
                        continue;
                    }

                    bool isDirty = false;
                    var postingRole = OrchCollectionExtensions.DeepCopy(role);
                    foreach (var (permissionName, permission) in roleParams.Permissions)
                    {
                        // Even if isDirty is already true, we still need to call SetPermission(),
                        // so we must not use the short-circuit OR operator || here
                        isDirty = isDirty | SetPermission(target, postingRole, permissionName + ".View",   permission, permission.View);
                        isDirty = isDirty | SetPermission(target, postingRole, permissionName + ".Edit",   permission, permission.Edit);
                        isDirty = isDirty | SetPermission(target, postingRole, permissionName + ".Create", permission, permission.Create);
                        isDirty = isDirty | SetPermission(target, postingRole, permissionName + ".Delete", permission, permission.Delete);
                    }

                    if (isDirty && ShouldProcess(target, "Update Role"))
                    {
                        try
                        {
                            // Returns nothing
                            drive.OrchAPISession.PutRole(postingRole);
                            drive.Roles.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateRoleError", ErrorCategory.InvalidOperation, drive));
                        }
                    }
                }
            }
            else // Create a new role
            {
                string nameUnescaped = WildcardPattern.Unescape(name);
                string target = drive.NameColonSeparator + nameUnescaped;
                if (ShouldProcess(target, "New Role"))
                {
                    try
                    {
                        // Convert the parameterSet to a Role and POST it
                        var postingRole = RoleParamsToRole(nameUnescaped, roleParams);
                        var createdRole = drive.OrchAPISession.PostRole(postingRole);
                        drive.Roles.ClearCache();
                        if (createdRole is not null)
                        {
                            createdRole.Path = drive.NameColonSeparator;
                            WriteObject(createdRole);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewRoleError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
