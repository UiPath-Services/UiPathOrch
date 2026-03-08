---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchTestSetSchedule
---

# Enable-OrchTestSetSchedule

## SYNOPSIS

Enables test set schedules in Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Enable-OrchTestSetSchedule [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Enables disabled test set schedules in UiPath Orchestrator folders. Once enabled, the test set schedules will resume executing according to their configured triggers.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test set schedule names. Tab completion shows only currently disabled test set schedules. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.TestSetSchedules

Required permissions: TestSetSchedules.Edit

## EXAMPLES

### Example 1: Enable a test set schedule

```powershell
PS Orch1:\Shared> Enable-OrchTestSetSchedule Sample-Test-Schedule
```

Enables the test set schedule named 'Sample-Test-Schedule' in the current folder.

### Example 2: Enable test set schedules using wildcards

```powershell
PS Orch1:\Shared> Enable-OrchTestSetSchedule TestSchedule*
```

Enables all test set schedules whose names match 'TestSchedule*' in the current folder.

### Example 3: Enable test set schedules recursively

```powershell
PS Orch1:\> Enable-OrchTestSetSchedule -Recurse *
```

Enables all disabled test set schedules in the current folder and all its subfolders.

### Example 4: Preview enable with WhatIf

```powershell
PS Orch1:\Shared> Enable-OrchTestSetSchedule * -WhatIf
```

Shows what test set schedules would be enabled without actually performing the operation.

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

Specifies the names of the test set schedules to enable. Supports wildcards. Tab completion dynamically suggests currently disabled test set schedule names from the target folders.

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

Tab completion for the -Name parameter shows only currently disabled test set schedules, making it easy to identify which schedules can be enabled.

## RELATED LINKS

Disable-OrchTestSetSchedule

Get-OrchTestSetSchedule
