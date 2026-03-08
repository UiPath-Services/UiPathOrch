---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-PmRobotAccount
---

# Copy-PmRobotAccount

## SYNOPSIS

Copies robot accounts from one UiPath Automation Cloud organization to another.

## SYNTAX

### __AllParameterSets

```
Copy-PmRobotAccount [-Name] <string[]> [-Destination] <string[]> [-Path <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies robot accounts at the organization (platform management) level from a source organization to one or more destination organizations. The cmdlet replicates robot account properties and group memberships.

Robot accounts are created in the destination with their group mappings preserved. Groups are matched by name (case-insensitive) in the destination, and created automatically if they do not exist.

The cmdlet prevents copying between drives that belong to the same organization (same partitionGlobalId). If the source and destination belong to the same partition, the operation is skipped.

The -Name parameter supports tab completion. The -Destination and -Path parameters support tab completion for available drives.

Primary Endpoint: POST /api/RobotAccount (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Copy all robot accounts to another organization

```powershell
PS Orch1:\> Copy-PmRobotAccount * Orch2:
```

Copies all robot accounts from the Orch1 organization to the Orch2 organization, preserving group memberships. Because -Name and -Destination are positional parameters, the parameter names can be omitted.

### Example 2: Copy a specific robot account across drives

```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: MyRobot1 Orch2:
```

Copies the specified robot account from the Orch1 organization to the Orch2 organization.

### Example 3: Copy robot accounts to multiple destinations

```powershell
PS Orch1:\> Copy-PmRobotAccount * Orch2:,Orch3:
```

Copies all robot accounts from the Orch1 organization to both the Orch2 and Orch3 organizations.

### Example 4: Preview a cross-organization copy

```powershell
PS Orch1:\> Copy-PmRobotAccount * Orch2: -WhatIf
```

Shows which robot accounts would be copied without performing the operation.

## PARAMETERS

### -Path

Specifies the source Pm: drive (organization) to copy robot accounts from. If not specified, the current drive is used. Tab completion suggests available drive names.

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

### -Destination

Specifies the destination Pm: drive(s) (organizations) to copy robot accounts to. Multiple destinations can be specified. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

### -Name

Specifies the name(s) of the robot account(s) to copy. Tab completion dynamically suggests robot account names from the source organization.

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

This cmdlet does not produce pipeline output. The copy operation is performed as a side effect.

## NOTES

The source and destination drives must belong to different organizations (different partitionGlobalId). If they belong to the same organization, the operation is skipped.

Group memberships are preserved during the copy. Groups are matched by name (case-insensitive) in the destination, and created automatically if they do not exist.

## RELATED LINKS

Get-PmRobotAccount

Set-PmRobotAccount

Remove-PmRobotAccount
