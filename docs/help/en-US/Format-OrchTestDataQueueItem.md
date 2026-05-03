---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Format-OrchTestDataQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/24/2026
PlatyPS schema version: 2024-05-01
title: Format-OrchTestDataQueueItem
---

# Format-OrchTestDataQueueItem

## SYNOPSIS

Formats Get-OrchTestDataQueueItem output as one table per test data queue, flattening ContentJson keys into columns.

## SYNTAX

### __AllParameterSets

```
Format-OrchTestDataQueueItem [[-InputObject] <Object>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Groups piped test data queue items by their containing queue (via Path, falling back to TestDataQueueId) and renders one Format-Table per queue. Each item's ContentJson (stored as a JSON string) is parsed and its top-level properties are promoted to columns, preceded by Id and IsConsumed.

Because Format-Table locks its column set on the first object seen, rendering items from multiple test data queues with a single Format-Table silently hides keys unique to later queues. Grouping by queue first avoids that problem.

Replaces the deprecated Get-OrchTestDataQueueItemTable function. This version is a proper Format-* filter that composes in pipelines, unlike the old Get-* function which performed its own retrieval and Format-Table internally.

## EXAMPLES

### Example 1: Format test data queue items from the current folder

```powershell
PS Orch1:\root> Get-OrchTestDataQueueItem | Format-OrchTestDataQueueItem
```

Fetches test data queue items from the current folder and pipes them through the formatter, one table per queue.

### Example 2: Format recursively across subfolders

```powershell
PS Orch1:\root> Get-OrchTestDataQueueItem -Recurse | Format-OrchTestDataQueueItem
```

Walks all subfolders, collects test data queue items across queues, and emits one table per queue. Handles heterogeneous schemas across queues correctly.

### Example 3: Format from a specific folder path

```powershell
PS C:\> Get-OrchTestDataQueueItem -Path Orch1:\root -Recurse | Format-OrchTestDataQueueItem
```

Absolute-path form, runnable from any location.

## PARAMETERS

### -InputObject

A UiPath.PowerShell.Entities.TestDataQueueItem flowing in from the pipeline (typically from Get-OrchTestDataQueueItem). Not intended to be bound positionally.

```yaml
Type: System.Object
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: true
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

### UiPath.PowerShell.Entities.TestDataQueueItem

TestDataQueueItems from Get-OrchTestDataQueueItem.

## OUTPUTS

### Microsoft.PowerShell.Commands.Internal.Format.FormatStartData

### Microsoft.PowerShell.Commands.Internal.Format.GroupStartData

### Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData

### Microsoft.PowerShell.Commands.Internal.Format.GroupEndData

### Microsoft.PowerShell.Commands.Internal.Format.FormatEndData

Returns Format-Table formatting records, one logical table per queue.

## NOTES

Items with malformed ContentJson are rendered with a single ContentJson column containing the raw string, rather than failing the whole pipeline.

## RELATED LINKS

[Get-OrchTestDataQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueueItem.md)

[Get-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueue.md)

[Reset-OrchTestDataQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Reset-OrchTestDataQueueItem.md)

[Format-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Format-OrchQueueItem.md)
