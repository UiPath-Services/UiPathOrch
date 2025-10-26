---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchQueueItem

## SYNOPSIS
Retrieves queue items from UiPath Orchestrator queues with comprehensive filtering capabilities.

## SYNTAX

```
Get-OrchQueueItem [[-Name] <String[]>] [-Status <String[]>] [-Revision <String[]>] [-Priority <String[]>]
 [-Exception <String[]>] [-Robot <String[]>] [-Reviewer <String[]>] [-DueDateAfter <DateTime>]
 [-DueDateBefore <DateTime>] [-DeferDateAfter <DateTime>] [-DeferDateBefore <DateTime>]
 [-StartProcessingAfter <DateTime>] [-StartProcessingBefore <DateTime>] [-EndProcessingAfter <DateTime>]
 [-EndProcessingBefore <DateTime>] [-Skip <Int32>] [-First <Int32>] [-OrderBy <String>] [-OrderAscending]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchQueueItem cmdlet retrieves queue items from UiPath Orchestrator queues with comprehensive filtering capabilities. Queue items represent work units in automation processes, containing data to be processed and tracking their execution status, priority, and processing history.

Each queue item contains extensive information including Status (New, InProgress, Successful, Failed, etc.), Priority (High, Normal, Low), processing timestamps, robot assignment, specific content data, exception details, and review information. Items also include unique identifiers (Id, Key, UniqueKey) and processing metadata.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. **Important**: This cmdlet requires at least one filter parameter to prevent excessive data retrieval. Without filters, it will output cached contents with a warning.

Primary Endpoint: GET /odata/QueueItems

OAuth required scopes: OR.Queues or OR.Queues.Read

Required permissions: Queues.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchQueueItem -Status Deleted -First 5
```

Retrieves the first 5 queue items with Deleted status from the current folder.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestValue -Status Deleted -First 2
```

Gets the first 2 deleted queue items from the specific queue named TestValue.

### Example 3
```powershell
PS C:\> Get-OrchQueueItem -Path Orch1:\Shared -Recurse -Status Deleted -Priority Normal
```

Retrieves normal-priority queue items with Deleted status from the Shared folder and its subfolders.

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchQueueItem -StartProcessingAfter (Get-Date).AddHours(-24) | ConvertTo-Json -Depth 3
```

Gets queue items that started processing in the last 24 hours and displays detailed JSON information including SpecificContent and ProcessingException. The output shows comprehensive queue item structure with robot details, processing times, and specific content data.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchQueueItem -Status Deleted -OrderBy EndProcessing -OrderAscending
```

Retrieves deleted queue items from the current folder, ordered by end processing time in ascending order (oldest first).

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchQueueItem -First 10
```

```
WARNING: Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.
```

Demonstrates the caching behavior when no filter parameters are specified. The cmdlet outputs cached contents with a warning, emphasizing the need for filter parameters to query fresh data from Orchestrator.

## PARAMETERS

### -Name
Specifies the names of queues whose items should be retrieved. Supports wildcard patterns for flexible queue selection.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target folders to search. If not specified, the current folder context will be used. For folder entity operations requiring path specification.

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

### -Skip
Specifies the number of queue items to skip from the beginning of the result set. Useful for pagination.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
Specifies the maximum number of queue items to return from the beginning of the result set.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Depth
Specifies the depth for recursion into target folders. A depth of 0 indicates the current location only. Higher values include more subfolder levels.

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

### -Recurse
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive queue item discovery.

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

### -Exception
Specifies the exception types to filter queue items. Use to find items with specific error types.

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

### -Priority
Specifies the priority levels to filter queue items. Common values include High, Normal, Low.

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

### -Reviewer
Specifies the reviewer usernames to filter queue items. Use to find items assigned to specific reviewers.

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

### -Revision
Specifies the revision numbers to filter queue items.

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

### -Robot
Specifies the robot names to filter queue items. Use to find items processed by specific robots.

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

### -Status
Specifies the status values to filter queue items. Common values include New, InProgress, Successful, Failed, Abandoned, Retried, Deleted.

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

### -OrderAscending
Specifies whether to order results in ascending (true) or descending (false) order.

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

### -OrderBy
Specifies the field to order results by (e.g., StartProcessing, EndProcessing, Priority, Status).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -DeferDateAfter
Specifies the earliest defer date for filtering queue items. Only items with defer dates after this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -DeferDateBefore
Specifies the latest defer date for filtering queue items. Only items with defer dates before this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -DueDateAfter
Specifies the earliest due date for filtering queue items. Only items with due dates after this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -DueDateBefore
Specifies the latest due date for filtering queue items. Only items with due dates before this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EndProcessingAfter
Specifies the earliest end processing time for filtering queue items. Only items that finished processing after this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EndProcessingBefore
Specifies the latest end processing time for filtering queue items. Only items that finished processing before this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartProcessingAfter
Specifies the earliest start processing time for filtering queue items. Only items that started processing after this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartProcessingBefore
Specifies the latest start processing time for filtering queue items. Only items that started processing before this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem
## NOTES
This cmdlet is a folder entity operation requiring at least one filter parameter to prevent excessive data retrieval. The cmdlet will output cached contents with a warning if no filter parameters are specified. Common filter patterns include queue names (Name), status values (Status), time ranges (StartProcessingAfter/Before), and robot assignments (Robot). Use pagination parameters (Skip, First) to manage large result sets. This operation requires Queues.View permissions in the target folders.



Primary Endpoint: GET /odata/QueueItems
OAuth required scopes: OR.Queues or OR.Queues.Read
Required permissions: Transactions.View

## RELATED LINKS

[Add-OrchQueueItem](Add-OrchQueueItem.md)

[Set-OrchQueueItem](Set-OrchQueueItem.md)

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)

[Get-OrchQueue](Get-OrchQueue.md)



