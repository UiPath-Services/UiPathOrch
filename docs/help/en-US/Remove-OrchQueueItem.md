---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchQueueItem
---

# Remove-OrchQueueItem

## SYNOPSIS

Deletes queue items from a queue.

## SYNTAX

### __AllParameterSets

```
Remove-OrchQueueItem [-Path <string>] [-Name] <string> [-Id] <long[]>
 [[-RowVersion] <string>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Deletes queue items from a UiPath Orchestrator queue. The cmdlet accumulates all specified item IDs during processing and submits them as a single batch delete request in the end-processing phase, with a batch size of up to 1000 items per API call.

Each queue item requires a RowVersion value for deletion. If the -RowVersion parameter is not specified, the cmdlet first checks the local cache for the RowVersion and, if not found, retrieves it from the Orchestrator API. Progress tracking is displayed during row version resolution and deletion.

If some items in a batch fail to delete, the cmdlet continues processing and returns the failed QueueItem objects as output. Successfully deleted items produce no output.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which queue items would be deleted, or -Confirm to be prompted before each delete operation.

The -Name, -Id, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual queue names, and the -Id completion shows items from the cache with Status not equal to "Deleted".

Primary Endpoint: POST /odata/Queues/UiPathODataSvr.BulkDeleteQueueItems

OAuth required scopes: OR.Queues

Required permissions: Queues.Edit, Transactions.Delete

## EXAMPLES

### Example 1: Remove specific queue items

```powershell
PS Orch1:\Shared> Remove-OrchQueueItem InvoiceQueue 48291,48292
```

Deletes the queue items with IDs 48291 and 48292 from the InvoiceQueue in the current folder (Shared).

### Example 2: Remove a queue item with an explicit RowVersion

```powershell
PS Orch1:\Shared> Remove-OrchQueueItem InvoiceQueue 48291 "AAAAAAAAB9k="
```

Deletes the queue item with ID 48291 from the InvoiceQueue, using the specified RowVersion value. This avoids the additional API call to look up the RowVersion.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchQueueItem OrderQueue 55012 -WhatIf
```

```output
What if: Performing the operation "Remove QueueItem" on target "Queue: 'OrderQueue [Shared]' Id: '55012'".
```

Shows what would happen without actually deleting any items.

### Example 4: Remove queue items from a specific folder

```powershell
PS C:\> Remove-OrchQueueItem -Path Orch1:\Shared TestQueue2 -Id 70345,70346,70347
```

Deletes the queue items with IDs 70345, 70346, and 70347 from TestQueue2 in the Shared folder on Orch1.

## PARAMETERS

### -Path

Specifies the folder containing the queue. If not specified, the current folder is used. Supports wildcards.

```yaml
Type: System.String
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

Specifies the IDs of the queue items to delete. Tab completion dynamically suggests item IDs from the cache with Status not equal to "Deleted". Multiple IDs can be specified as a comma-separated list.

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

Specifies the name of the queue containing the items to delete. Supports wildcards. Tab completion dynamically suggests queue names from the target folder.

```yaml
Type: System.String
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

### -RowVersion

Specifies the RowVersion (concurrency token) of the queue items to delete. If not specified, the cmdlet resolves the RowVersion automatically by checking the local cache first, then querying the Orchestrator API.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
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

### System.String

You can pipe a queue name to this cmdlet via the Name property.

### System.Int64[]

You can pipe queue item IDs to this cmdlet via the Id property.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem

Returns QueueItem objects only for items that failed to delete. If all items are deleted successfully, no output is returned. This allows you to pipe failed items to subsequent retry or investigation commands.

## NOTES

The cmdlet uses two-phase processing: item IDs are accumulated during the process phase and submitted as a single batch delete request during the end-processing phase. The batch size is up to 1000 items per API call.

Each queue item requires a RowVersion (concurrency token) for deletion. The cmdlet resolves RowVersion values in the following order: (1) the -RowVersion parameter if specified, (2) the local cache, (3) the Orchestrator API. Progress bars are displayed during row version resolution and deletion.

If some items fail to delete (e.g., due to a stale RowVersion or concurrent modification), the cmdlet outputs the failed QueueItem objects and continues processing the remaining items.

Queue items are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the target folder.

## RELATED LINKS

[Get-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueItem.md)

[Redo-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Redo-OrchQueueItem.md)
