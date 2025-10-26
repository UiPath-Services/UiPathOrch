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
PS Orch1:\Shared> Copy-OrchQueueItem TestQueue123 "Orch1:\Development" -WhatIf
```

Tests copying all "New" queue items from TestQueue123 in the current folder to TestQueue123 in the Development folder within the same tenant.

### Example 2
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem ProcessingQueue "Orch1:\Development"
```

Copies all "New" queue items from ProcessingQueue in the Shared folder to ProcessingQueue in the Development folder.

### Example 3
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem ProcessingQueue "Orch1:\Development" | Remove-OrchQueueItem
```

Moves all "New" queue items from ProcessingQueue in Shared to Development. The copied items are then removed from the source queue, creating a complete move operation.

### Example 4
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem Test* "Orch1:\Development" -WhatIf
```

Tests copying all "New" queue items from all queues starting with "Test" to corresponding queues in the Development folder.

### Example 5
```powershell
PS C:\> Copy-OrchQueueItem -Path "Orch1:\Shared" WorkQueue "Orch2:\Production"
```

Copies all "New" queue items from WorkQueue in Orch1 Shared folder to WorkQueue in Orch2 Production folder, demonstrating inter-tenant copying.

### Example 6
```powershell
PS Orch1:\> Copy-OrchQueueItem -Recurse ProcessingQueue "Orch2:" -WhatIf
```

Tests copying all "New" queue items from ProcessingQueue in all subfolders to corresponding queues in another tenant with the same folder structure.

### Example 7
```powershell
PS Orch1:\Shared> $copiedItems = Copy-OrchQueueItem BackupQueue "Orch1:\Archive"
PS Orch1:\Shared> $copiedItems | Remove-OrchQueueItem -Confirm:$false
```

Performs a controlled move operation by first copying items, storing the results, then removing the original items only if the copy was successful.

### Example 8
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MainQueue "Orch1:\Development" -Depth 2 -WhatIf
```

Tests copying queue items from MainQueue within 2 folder levels deep to the Development folder.

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
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider.

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
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem
## NOTES
- Only queue items with "New" status are copied to prevent duplication of processed work.
- The destination queue must have the same name as the source queue and must exist in the target folder.
- The cmdlet can be piped to Remove-OrchQueueItem to create move operations.
- Use -WhatIf to preview operations before execution, especially for bulk operations.
- Wildcard patterns enable efficient bulk copying across multiple queues.

## RELATED LINKS

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)

[Copy-OrchQueue](Copy-OrchQueue.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Export-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/export-csv)[Get-OrchQueueItem](Get-OrchQueueItem.md)
[Remove-OrchQueueItem](Remove-OrchQueueItem.md)
[Import-OrchQueueItem](Import-OrchQueueItem.md)
[Get-OrchQueue](Get-OrchQueue.md)
