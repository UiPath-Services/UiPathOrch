---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchQueueItem
---

# Get-OrchQueueItem

## SYNOPSIS

Gets queue items (transactions) from UiPath Orchestrator queues.

## SYNTAX

### __AllParameterSets

```
Get-OrchQueueItem [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [-DeferDateAfter <datetime>] [-DeferDateBefore <datetime>] [-DueDateAfter <datetime>]
 [-DueDateBefore <datetime>] [-EndProcessingAfter <datetime>]
 [-EndProcessingBefore <datetime>] [-Exception <string[]>] [-First <int>] [-Id <long[]>]
 [-OrderAscending] [-OrderBy <string>] [-Priority <string[]>] [-Reviewer <string[]>]
 [-Revision <string[]>] [-Robot <string[]>] [-Skip <int>]
 [-StartProcessingAfter <datetime>] [-StartProcessingBefore <datetime>]
 [-Status <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets queue items (transactions) from UiPath Orchestrator queues. Queue items represent individual work units that are processed by robots through the queue mechanism. Each item has a status, priority, and optional scheduling dates.

This cmdlet supports extensive filtering by status, priority, robot, reviewer, exception type, various date ranges, and specific item ids (-Id, emitted as an OData `Id in (...)` filter). When no filter parameters are specified, the cmdlet outputs cached items and displays a warning. Use -First to limit the number of results returned.

Results are ordered by Id in descending order by default. Use -OrderBy to change the sort field and -OrderAscending to reverse the sort direction.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available queue names dynamically populated from the target folders. The -Status, -Revision, -Priority, and -Exception parameters also support tab completion with predefined values.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Queue-specific payload lives in the SpecificContent dictionary (and its JSON-string twin SpecificData), so the default table view cannot column the per-item keys. Two conveniences are provided to work around this:

- Each QueueItem has an Expanded ScriptProperty (attached by the module via Update-TypeData). It returns a PSCustomObject whose leading columns are Id, Reference, Status, Priority, DeferDate, DueDate, StartProcessing, EndProcessing, followed by SpecificContent keys in sorted order. Pipe `| ForEach-Object Expanded | Format-Table` to view a single queue's items as a flat table.
- Format-OrchQueueItem handles mixed-queue output. It groups piped items by QueueDefinitionId and emits one Format-Table per queue, so each block uses that queue's specific columns without leaking schemas across queues. Format-Table by itself locks columns on the first object seen and would silently hide keys unique to later queues.

Primary Endpoint: GET /odata/QueueItems?$filter=(QueueDefinitionId eq {queueId})&$expand=Robot,ReviewerUser&orderby=Id

OAuth required scopes: OR.Queues or OR.Queues.Read

Required permissions: Queues.View and Transactions.View

## EXAMPLES

### Example 1: Get all items from a queue

```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestQueue2
```

Gets all queue items from the queue named "TestQueue2" in the current folder.

### Example 2: Get items filtered by status

```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestQueue2 -Status Failed
```

Gets only the failed queue items from the "TestQueue2" queue. Valid status values are: New, InProgress, Failed, Successful, Abandoned, Retried, and Deleted.

### Example 3: Get high-priority items with a result limit

```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestQueue2 -Priority High -First 10
```

Gets the first 10 high-priority queue items from the "TestQueue2" queue.

### Example 4: Get items within a date range

```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestQueue2 -DueDateAfter "2026-01-01" -DueDateBefore "2026-03-01"
```

Gets queue items from the "TestQueue2" queue that have a due date between January 1, 2026 and March 1, 2026.

### Example 5: Get items ordered by processing end date

```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestQueue2 -Status Successful -OrderBy EndProcessing -OrderAscending
```

Gets successful queue items from the "TestQueue2" queue, ordered by end processing date in ascending order.

### Example 6: Get items from a specific folder with robot filter

```powershell
PS C:\> Get-OrchQueueItem -Path Orch1:\Production TestQueue2 -Robot Robot01
```

Gets queue items from the "TestQueue2" queue in the Production folder that were processed by "Robot01". When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 7: Get items recursively from all folders

```powershell
PS Orch1:\> Get-OrchQueueItem -Recurse -Status Failed -First 20
```

Gets the first 20 failed queue items from all queues across all folders recursively.

### Example 8: Get specific items by id

```powershell
PS Orch1:\Shared> Get-OrchQueueItem TestQueue2 -Id 12345,12346,12347
```

Gets the three queue items with the given ids from the "TestQueue2" queue in a single request (an OData `Id in (...)` filter). Tab completion on -Id suggests ids already loaded in the cache for the queue.

### Example 9: Flatten a single queue into a table with SpecificContent columns

```powershell
PS Orch1:\Shared> Get-OrchQueueItem OrderQueue -First 20 | ForEach-Object Expanded | Format-Table
```

Uses the Expanded ScriptProperty to promote SpecificContent keys into top-level columns. Format-Table then renders one row per item with columns for Id, Reference, Status, Priority, the standard dates, and the queue's specific payload keys (e.g., OrderId, Customer, Amount).

### Example 10: Render multiple queues with per-queue schemas

```powershell
PS Orch1:\Shared> Get-OrchQueueItem -Name 'Order*' -Status New | Format-OrchQueueItem
```

Groups items by QueueDefinitionId and emits one Format-Table block per queue. Needed when the matched queues have different SpecificContent schemas — a plain `| Format-Table` would column-lock on the first queue and hide later queues' keys.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -DeferDateAfter

Filters queue items to those with a defer date after the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -DeferDateBefore

Filters queue items to those with a defer date before the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -DueDateAfter

Filters queue items to those with a due date after the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -DueDateBefore

Filters queue items to those with a due date before the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -EndProcessingAfter

Filters queue items to those that finished processing after the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -EndProcessingBefore

Filters queue items to those that finished processing before the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -Exception

Filters queue items by exception type. Tab completion suggests available exception categories. Supports wildcards.

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

### -First

Gets only the specified number of queue items. Enter the number of items to retrieve. Tab completion suggests 10 as a default value.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
SupportsWildcards: false
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

### -Id

Filters queue items to the specified item ids. Accepts multiple comma-separated values, emitted as a single OData `Id in (...)` filter (one server round trip). Combine with -Name to scope the lookup to a queue. Tab completion suggests ids already loaded in the cache for the targeted queue(s) and excludes ids already typed on the same -Id list.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
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

### -Name

Specifies the names of the queues from which to retrieve items. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the target folders.

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

### -OrderAscending

Sorts results in ascending order. By default, results are sorted in descending order. Use with -OrderBy to control both the sort field and direction.

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

### -OrderBy

Specifies the field to sort results by. Valid values are: DueDate, DeferDate, StartProcessing, and EndProcessing. The default sort field is DeferDate.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -Priority

Filters queue items by priority level. Valid values are: Low, Normal, and High. Tab completion suggests available priority values. Supports wildcards.

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

### -Reviewer

Filters queue items by the assigned reviewer user. Tab completion suggests available reviewer names. Supports wildcards. When multiple reviewers are specified, they are batched into groups of up to 15 per API call due to API limitations.

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

### -Revision

Filters queue items by revision status. Tab completion suggests available revision values. Supports wildcards.

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

### -Robot

Filters queue items by the robot that processed them. Tab completion suggests available robot names. Supports wildcards. When multiple robots are specified, they are batched into groups of up to 15 per API call due to API limitations.

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

### -Skip

Ignores the specified number of queue items and then gets the remaining items. Enter the number of items to skip. Use with -First for pagination.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
SupportsWildcards: false
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

### -StartProcessingAfter

Filters queue items to those that started processing after the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -StartProcessingBefore

Filters queue items to those that started processing before the specified date and time. Tab completion suggests common time expressions.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
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

### -Status

Filters queue items by processing status. Valid values are: New, InProgress, Failed, Successful, Abandoned, Retried, and Deleted. Tab completion suggests available status values. Supports wildcards.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe queue names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem

Returns QueueItem objects representing individual transactions in a queue. Properties include Id, QueueDefinitionId, Status, Priority, DeferDate, DueDate, StartProcessing, EndProcessing, Robot, ReviewerUser, SpecificContent, Output, and exception information.

## NOTES

Queue items are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The cmdlet enforces a 600ms delay between API calls to respect Orchestrator rate limits. When filtering by -Robot or -Reviewer with many values, the cmdlet batches requests with a maximum of 15 values per API call.

When no filter parameters are specified, the cmdlet returns cached items and displays a warning. Specify at least one filter parameter (such as -Name, -Status, or -First) to query the server directly.

The default sort order is by Id in descending order. Use -OrderBy and -OrderAscending to customize sorting.

For viewing items as a flat table, use the Expanded ScriptProperty (single queue) or Format-OrchQueueItem (multiple queues). See the Description and Examples 9-10.

## RELATED LINKS

[Get-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueue.md)

[Copy-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchQueueItem.md)

[Import-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchQueueItem.md)

[Redo-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Redo-OrchQueueItem.md)

[Remove-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueItem.md)

[Format-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Format-OrchQueueItem.md)
