---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchRole.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchRole
---

# Set-OrchRole

## SYNOPSIS

Creates or updates a role.

## SYNTAX

### __AllParameterSets

```
Set-OrchRole [-Path <string[]>] [-Name] <string[]> [-Confirm] [-Create <string>]
 [-Delete <string>] [-Edit <string>] [-ExpandPermission] -PermissionName <string>
 [-Scope <string>] [-Type <string>] [-View <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates or updates roles in UiPath Orchestrator. If a role with the specified name already exists, its permissions are updated. If no matching role exists, a new role is created.

This cmdlet is designed to work with CSV import from Get-OrchRole -ExpandPermission -ExportCsv. When importing to a different tenant, modify the Path column in the CSV or use the -Path parameter to override the destination.

All parameters support ValueFromPipelineByPropertyName, enabling CSV import via Import-Csv | Set-OrchRole. The cmdlet collects all permission settings during pipeline processing and applies them as batch operations in EndProcessing using deep-copy to prevent unintended modifications.

The -View, -Edit, -Create, and -Delete parameters accept 'true' or 'false'. Tab completion suggests these values.

Roles are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: POST /odata/Roles (new) or PUT /odata/Roles({roleId}) (update)

OAuth required scopes: OR.Users or OR.Users.Write

Required permissions: Roles.View, Roles.Create (new) or Roles.Edit (update)

## EXAMPLES

### Example 1: Import roles from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\roles.csv | Set-OrchRole
```

Imports roles and permissions from a CSV file previously exported with Get-OrchRole -ExpandPermission -ExportCsv. The Path column in the CSV determines the target drive.

### Example 2: Import roles to a different tenant

```powershell
PS C:\> Import-Csv C:\temp\roles.csv | Set-OrchRole -Path Orch2:\
```

Imports roles from a CSV file to Orch2, overriding the Path column values.

### Example 3: Create a new role with specific permissions

```powershell
PS Orch1:\> Set-OrchRole NewRole -Type Folder -PermissionName Robots -View true -Edit true
```

Creates a new folder-scoped role named 'NewRole' with Robots.View and Robots.Edit permissions enabled.

### Example 4: Update an existing role's permissions

```powershell
PS Orch1:\> Set-OrchRole MyRole -PermissionName Assets -View true -Edit false
```

Updates the 'MyRole' role to enable Assets.View and disable Assets.Edit.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted. When specified, overrides Path values from pipeline input (CSV import).

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

### -Create

Specifies whether to grant or deny the Create action for the permission. Tab completion suggests 'true' or 'false'.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Delete

Specifies whether to grant or deny the Delete action for the permission. Tab completion suggests 'true' or 'false'.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Edit

Specifies whether to grant or deny the Edit action for the permission. Tab completion suggests 'true' or 'false'.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExpandPermission

Reserved for future use.

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

### -Name

Specifies the names of the roles to create or update. Supports wildcards (for updating existing roles). Tab completion dynamically suggests role names from the target drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PermissionName

Specifies the permission name to configure (e.g., 'Robots', 'Assets', 'Packages'). The View, Edit, Create, and Delete actions are appended automatically. This parameter is mandatory.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Scope

Specifies the permission scope (e.g., 'Global', 'Folder'). Used when creating new roles.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

Specifies the role type: 'Tenant', 'Folder', or 'Mixed'. Used when creating new roles. When updating, the type must match the existing role.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -View

Specifies whether to grant or deny the View action for the permission. Tab completion suggests 'true' or 'false'.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe role properties to this cmdlet. All parameters support ValueFromPipelineByPropertyName for CSV import.

### System.String[]

You can pipe role names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Role

Returns a Role object when a new role is created. No output when updating existing roles.

## NOTES

Roles are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

When updating existing roles, the cmdlet uses deep-copy to prevent unintended modifications to cached data.

If a permission name is specified multiple times for the same role, a warning is issued and only the first occurrence is used.

If the -Type does not match the existing role's type, the update is skipped with a warning.

## RELATED LINKS

[Get-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchRole.md)

[Remove-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchRole.md)

[Copy-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchRole.md)
