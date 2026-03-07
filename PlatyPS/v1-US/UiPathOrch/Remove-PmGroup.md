---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmGroup
---

# Remove-PmGroup

## SYNOPSIS

Removes a platform management group from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmGroup [-GroupName] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more groups at the organization (platform management) level from a UiPath Automation Cloud organization. The cmdlet calls the identity service delete API to permanently remove the group.

The -GroupName parameter supports wildcards, allowing batch deletion of groups matching a pattern. The cmdlet supports Ctrl+C cancellation during batch operations.

After deletion, the group cache and directory search caches are cleared.

The -GroupName and -Path parameters support tab completion.

Primary Endpoint: DELETE /api/Group/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific group

```powershell
PS Orch1:\> Remove-PmGroup TestGroup
```

Removes the group named "TestGroup". Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Remove multiple groups with a wildcard

```powershell
PS Orch1:\> Remove-PmGroup Test*
```

Removes all groups whose names start with "Test".

### Example 3: Remove a group with confirmation

```powershell
PS Orch1:\> Remove-PmGroup "Production Team" -Confirm
```

Prompts for confirmation before removing the group.

### Example 4: Preview group removal

```powershell
PS Orch1:\> Remove-PmGroup Dev* -WhatIf
```

Shows which groups would be removed without actually deleting them.

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

Specifies the name(s) of the group(s) to remove. Supports wildcards for pattern matching. Tab completion dynamically suggests group names. This parameter has the alias "Name".

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

### System.Object

This cmdlet does not produce output. The deletion is performed as a side effect.

## NOTES

The cmdlet supports Ctrl+C cancellation during batch operations.

After a group is deleted, the group cache and directory search caches are cleared to ensure subsequent commands reflect the current state.

## RELATED LINKS

Get-PmGroup

New-PmGroup

Copy-PmGroup
