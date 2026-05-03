---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchRole.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchRole
---

# Remove-OrchRole

## SYNOPSIS

Removes roles from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchRole [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes roles from UiPath Orchestrator. Static (built-in) roles cannot be removed and are automatically excluded from the operation.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available role names. Multiple values can be specified using comma-separated text that includes wildcards.

Roles are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: DELETE /odata/Roles({roleId})

OAuth required scopes: OR.Users

Required permissions: Roles.Delete

## EXAMPLES

### Example 1: Remove a role

```powershell
PS Orch1:\> Remove-OrchRole MyCustomRole
```

Removes the role named 'MyCustomRole' from the current tenant.

### Example 2: Remove roles using wildcards

```powershell
PS Orch1:\> Remove-OrchRole Test*
```

Removes all non-static roles whose names match 'Test*'.

### Example 3: Remove a role from a specific drive

```powershell
PS C:\> Remove-OrchRole -Path Orch1:\ OldRole
```

Removes the role 'OldRole' from the Orch1 tenant.

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

### -Name

Specifies the names of the roles to remove. Supports wildcards. Tab completion dynamically suggests role names from the target drives. Static (built-in) roles are automatically excluded.

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

### System.String[]

You can pipe role names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Roles are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

Static (built-in) roles cannot be removed and are automatically excluded from wildcard matching.

## RELATED LINKS

Get-OrchRole

Set-OrchRole

Copy-OrchRole
