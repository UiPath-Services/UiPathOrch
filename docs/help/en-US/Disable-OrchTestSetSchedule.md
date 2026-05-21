---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTestSetSchedule.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Disable-OrchTestSetSchedule
---

# Disable-OrchTestSetSchedule

## SYNOPSIS

Disables test set schedules in Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Disable-OrchTestSetSchedule [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-Name] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Disables enabled test set schedules in UiPath Orchestrator folders. Once disabled, the test set schedules will stop executing until they are re-enabled.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test set schedule names. Tab completion shows only currently enabled test set schedules. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Write

Required permissions: TestSetSchedules.Edit

## EXAMPLES

### Example 1: Disable a test set schedule

```powershell
PS Orch1:\Shared> Disable-OrchTestSetSchedule Sample-Test-Schedule
```

Disables the test set schedule named 'Sample-Test-Schedule' in the current folder.

### Example 2: Disable test set schedules using wildcards

```powershell
PS Orch1:\Shared> Disable-OrchTestSetSchedule TestSchedule*
```

Disables all test set schedules whose names match 'TestSchedule*' in the current folder.

### Example 3: Disable test set schedules recursively

```powershell
PS Orch1:\> Disable-OrchTestSetSchedule -Recurse *
```

Disables all enabled test set schedules in the current folder and all its subfolders.

### Example 4: Preview disable with WhatIf

```powershell
PS Orch1:\Shared> Disable-OrchTestSetSchedule * -WhatIf
```

Shows what test set schedules would be disabled without actually performing the operation.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
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

Specifies the names of the test set schedules to disable. Supports wildcards. Tab completion dynamically suggests currently enabled test set schedule names from the target folders.

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

You can pipe test set schedule names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Tab completion for the -Name parameter shows only currently enabled test set schedules, making it easy to identify which schedules can be disabled.

## RELATED LINKS

[Enable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTestSetSchedule.md)

[Get-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetSchedule.md)
