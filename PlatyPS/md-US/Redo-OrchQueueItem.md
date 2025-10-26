---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Redo-OrchQueueItem

## SYNOPSIS
Retries failed transaction items in the specified queues.

## SYNTAX

```
Redo-OrchQueueItem [-Name] <String[]> [-Id] <Int64[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet retries transaction items by specifying the queue name and the item IDs. It retries only retryable items, defined as those with a Status of Failed and a Revision of either None or InReview.

Primary Endpoint: POST /odata/QueueItems/UiPathODataSvc.SetItemReviewStatus

OAuth required scopes: OR.Queues

Required permissions: Queues.View and Transactions.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Redo-OrchQueueItem YourQueueName <item IDs>
```

Retries the specified items in the queue. The -Id parameter, which specifies <item IDs>, supports auto-completion.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose
```

Retries all failed items in the specified queue, YourQueueName.
The -Verbose parameter displays the IDs of the retried items.

Please note that the Get-OrchQueueItem cmdlet can retrieve up to a maximum of 1000 items at a time.
If there are more than 1,000 retriable items in the queue, repeat this command until the -Verbose parameter stops producing output to retry all of these items.

### Example 3
```powershell
PS Orch1:\> Get-OrchQueueItem -Recurse * -Status Failed | Redo-OrchQueueItem
```

Retries all retryable queue items across all queues in the tenant.

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
Specifies the Id of the queue items to retry.

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
Specifies the Name of the queues that contain the queue items to retry.

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
Specifies the target folder. If not specified, the current folder will be targeted.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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
### System.Int64[]
## OUTPUTS

### UiPath.PowerShell.Entities.BulkOperationResponse
## NOTES

## RELATED LINKS
