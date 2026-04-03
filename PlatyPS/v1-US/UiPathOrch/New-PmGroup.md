---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-PmGroup
---

# New-PmGroup

## SYNOPSIS

Creates a new platform management group in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
New-PmGroup [-GroupName] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates one or more new groups at the organization (platform management) level in UiPath Automation Cloud. This cmdlet creates empty groups without members. To add members to a group, use Add-PmGroupMember after creating the group.

Multiple group names can be specified to create multiple groups in a single command. The cmdlet supports Ctrl+C cancellation during batch operations.

Tab completion for -GroupName suggests a unique new group name (e.g., "NewGroup1") that does not conflict with existing groups. Wildcard escape characters in the group name are unescaped before creating the group.

The -GroupName and -Path parameters support tab completion.

Primary Endpoint: POST /api/Group (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Create a new group

```powershell
PS Orch1:\> New-PmGroup NewGroup1
```

Creates a new group named "NewGroup1". Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Create multiple groups

```powershell
PS Orch1:\> New-PmGroup NewGroup1, NewGroup2, NewGroup3
```

Creates three new groups in a single command.

### Example 3: Create a group with confirmation

```powershell
PS Orch1:\> New-PmGroup "New Team" -Confirm
```

Prompts for confirmation before creating the group.

### Example 4: Preview group creation

```powershell
PS Orch1:\> New-PmGroup TestGroup -WhatIf
```

Shows what would happen without actually creating the group.

## PARAMETERS

### -Path

Specifies the target Pm: drives (organizations). If not specified, the current drive is targeted. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -GroupName

Specifies the name(s) of the group(s) to create. Multiple names can be specified as a comma-separated list. Tab completion suggests a unique new group name that does not conflict with existing groups. This parameter has the alias "Name".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- Name
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

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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
DefaultValue: ''
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

You can pipe group names to this cmdlet via the GroupName property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmGroup

Returns the created PmGroup object.

## NOTES

This cmdlet creates empty groups. To add members, use Add-PmGroupMember separately.

The cmdlet supports Ctrl+C cancellation when creating multiple groups.

## RELATED LINKS

Get-PmGroup

Copy-PmGroup

Remove-PmGroup

Add-PmGroupMember
