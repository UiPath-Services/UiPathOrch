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
Gets job information from UiPath Orchestrator folders. Jobs represent process executions and contain details about automation runs including status, timing, robot assignments, and execution results.

This cmdlet provides powerful filtering capabilities to query jobs by various criteria such as time ranges, status, process names, robots, and priorities. It operates on folder entities and supports recursive retrieval across folder hierarchies.

Multiple values for parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Jobs

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchJob -First 5
```

Gets the first 5 jobs from the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchJob -Recurse -State Faulted -First 10
```

Gets the first 10 failed jobs from all folders.

### Example 3
```powershell
PS Orch1:\> Get-OrchJob -Last Day -State Successful
```

Gets all successful jobs from the last day.

### Example 4
```powershell
PS Orch1:\> Get-OrchJob -ReleaseName BlankProcess19 -First 5
```

Gets the first 5 jobs for a specific process.

### Example 5
```powershell
PS Orch1:\> Get-OrchJob -Recurse -Priority High -State Running
```

Gets running high-priority jobs from all folders.

### Example 6
```powershell
PS Orch1:\> Get-OrchJob -CreationTimeAfter (Get-Date).AddHours(-1) -First 10
```

Gets jobs created in the last hour.

### Example 7
```powershell
PS Orch1:\> Get-OrchJob -Robot Robot1 -State Successful -OrderBy CreationTime
```

Gets successful jobs for a specific robot ordered by creation time.

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
{{ Fill ProcessType Description }}

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
{{ Fill EndTimeAfter Description }}

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
{{ Fill EndTimeBefore Description }}

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
{{ Fill ResumeTimeAfter Description }}

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
{{ Fill ResumeTimeBefore Description }}

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
{{ Fill StartTimeAfter Description }}

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
{{ Fill StartTimeBefore Description }}

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

### System.Int64[]
Job IDs can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Job
Job objects can be piped to this cmdlet. The Id property will be automatically mapped to the -Id parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.Job
Returns job objects with metadata including Id, ReleaseName, State, SourceType, CreationTime, StartTime, EndTime, Robot, and other properties.

## NOTES
Job entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Use the Filter parameter set for complex queries with time ranges, status filters, and sorting options. The JobId parameter set is optimized for retrieving specific jobs by ID.

Jobs represent the execution history of automation processes and provide detailed information about robot performance and process outcomes.

## RELATED LINKS

[Start-OrchJob](Start-OrchJob.md)

[Stop-OrchJob](Stop-OrchJob.md)

[Get-OrchJobMedia](Get-OrchJobMedia.md)

[Open-OrchJob](Open-OrchJob.md)
