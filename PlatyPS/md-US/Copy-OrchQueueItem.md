---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchQueueItem

## SYNOPSIS
Copies queue items with a status of New to a queue with the same name located elsewhere.

## SYNTAX

```
Copy-OrchQueueItem [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchQueueItem cmdlet copies queue items with a status of "New" from source queues to destination queues with the same name in different folders within UiPath Orchestrator tenants or across different tenants. This cmdlet moves pending work items between environments while preserving their data and priority.

The cmdlet only copies queue items with "New" status, ensuring that work that has already been processed or is currently being processed is not duplicated. The destination queue must have the same name as the source queue and must exist in the target folder.

The cmdlet outputs the items that were successfully copied. This output can be piped to Remove-OrchQueueItem to remove those items from the source queue, creating a complete move operation and preventing duplicate transaction processing. This pattern is essential for queue item migration workflows.

Use the -Name parameter to specify which queues' items to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying items from multiple queues efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying queue items from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/QueueDefinitions, GET /odata/QueueItems, POST /odata/Queues/UiPathODataSvc.AddQueueItem

OAuth required scopes: OR.Queues

Required permissions: Queues.View, Transactions.View, Transactions.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst
```

Copies all "New" queue items from MyQueue in the current folder (\Shared) to MyQueue in the \dst folder within the same tenant.

### Example 2
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem | Export-Csv c:\itemsToRemove.csv -Encoding utf8BOM
```

Moves all "New" queue items from MyQueue in \Shared to MyQueue in \dst. The copied items are then removed from the source queue, and any items that failed to be removed are exported to CSV for retry later.

### Example 3
```powershell
PS Orch1:\> Copy-OrchQueueItem -Recurse * Orch2:\ | Remove-OrchQueueItem | Export-Csv c:\itemsToRemove.csv -Encoding utf8BOM
```

Moves all "New" queue items from all queues in one tenant to corresponding queues in another tenant with the same folder structure. Failed removals are exported to CSV.

### Example 4
```powershell
PS C:\> Copy-OrchQueueItem -Path Orch1:\Development ProcessingQueue Orch2:\Production
```

Copies all "New" queue items from ProcessingQueue in Orch1:\Development to ProcessingQueue in Orch2:\Production, demonstrating inter-tenant queue item copying.

### Example 5
```powershell
PS Orch1:\Development> Copy-OrchQueueItem *Invoice*, *Report* Orch1:\Production -WhatIf
```

Shows what would happen when copying "New" queue items from queues with names containing Invoice or Report from the current folder to the Production folder.

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

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Destination
Specifies the destination folder where queue items should be copied.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the queues whose items should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the source folder. If not specified, the current folder will be used as the source.

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

### -Recurse
Specifies that queue items should be copied from all subfolders recursively, maintaining the folder structure in the destination.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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

### System.String[]
Queue names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Queue
Queue objects from Get-OrchQueue can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem
Returns the queue items that were successfully copied. These objects can be piped to Remove-OrchQueueItem to complete the move operation.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This cmdlet only copies queue items with "New" status. The destination queue must exist and have the same name as the source queue. Use Copy-OrchQueue to copy queue definitions first if needed.

**Important**: The cmdlet outputs successfully copied items that can be piped to Remove-OrchQueueItem to complete a move operation. This prevents duplicate transaction processing. Use Export-Csv to save failed removal items for retry later. The exported CSV can be re-imported using Import-Csv and piped to Remove-OrchQueueItem to retry deletion of failed items.

**Complete Queue Migration Workflow**:
1. Copy queue definitions: `Copy-OrchQueue * \destination`
2. Move queue items: `Copy-OrchQueueItem * \destination | Remove-OrchQueueItem | Export-Csv failedRemovals.csv`

Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)

[Copy-OrchQueue](Copy-OrchQueue.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Export-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/export-csv)
