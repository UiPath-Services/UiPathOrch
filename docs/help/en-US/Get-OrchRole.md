---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchRole.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchRole
---

# Get-OrchRole

## SYNOPSIS

Gets roles from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchRole [[-Name] <string[]>] [-Path <string[]>] [-ExpandPermission] [-ExportCsv <string>]
 [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets role information from UiPath Orchestrator. Roles define sets of permissions that can be assigned to users for controlling access to Orchestrator resources. Roles have a Type property (Tenant, Folder, or Mixed) that determines which permission scopes they include.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available role names. Multiple values can be specified using comma-separated text that includes wildcards.

When -ExpandPermission is specified, the cmdlet outputs individual permission entries for each role, grouped by permission name with View, Edit, Create, and Delete columns.

The cmdlet supports CSV export (requires -ExpandPermission) with columns: Path, Name, Type, PermissionName, Scope, IsEditable, View, Edit, Create, Delete. The exported CSV can be imported back using Set-OrchRole.

Roles are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Roles?$expand=Permissions

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Roles.View or Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1: Get all roles

```powershell
PS Orch1:\> Get-OrchRole
```

Gets all roles from the current Orchestrator tenant.

### Example 2: Get roles by name

```powershell
PS Orch1:\> Get-OrchRole Automation*
```

Gets roles whose names match `Automation*`.

### Example 3: Get roles with expanded permissions

```powershell
PS Orch1:\> Get-OrchRole "Folder Administrator" -ExpandPermission
```

Gets the role "Folder Administrator" with its permissions expanded into individual rows showing View, Edit, Create, and Delete settings.

### Example 4: Export roles to CSV

```powershell
PS Orch1:\> Get-OrchRole -ExpandPermission -ExportCsv C:\temp\roles.csv
```

Exports all roles with their permissions to a CSV file. This CSV can be imported to another tenant using Set-OrchRole.

### Example 5: Get roles from multiple drives

```powershell
PS C:\> Get-OrchRole -Path Orch1:\,Orch2:\
```

Gets all roles from both Orch1 and Orch2 tenants.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExpandPermission

Expands the permissions assigned to each role into individual rows. Each row contains a permission name with its View, Edit, Create, and Delete settings. Required for CSV export.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExportCsv

Exports roles to the specified CSV file path. The CSV includes columns: Path, Name, Type, PermissionName, Scope, IsEditable, View, Edit, Create, Delete. Requires -ExpandPermission to be specified. Requires a filesystem path (not an Orch: drive path). If only a filename is specified, the default filename 'ExportedRoles.csv' is used.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the roles to retrieve. Supports wildcards. Tab completion dynamically suggests role names from the target drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe role names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Role

Returns Role objects when -ExpandPermission is not specified. Properties include Name, Type, IsEditable, and Permissions.

### UiPath.PowerShell.Entities.OrchRolePermissionExpanded

Returns OrchRolePermissionExpanded objects when -ExpandPermission is specified. Properties include Name, Type, PermissionName, Scope, View, Edit, Create, and Delete.

## NOTES

Roles are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

Results are ordered by Type (descending) and then by Name.

When -ExpandPermission is specified, Tenant-type roles exclude Folder-scoped permissions and Folder-type roles exclude Global-scoped permissions.

## RELATED LINKS

Set-OrchRole

Remove-OrchRole

Copy-OrchRole
