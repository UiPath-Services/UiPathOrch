---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Redo-OrchQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Redo-OrchQueueItem
---

# Redo-OrchQueueItem

## SYNOPSIS

Retries failed queue items.

## SYNTAX

### __AllParameterSets

```
Redo-OrchQueueItem [-Path <string[]>] [-Name] <string[]> [-Id] <long[]> [-Confirm]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retries failed queue items in a UiPath Orchestrator queue. Only items with Status "Failed" and a ReviewStatus that is not "Retried" or "Verified" are eligible for retry. The cmdlet accumulates all specified item IDs during processing and submits them as a single batch request in the end-processing phase.

A rate limit of 600 milliseconds is enforced between API calls. The cmdlet retrieves up to 1000 retryable items per queue.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which queue items would be retried, or -Confirm to be prompted before each retry operation.

The -Name, -Id, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual queue names, and the -Id completion shows items with Status "Failed" whose ReviewStatus is not "Retried" or "Verified".

Primary Endpoint: POST /odata/Queues/UiPathODataSvr.BulkRetryQueueItems

OAuth required scopes: OR.Queues

Required permissions: Queues.Edit, Transactions.Edit

## EXAMPLES

### Example 1: Retry a specific failed queue item

```powershell
PS Orch1:\Shared> Redo-OrchQueueItem TestQueue2 48291
```

Retries the failed queue item with ID 48291 in TestQueue2 in the current folder (Shared).

### Example 2: Preview retry with -WhatIf

```powershell
PS Orch1:\Shared> Redo-OrchQueueItem TestQueue2 55012,55013 -WhatIf
```

```output
What if: Performing the operation "Redo QueueItem" on target "Queue: 'TestQueue2 [Shared]' Id: '55012, 55013'".
```

Shows what would happen without actually retrying the items.

### Example 3: Retry failed items from a specific folder

```powershell
PS C:\> Redo-OrchQueueItem -Path Orch1:\Shared TestQueue2 -Id 70345
```

Retries the failed queue item with ID 70345 in TestQueue2 located in the Shared folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folder containing the queue. If not specified, the current folder is used. Supports wildcards.

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

### -Id

Specifies the IDs of the queue items to retry. Only items with Status "Failed" and ReviewStatus not "Retried" or "Verified" can be retried. Tab completion dynamically suggests eligible item IDs. Multiple IDs can be specified as a comma-separated list.

```yaml
Type: System.Int64[]
DefaultValue: None
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

Specifies the names of the queues containing the items to retry. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the target folders.

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

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe queue names to this cmdlet via the Name property.

### System.Int64[]

You can pipe queue item IDs to this cmdlet via the Id property.

## OUTPUTS

### UiPath.PowerShell.Entities.BulkOperationResponse

Returns a BulkOperationResponse object containing the result of the bulk retry operation, including the count of successfully retried items and any failures.

## NOTES

Only queue items with Status "Failed" and a ReviewStatus that is not "Retried" or "Verified" are eligible for retry. Attempting to retry items that do not meet these criteria will result in those items being skipped.

The cmdlet uses two-phase processing: item IDs are accumulated during the process phase and submitted as a single batch request during the end-processing phase. This approach minimizes API calls.

A rate limit of 600 milliseconds is enforced between API calls. Up to 1000 retryable items can be retrieved per queue.

Queue items are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the target folder.

## RELATED LINKS

[Get-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueItem.md)

[Remove-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueItem.md)
