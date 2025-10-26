---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Set-OrchRole

## SYNOPSIS
Adds or updates a role.

## SYNTAX

```
Set-OrchRole [-Name] <String[]> [-Type <String>] -PermissionName <String> [-Scope <String>] [-View <String>]
 [-Edit <String>] [-Create <String>] [-Delete <String>] [-Path <String[]>] [-ExpandPermission]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Set-OrchRole cmdlet creates new roles or updates existing roles in UiPath Orchestrator. Roles define permissions that control user access to various Orchestrator features and resources. This cmdlet allows you to configure role permissions for different areas such as Assets, Jobs, Processes, Queues, and more.

Use the -Name parameter to specify the role name and -PermissionName to specify which permission area to configure. The permission areas include Assets, Jobs, Processes, Queues, Machines, Users, and many others. For each permission area, you can granularly control View, Edit, Create, and Delete access using the corresponding parameters.

The cmdlet supports both Tenant and Folder scoped roles using the -Type parameter. Tenant roles apply across the entire tenant, while Folder roles apply to specific folders and their contents. Use the -Scope parameter to specify the folder scope when creating folder roles.

You can import roles from CSV files exported with Get-OrchRole -ExportCsv. When importing to a different tenant than the source, modify the Path column in the CSV file or use the -Path parameter to override the destination tenant.

The -ExpandPermission parameter provides detailed permission information when working with existing roles, helping you understand the current permission structure before making changes.

Primary Endpoint: GET /odata/Roles&$expand=Permissions, POST /odata/Roles, PUT /odata/Roles({role.Id})

OAuth required scopes: OR.Users or OR.Users.Write

Required permissions: Roles.View, Roles.Create, Roles.Edit or Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Set-OrchRole ReadOnlyAssets -PermissionName Assets -View True -Edit False -Create False -Delete False
```

Tests creating a new role that provides read-only access to Assets using -WhatIf to preview the operation.

### Example 2
```powershell
PS Orch1:\> Set-OrchRole ProcessManager -PermissionName Processes -View True -Edit True -Create True -Delete False
```

Creates a role that can view, edit, and create processes but cannot delete them.

### Example 3
```powershell
PS Orch1:\> Set-OrchRole QueueAdministrator -PermissionName Queues -View True -Edit True -Create True -Delete True
```

Creates a role with full administrative access to queue management operations.

### Example 4
```powershell
PS Orch1:\> Set-OrchRole FolderOperator Folder -Scope Default -PermissionName Jobs -View True -Edit False -Create False -Delete False
```

Creates a folder-scoped role that provides read-only access to jobs within the specified folder scope.

### Example 5
```powershell
PS C:\> Set-OrchRole -Path Orch1:, Orch2: -Name CrossTenantRole -PermissionName Machines -View True -Edit True -Create False -Delete False
```

Creates a role across multiple tenants with view and edit permissions for machines.

### Example 6
```powershell
PS Orch1:\> Set-OrchRole JobManager -PermissionName Jobs -View True -Edit True -Create True -Delete True -ExpandPermission
```

Creates a role with full job management permissions and displays expanded permission details.

### Example 7
```powershell
PS C:\> Import-Csv roles.csv | Set-OrchRole -WhatIf
```

Tests bulk role creation from a CSV file. The CSV should contain columns: Name, PermissionName, View, Edit, Create, Delete, Type, Scope.

### Example 8
```powershell
PS Orch1:\> Set-OrchRole MonitoringRole -PermissionName Logs -View True -Edit False -Create False -Delete False
```

Creates a monitoring role that can view logs for troubleshooting purposes without modification rights.

### Example 9
```powershell
PS Orch1:\> Set-OrchRole UserAdministrator -PermissionName Users -View True -Edit True -Create True -Delete False
```

Creates a user administration role that can manage users but cannot delete user accounts.

### Example 10
```powershell
PS Orch1:\> Set-OrchRole TenantAdmin -PermissionName Settings -View True -Edit True -Create True -Delete True
```

Creates a tenant administrator role with full access to tenant settings and configuration.

### Example 11
```powershell
PS C:\> Import-Csv <filepath> | Set-OrchRole
```

Tests creating a new role that provides read-only access to Assets using -WhatIf to preview the operation.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Create
Specifies the Create permission level for the role. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Delete
Specifies the Delete permission level for the role. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Edit
Specifies the Edit permission level for the role. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExpandPermission
Displays detailed permission information when working with existing roles.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the name of the role to create or update.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -PermissionName
Specifies the permission area to configure (e.g., Assets, Jobs, Processes, Queues, Users).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Scope
Specifies the scope for folder roles.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Type
Specifies the role type. Valid values: Tenant, Folder.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -View
Specifies the View permission level for the role. Valid values: True, False.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.Role
## NOTES

## RELATED LINKS
