---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJob

## SYNOPSIS
Gets jobs.

## SYNTAX

### JobId (Default)
```
Get-OrchJob [-Id <Int64[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### Filter
```
Get-OrchJob [-Last <String>] [-CreationTimeAfter <DateTime>] [-CreationTimeBefore <DateTime>]
 [-StartTimeAfter <DateTime>] [-StartTimeBefore <DateTime>] [-EndTimeAfter <DateTime>]
 [-EndTimeBefore <DateTime>] [-ResumeTimeAfter <DateTime>] [-ResumeTimeBefore <DateTime>] [-Priority <String>]
 [-ReleaseName <String[]>] [-SourceType <String[]>] [-State <String[]>] [-ProcessType <String[]>]
 [-Robot <String[]>] [-Skip <UInt64>] [-OrderBy <String>] [-OrderAscending] [-First <UInt64>]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchJob cmdlet retrieves job execution information from UiPath Orchestrator. Jobs represent individual process executions and contain detailed information about automation runs including status, timing, robot assignments, and execution results.

This cmdlet provides flexible filtering capabilities to retrieve jobs based on various criteria including time ranges, execution states, process names, robot assignments, and priority levels. Use the -Last parameter for quick time-based filtering or specific date parameters for precise time range queries.

The cmdlet supports two parameter sets: JobId for retrieving specific jobs by ID, and Filter for advanced filtering and sorting operations. The Filter parameter set includes pagination support with -Skip and -First parameters for efficient handling of large result sets.

Jobs can be filtered by execution state (Pending, Running, Successful, Faulted, Failed, Stopped), priority levels, source types, and process types. Use the -Recurse parameter to search across all subfolders and -Depth to limit the search depth.

This is a folder entity cmdlet that operates within specific folder scopes. Use Set-Location to navigate to the target folder or specify folders using the -Path parameter. The cmdlet includes built-in caching to improve performance when no filter parameters are specified.

Primary Endpoint: GET /odata/Jobs

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchJob -Last Day -First 10
```

Gets the first 10 jobs from the last day in the current folder.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchJob -State Successful -First 5
```

Gets the first 5 successful jobs from the current folder.

### Example 3
```powershell
PS C:\> Get-OrchJob -Path Orch1:\Shared -ReleaseName InvoiceProcess -Last Week
```

Gets all jobs for InvoiceProcess from the last week in the Shared folder.

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchJob -State Faulted,Failed -Priority High -First 10
```

Gets the first 10 high-priority jobs that have failed or faulted.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJob -CreationTimeAfter (Get-Date).AddHours(-6) -OrderBy StartTime -OrderAscending
```

Gets jobs created in the last 6 hours, ordered by start time in ascending order.

### Example 6
```powershell
PS Orch1:\> Get-OrchJob -Recurse -Robot robotaccount01-unattended -Last Month | Group-Object State
```

Gets all jobs executed by robotaccount01-unattended in the last month across all subfolders and groups them by state.

### Example 7
```powershell
PS Orch1:\Shared> Get-OrchJob -Id 12345,12346,12347
```

Gets specific jobs by their IDs for detailed analysis.

### Example 8
```powershell
PS Orch1:\Shared> Get-OrchJob -StartTimeAfter (Get-Date "2024-01-01") -StartTimeBefore (Get-Date "2024-01-31") -State Successful
```

Gets all successful jobs that started in January 2024.

### Example 9
```powershell
PS Orch1:\Shared> Get-OrchJob -Last Week -Skip 50 -First 25 | Select-Object Id, ReleaseName, State, Duration
```

Gets jobs 51-75 from the last week and displays key information for pagination.

### Example 10
```powershell
PS Orch1:\Shared> Get-OrchJob -ProcessType Unattended -Priority Normal -Last Month | Where-Object {$_.Duration -gt "00:30:00"}
```

Gets unattended jobs with normal priority from the last month that ran longer than 30 minutes.

## PARAMETERS

### -CreationTimeAfter
Specifies the earliest creation time for jobs to retrieve.

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CreationTimeBefore
Specifies the latest creation time for jobs to retrieve.

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Depth
Specifies the depth of folder recursion. A depth of 0 targets only the current folder.

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

### -Last
Specifies a time period for recent jobs. Valid values: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies target folders. Use comma-separated values for multiple folders. Supports wildcards. If not specified, targets the current folder.

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
Specifies job priority to filter by. Valid values: Low, Normal, High.

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
Controls how progress information is displayed during cmdlet execution.

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

### -Recurse
Includes the target folder and all its subfolders in the operation.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -SourceType
Specifies job source types to filter by. Valid values include: Manual, Schedule, Agent, Robot.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -State
Specifies job states to filter by. Valid values include: Pending, Running, Successful, Faulted, Stopped, Suspended.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip.

```yaml
Type: UInt64
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
Specifies the maximum number of jobs to return.

```yaml
Type: UInt64
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ReleaseName
Specifies process names to filter by. Supports wildcards and multiple values.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -OrderAscending
Sorts results in ascending order. Default is descending.

```yaml
Type: SwitchParameter
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -OrderBy
Specifies the property to sort by. Valid values include: Id, CreationTime, StartTime, EndTime, State.

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProcessType
[PLACEHOLDER - requires verification of ProcessType parameter description]

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -EndTimeAfter
[PLACEHOLDER - requires verification of EndTimeAfter parameter description]

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EndTimeBefore
[PLACEHOLDER - requires verification of EndTimeBefore parameter description]

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResumeTimeAfter
[PLACEHOLDER - requires verification of ResumeTimeAfter parameter description]

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResumeTimeBefore
[PLACEHOLDER - requires verification of ResumeTimeBefore parameter description]

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeAfter
[PLACEHOLDER - requires verification of StartTimeAfter parameter description]

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeBefore
[PLACEHOLDER - requires verification of StartTimeBefore parameter description]

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Id
Specifies job IDs to retrieve. Supports multiple values.

```yaml
Type: Int64[]
Parameter Sets: JobId
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Robot
Specifies robot names to filter by. Supports wildcards and multiple values.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Job
## NOTES
Job entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Use the Filter parameter set for complex queries with time ranges, status filters, and sorting options. The JobId parameter set is optimized for retrieving specific jobs by ID.

Jobs represent the execution history of automation processes and provide detailed information about robot performance and process outcomes.



Primary Endpoint: GET /odata/Jobs
OAuth required scopes: OR.Jobs or OR.Jobs.Read
Required permissions: Jobs.View

## RELATED LINKS

[Start-OrchJob](Start-OrchJob.md)

[Stop-OrchJob](Stop-OrchJob.md)

[Get-OrchJobMedia](Get-OrchJobMedia.md)

[Open-OrchJob](Open-OrchJob.md)


