using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using BoolCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.True_False>;
using System;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    // WIP
    [Cmdlet(VerbsCommon.Set, "OrchRole", SupportsShouldProcess = true)]
    //[OutputType(typeof(Entities.Role))]
    class SetRoleCommand : OrchestratorPSCmdlet
    {
        public class PermissionParams
        {
            // PermissionName は辞書のキーで管理
            public string? Scope { get; set; }
            public bool? View { get; set; }
            public bool? Edit { get; set; }
            public bool? Create { get; set; }
            public bool? Delete { get; set; }

            public PermissionParams(string? Scope, string? view, string? edit, string? create, string? delete)
            {
                this.Scope = Scope;
                if (bool.TryParse(view,   out var bView))   { this.View   = bView; }
                if (bool.TryParse(edit,   out var bEdit))   { this.Edit   = bEdit; }
                if (bool.TryParse(create, out var bCreate)) { this.Create = bCreate; }
                if (bool.TryParse(delete, out var bDelete)) { this.Delete = bDelete; }
            }
        }

        // Key: Name
        public Dictionary<(OrchDriveInfo drive, string name), (string? type, Dictionary<string, PermissionParams> permissions)>? _parameterSet;

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(RoleNameCompleter<Positional.Name>))]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Type { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
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

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter ExpandPermission { get; set; }

        protected override void ProcessRecord()
        {
            _parameterSet ??= [];
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            foreach (var drive in drives)
            {
                foreach (var name in Name!)
                {
                    if (_parameterSet.TryGetValue((drive, name), out var type_permissions))
                    {
                        string target = System.IO.Path.Combine(drive.NameColonSeparator, name);

                        if (type_permissions.type == null)
                        {
                            type_permissions.type = Type;
                        }
                        else if (type_permissions.type != Type)
                        {
                            WriteWarning($"{target}: Type were specified multiple times. Using '{type_permissions.type}'.");
                        }

                        var (type, permissions) = type_permissions;
                        if (permissions!.ContainsKey(PermissionName!))
                        {
                            WriteWarning($"{target}: Permission '{PermissionName}' were specified multiple times. Using the one specified first.");
                        }
                        else
                        {
                            var permission = new PermissionParams(Scope, View, Edit, Create, Delete);
                            permissions[PermissionName!] = permission;
                            _parameterSet[(drive, name)] = (type_permissions.type, permissions);
                        }
                    }
                    else
                    {
                        var permission = new PermissionParams(Scope, View, Edit, Create, Delete);
                        Dictionary<string, PermissionParams> permissoins = [];
                        permissoins[PermissionName!] = permission;
                        _parameterSet[(drive, name)] = (type_permissions.type, permissoins);
                    }
                }
            }
        }

        // isDirty を返す
        private bool SetPermission(string target, Role postingRole, string permissionFullName, PermissionParams permission, bool newValue)
        {
            bool isDirty = false;
            var existingPermission = postingRole.Permissions?.FirstOrDefault(p => p.Name == permissionFullName);
            if (existingPermission != null)
            {
                if (existingPermission.Scope != permission.Scope)
                {
                    WriteWarning($"{System.IO.Path.Combine(target, permissionFullName)}: Scope mismatch. Existing scope is '{existingPermission.Scope}' but '{permission.Scope}' was specified. Ignoring {permissionFullName}.");
                }
                else
                {
                    if (existingPermission.IsGranted != newValue)
                    {
                        existingPermission.IsGranted = newValue;
                        isDirty = true;
                    }
                }
            }
            else
            {
                postingRole.Permissions ??= [];
                postingRole.Permissions.Add(new Permission()
                {
                    Name = permissionFullName,
                    DisplayName = permissionFullName,
                    IsGranted = newValue,
                    Scope = permission.Scope
                });
                isDirty = true;
            }
            return isDirty;
        }

        protected override void EndProcessing()
        {
            if (_parameterSet == null) return;

            foreach (var parameterSet in _parameterSet)
            {
                var (drive, name) = parameterSet.Key;
                var (type, permissions) = parameterSet.Value;

                var existingRoles = drive.GetRoles();

                var wpName = new WildcardPattern(name);

                var targetRoles = existingRoles
                    .Where(r => r.IsEditable.GetValueOrDefault()) // 更新できるものだけを対象にする
                    .Where(r => wpName.IsMatch(r.Name));

                if (targetRoles.Any()) // 既存のロールを更新
                {
                    foreach (var role in targetRoles.OrderBy(r => r.Name))
                    {
                        string target = role.GetPSPath();

                        if (type != role.Type)
                        {
                            WriteWarning($"{target}: Type mismatch. Existing type is '{role.Type}', but '{type}' was specified. Skipping the update.");
                            continue;
                        }

                        bool isDirty = false;
                        var postingRole = OrchCollectionExtensions.DeepCopy(role);
                        foreach (var name_permission in permissions)
                        {
                            var permissionName = name_permission.Key;
                            var permission = name_permission.Value;

                            isDirty = isDirty || SetPermission(target, postingRole, permissionName + ".View",   permission, permission.View.GetValueOrDefault());
                            isDirty = isDirty || SetPermission(target, postingRole, permissionName + ".Edit",   permission, permission.Edit.GetValueOrDefault());
                            isDirty = isDirty || SetPermission(target, postingRole, permissionName + ".Create", permission, permission.Create.GetValueOrDefault());
                            isDirty = isDirty || SetPermission(target, postingRole, permissionName + ".Delete", permission, permission.Delete.GetValueOrDefault());
                        }

                        if (isDirty && ShouldProcess(target, "Update Role"))
                        {
                            try
                            {
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }
                else // 新規にロールを作成
                {
                    string nameUnescaped = WildcardPattern.Unescape(name);

                }
            }
        }
    }
}
