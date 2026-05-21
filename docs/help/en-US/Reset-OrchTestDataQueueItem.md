---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Reset-OrchTestDataQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Reset-OrchTestDataQueueItem
---

# Reset-OrchTestDataQueueItem

## SYNOPSIS

Resets the consumption state of items in test data queues.

## SYNTAX

### __AllParameterSets

```
Reset-OrchTestDataQueueItem [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [-Confirm] [-IsConsumed] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Resets the consumption state of all items in the specified test data queues. By default, all items are marked as not consumed, making them available for test execution again. When -IsConsumed is specified, all items are marked as consumed instead. Personal folders are excluded from processing.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test data queue names. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /api/TestDataQueueActions/SetAllItemsConsumed

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Write

Required permissions: TestDataQueueItems.Edit

## EXAMPLES

### Example 1: Reset all items to not consumed

```powershell
PS Orch1:\Shared> Reset-OrchTestDataQueueItem LoginTestData
```

Resets all items in the test data queue 'LoginTestData' to not consumed, making them available for test execution again.

### Example 2: Mark all items as consumed

```powershell
PS Orch1:\Shared> Reset-OrchTestDataQueueItem LoginTestData -IsConsumed
```

Marks all items in the test data queue 'LoginTestData' as consumed.

### Example 3: Reset items in all test data queues recursively

```powershell
PS Orch1:\> Reset-OrchTestDataQueueItem -Recurse *
```

Resets all items in all test data queues across the current folder and all its subfolders to not consumed.

### Example 4: Preview reset with WhatIf

```powershell
PS Orch1:\Shared> Reset-OrchTestDataQueueItem * -WhatIf
```

Shows what test data queues would have their items reset without actually performing the operation.

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

### -IsConsumed

Marks all items as consumed instead of resetting them to not consumed. When this switch is not specified, items are reset to not consumed (the default behavior).

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

Specifies the names of the test data queues whose items should be reset. Supports wildcards. Tab completion dynamically suggests test data queue names from the target folders.

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

You can pipe test data queue names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

The reset operation affects all items in each matching test data queue. There is no way to selectively reset individual items.

## RELATED LINKS

[Get-OrchTestDataQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueueItem.md)

[Get-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueue.md)
