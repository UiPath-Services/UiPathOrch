---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestDataQueueItem
---

# Get-OrchTestDataQueueItem

## SYNOPSIS

Gets items from test data queues in Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestDataQueueItem [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the items stored in test data queues from UiPath Orchestrator. Each item contains structured JSON data along with its consumption status. The -Name parameter specifies which test data queues to retrieve items from.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test data queue names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder and queue processing for improved performance. Personal folders are excluded from processing.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TestDataQueueItems?$filter=(TestDataQueueId eq {testDataQueueId})

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Read

Required permissions: TestDataQueueItems.View

## EXAMPLES

### Example 1: Get items from all test data queues

```powershell
PS Orch1:\root> Get-OrchTestDataQueueItem
```

Gets all items from all test data queues in the current folder.

### Example 2: Get items from a specific test data queue

```powershell
PS Orch1:\root> Get-OrchTestDataQueueItem テストキュー
```

Gets all items from the test data queue named 'テストキュー' in the current folder.

### Example 3: Get items from test data queues recursively

```powershell
PS Orch1:\> Get-OrchTestDataQueueItem -Recurse
```

Gets all items from all test data queues in the current folder and all its subfolders.

### Example 4: Get items from a specific folder

```powershell
PS C:\> Get-OrchTestDataQueueItem -Path Orch1:\root
```

Gets all items from all test data queues in the root folder on Orch1.

### Example 5: View items as a flat table with JSON columns

```powershell
PS Orch1:\root> Get-OrchTestDataQueueItem -Recurse | Format-OrchTestDataQueueItem
```

Pipes items to Format-OrchTestDataQueueItem, which groups by queue and renders each queue's ContentJson as a table with one column per top-level JSON property. Needed because Format-Table alone locks columns on the first object, so multi-queue output with different schemas would hide later keys.

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

Specifies the names of the test data queues to retrieve items from. Supports wildcards. Tab completion dynamically suggests test data queue names from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe test data queue names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestDataQueueItem

Returns TestDataQueueItem objects with properties including Id, IsConsumed, and ContentJson containing the structured test data.

## NOTES

The cmdlet uses multi-threaded processing at both the folder and queue levels for improved performance.

Pipe to Format-OrchTestDataQueueItem for a table view that groups by queue and flattens each item's ContentJson into columns.

## RELATED LINKS

Get-OrchTestDataQueue

Format-OrchTestDataQueueItem

Reset-OrchTestDataQueueItem
