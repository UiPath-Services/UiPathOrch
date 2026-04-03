---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Move-PmGroupMember
---

# Move-PmGroupMember

## SYNOPSIS

Moves or copies members between platform management groups within the same UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Move-PmGroupMember [-GroupName] <string> [-UserName] <string[]> [-Destination] <string[]>
 [-KeepSource <string>] [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Moves members from a source group to one or more destination groups within the same organization at the platform management level. By default, the member is removed from the source group after being added to the destination group(s). When -KeepSource is set to "true", the member is copied (added to destination while remaining in the source group).

The cmdlet collects all additions and removals during ProcessRecord, then applies them in bulk during EndProcessing. Before applying, it performs the following optimizations:
- Members that appear in both the add and remove lists for the same group are cancelled out (no operation).
- Members already present in the destination group are excluded from the add list.
- Members already absent from the source group are excluded from the remove list.

The -GroupName, -UserName, -Destination, and -Path parameters support wildcards and tab completion. The -KeepSource parameter supports tab completion with "true" and "false".

Primary Endpoint: PUT /api/Group/{groupId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Move a member between groups

```powershell
PS Orch1:\> Move-PmGroupMember "Automation Developers" ytsuda@gmail.com Administrators
```

Moves "ytsuda@gmail.com" from the "Automation Developers" group to the "Administrators" group. Because -GroupName, -UserName, and -Destination are positional parameters (positions 0, 1, 2), parameter names can be omitted.

### Example 2: Copy a member to another group (keep in source)

```powershell
PS Orch1:\> Move-PmGroupMember "Automation Developers" ytsuda@gmail.com Administrators -KeepSource true
```

Copies "ytsuda@gmail.com" to the "Administrators" group while keeping them in the "Automation Developers" group.

### Example 3: Move all members to another group

```powershell
PS Orch1:\> Move-PmGroupMember "Automation Developers" * Administrators
```

Moves all members from "Automation Developers" to "Administrators".

### Example 4: Move members to multiple destination groups

```powershell
PS Orch1:\> Move-PmGroupMember "Automation Developers" ytsuda@gmail.com Administrators,Everyone
```

Moves "ytsuda@gmail.com" from "Automation Developers" to both "Administrators" and "Everyone" groups.

### Example 5: Preview a move operation

```powershell
PS Orch1:\> Move-PmGroupMember "Automation Developers" * Administrators -WhatIf
```

Shows which members would be moved without actually performing the operation.

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

### -Destination

Specifies the destination group name(s) to move or copy members to. Supports wildcards for pattern matching. Tab completion dynamically suggests group names.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -GroupName

Specifies the source group name to move members from. Supports wildcards. Tab completion dynamically suggests group names. This parameter has the alias "Name".

```yaml
Type: System.String
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

### -KeepSource

Specifies whether to keep the member in the source group. When set to "true", the operation becomes a copy instead of a move. Defaults to "false" (move). Tab completion suggests "true" and "false".

```yaml
Type: System.String
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

### -UserName

Specifies the name(s) of the member(s) to move. Supports wildcards for pattern matching. Tab completion suggests member names within the source group.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
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

You can pipe objects with GroupName, UserName, Destination, KeepSource, and Path properties to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. Group updates are performed as a side effect via the PUT API.

## NOTES

All additions and removals are collected during ProcessRecord and applied in bulk during EndProcessing. This ensures efficient API usage even with many pipeline inputs.

If the same source group and destination group are specified, the operation for that pair is skipped.

Members already present in the destination group are not re-added. Members already absent from the source group are not re-removed.

## RELATED LINKS

Add-PmGroupMember

Remove-PmGroupMember

Get-PmGroupMember
