---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmRobotAccount
---

# Remove-PmRobotAccount

## SYNOPSIS

Removes a robot account from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmRobotAccount [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more robot accounts at the organization (platform management) level from a UiPath Automation Cloud organization. The cmdlet calls the identity service delete API to permanently remove the robot account.

The deletion is performed single-threaded to avoid overwhelming the API.

The -Name and -Path parameters support tab completion.

Primary Endpoint: DELETE /api/RobotAccount/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific robot account

```powershell
PS Orch1:\> Remove-PmRobotAccount MyRobot
```

Removes the specified robot account from the organization. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Remove multiple robot accounts

```powershell
PS Orch1:\> Remove-PmRobotAccount Robot1,Robot2,Robot3
```

Removes the specified robot accounts from the organization.

### Example 3: Preview robot account removal without executing

```powershell
PS Orch1:\> Remove-PmRobotAccount MyRobot -WhatIf
```

Shows what would happen if the robot account were removed without actually deleting it.

### Example 4: Remove a robot account with confirmation

```powershell
PS Orch1:\> Remove-PmRobotAccount MyRobot -Confirm
```

Prompts for confirmation before removing the robot account.

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

### -Name

Specifies the name(s) of the robot account(s) to remove. Tab completion dynamically suggests robot account names from the target organizations.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

You can pipe robot account names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. The deletion is performed as a side effect.

## NOTES

The deletion is performed single-threaded. For large batch deletions, the operation may take some time.

## RELATED LINKS

Get-PmRobotAccount

Set-PmRobotAccount

Copy-PmRobotAccount
