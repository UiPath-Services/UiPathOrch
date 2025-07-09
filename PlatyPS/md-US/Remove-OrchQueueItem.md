---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchQueueItem

## SYNOPSIS
Removes queue items.

## SYNTAX

```
Remove-OrchQueueItem [-Name] <String> [-Id] <Int64[]> [[-RowVersion] <String>] [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Remove-OrchQueueItem cmdlet removes specific queue items by their IDs from queues within UiPath Orchestrator. This cmdlet is designed to work both independently and as part of queue item migration workflows.

Specify the queue name and the IDs of the items you want to delete. If you also provide the RowVersion, the deletion process will be faster. When RowVersion is not specified, it will be retrieved from the cache for the given ID. If the cache does not contain the RowVersion, the cmdlet will automatically call the GetItemById API to obtain it.

The cmdlet outputs any items that failed to be removed. These failed items can be exported to a CSV file using Export-Csv and later re-imported with Import-Csv to retry deletion. This provides resilient error handling for large-scale queue item management operations.

You can easily remove items that were successfully copied by piping the output of Copy-OrchQueueItem into Remove-OrchQueueItem. This creates a complete move operation that prevents duplicate transaction processing.

Primary Endpoint: GET /odata/QueueItems({queueItemId}), POST /odata/QueueItems/UiPathODataSvc.DeleteBulk

OAuth required scopes: OR.Queues

Required permissions: Queues.View and Transactions.Delete

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Remove-OrchQueueItem MyQueue 12345, 12346, 12347
```

Removes queue items with IDs 12345, 12346, and 12347 from MyQueue in the current folder.

### Example 2
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem
```

Copies queue items from MyQueue to destination and removes the successfully copied items from the source queue, completing a move operation.

### Example 3
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem | Export-Csv c:\failedRemovals.csv -Encoding utf8BOM
```

Moves queue items and exports any items that failed to be removed to a CSV file for later retry.

### Example 4
```powershell
PS C:\> Import-Csv c:\failedRemovals.csv | Remove-OrchQueueItem
```

Re-imports previously failed removal items from CSV and retries their deletion.

### Example 5
```powershell
PS Orch1:\Production> Remove-OrchQueueItem ProcessingQueue 98765 "ABC123-RowVersion" -WhatIf
```

Shows what would happen when removing a specific queue item with ID 98765 and RowVersion "ABC123-RowVersion" from ProcessingQueue.

### Example 6
```powershell
PS C:\> Remove-OrchQueueItem -Path Orch1:\Development TestQueue 11111, 22222 -Confirm
```

Removes queue items with IDs 11111 and 22222 from TestQueue in the Development folder with confirmation prompts.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
Specifies the IDs of the queue items to be removed.

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the name of the queue containing the items to be removed.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the source folder containing the queue. If not specified, the current folder will be used.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -RowVersion
Specifies the RowVersion of the queue items for faster deletion. If not provided, it will be retrieved automatically.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
### System.Int64[]
## OUTPUTS

### System.Object
## NOTES
This cmdlet can be used independently by specifying queue name and item IDs, or as part of a pipeline with Copy-OrchQueueItem for move operations.

**Performance**: Providing RowVersion accelerates deletion by avoiding additional API calls to retrieve version information.

**Error Handling**: Failed removals are returned as output and can be exported to CSV for retry later. Use Import-Csv to reload failed items and pipe them back to Remove-OrchQueueItem.

**Move Operation Pattern**:
1. `Copy-OrchQueueItem SourceQueue Destination | Remove-OrchQueueItem | Export-Csv failedRemovals.csv`
2. `Import-Csv failedRemovals.csv | Remove-OrchQueueItem` (retry failures)

Use -WhatIf to preview deletions before executing, especially for bulk operations.

## RELATED LINKS

[Copy-OrchQueueItem](Copy-OrchQueueItem.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Add-OrchQueueItem](Add-OrchQueueItem.md)

[Export-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/export-csv)

[Import-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/import-csv)
