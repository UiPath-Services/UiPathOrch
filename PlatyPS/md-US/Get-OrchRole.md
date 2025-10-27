---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchRole

## SYNOPSIS
Gets the roles.

## SYNTAX

```
Get-OrchRole [-Name <String[]>] [-Path <String[]>] [-ExpandPermission] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Gets role information from UiPath Orchestrator tenants. Roles define permissions and access rights for users and can be either tenant-level or folder-level. This cmdlet retrieves role metadata and can expand detailed permission information.

Roles are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name.

Primary Endpoint: GET /odata/Roles?$expand=Permissions

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Roles.View or Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchRole
```

Gets all roles from the current tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchRole *Admin*
```

Gets roles containing Admin in their name.

### Example 3
```powershell
PS Orch1:\> Get-OrchRole -ExpandPermission | Where-Object {$_.PermissionName -eq "Assets"}
```

Gets roles with Assets permission using expanded details.

### Example 4
```powershell
PS Orch1:\> Get-OrchRole | Where-Object {$_.IsStatic -eq $false}
```

Gets custom (non-static) roles that can be modified.

## PARAMETERS

### -ExpandPermission
Expands detailed permission information for each role, showing individual permissions like Assets.View, Processes.Edit, etc.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Specifies target tenants by drive name. Use comma-separated values for multiple tenants. If not specified, targets the current tenant.

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

### -ProgressAction
Controls how progress information is displayed during cmdlet execution.

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

### -Name
Specifies the names of roles to retrieve. Supports wildcards and multiple values.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -CsvEncoding
Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Exports results to CSV file with UTF-8 BOM encoding. Automatically converts internal IDs to human-readable names. Can be used with corresponding Import cmdlets.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Role
### UiPath.PowerShell.Entities.OrchRolePermissionExpanded
## NOTES
Role entities are tenant-scoped. They operate across the entire tenant and are not folder-specific.

Roles can be either Tenant or Folder type. Static roles are built-in and cannot be modified, while custom roles can be edited.

Use -ExpandPermission when you need detailed permission analysis or want to filter roles by specific permissions.

The -ExportCsv parameter creates import-ready CSV files with human-readable names instead of internal IDs.



Primary Endpoint: GET /odata/Roles
OAuth required scopes: OR.Users or OR.Users.Read
Required permissions: Roles.View

## RELATED LINKS

[Set-OrchRole](Set-OrchRole.md)

[Copy-OrchRole](Copy-OrchRole.md)

[Remove-OrchRole](Remove-OrchRole.md)

[Add-OrchRoleToFolderUser](Add-OrchRoleToFolderUser.md)

