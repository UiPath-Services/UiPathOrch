---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Format-OrchQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/24/2026
PlatyPS schema version: 2024-05-01
title: Format-OrchQueueItem
---

# Format-OrchQueueItem

## SYNOPSIS

Formats Get-OrchQueueItem output as one table per queue, flattening SpecificContent keys into columns.

## SYNTAX

### __AllParameterSets

```
Format-OrchQueueItem [[-InputObject] <Object>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Groups piped queue items by QueueDefinitionId and renders one Format-Table per queue. Each row uses the QueueItem's Expanded property: leading columns for the standard fields (Id, Reference, Status, Priority, DeferDate, DueDate, StartProcessing, EndProcessing) followed by the keys of SpecificContent.

Because Format-Table locks its column set on the first object seen, rendering items from multiple queues with a single Format-Table silently hides keys that aren't present in the first queue's schema. Grouping by QueueDefinitionId first avoids that problem.

The companion mechanism is the Expanded ScriptProperty added to UiPath.PowerShell.Entities.QueueItem via Update-TypeData. When you only need a single queue's items, `| ForEach-Object Expanded | Format-Table` is sufficient; this function is the convenience wrapper for mixed-queue output.

## EXAMPLES

### Example 1: Format a single queue's items

```powershell
PS Orch1:\Shared> Get-OrchQueueItem -Name OrderQueue -First 20 | ForEach-Object Expanded | Format-Table
```

For a single queue the Expanded ScriptProperty alone is enough — Format-Table sees a consistent schema and all columns align.

### Example 2: Format across multiple queues

```powershell
PS Orch1:\Shared> Get-OrchQueueItem -Name 'Order*' -Status New | Format-OrchQueueItem
```

Emits one Format-Table block per queue. Each block uses that queue's specific columns (OrderId, Customer, etc. for OrderQueue; a different set for a different queue) without leaking schemas across queues.

### Example 3: Combine with wildcards and filters

```powershell
PS Orch1:\> Get-OrchQueueItem -Path Orch1:\ -Recurse -Name '*' -Status Failed | Format-OrchQueueItem
```

Surveys all failed items across every queue in every folder, grouped per queue.

## PARAMETERS

### -InputObject

A UiPath.PowerShell.Entities.QueueItem flowing in from the pipeline (typically from Get-OrchQueueItem). Not intended to be bound positionally.

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

### UiPath.PowerShell.Entities.QueueItem

QueueItems from Get-OrchQueueItem.

## OUTPUTS

### Microsoft.PowerShell.Commands.Internal.Format.FormatStartData

### Microsoft.PowerShell.Commands.Internal.Format.GroupStartData

### Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData

### Microsoft.PowerShell.Commands.Internal.Format.GroupEndData

### Microsoft.PowerShell.Commands.Internal.Format.FormatEndData

Returns Format-Table formatting records, one logical table per queue.

## NOTES

Loads an ETS ScriptProperty named Expanded onto UiPath.PowerShell.Entities.QueueItem at module import time (see UiPathOrch.psm1). The Expanded property is itself usable directly when you only need one queue's worth of items.

## RELATED LINKS

[Get-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueItem.md)

Get-OrchTestDataQueueItemTable
