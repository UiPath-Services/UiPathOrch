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
In the Filter parameter set, you can specify conditions for retrieving jobs using various parameters.

In the JobId parameter set, the `-Id` parameter allows you to directly specify the ID of the job to retrieve. The completion list for this `-Id` parameter only includes job IDs that have been previously retrieved and cached in memory by UiPathOrch.

After retrieving multiple jobs using the first parameter set, if you want to examine a specific job in detail, you can specify its ID using the `-Id` parameter. Even if a job entity with the specified ID exists in the cache, `Get-OrchJob` will check and display the latest state of that job.

Multiple values for the -Path parameter can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Jobs?{filter}&$expand=Robot,Machine,Release&$orderby=CreationTime%20desc, GET /odata/Jobs({jobId})?$expand=Robot,Machine,Release

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchJob -Recurse -First 10
```

  

Retrieves the 10 most recent jobs across all folders.  

### Example 2
```powershell
PS Orch1:\> Get-OrchJob -Recurse
```

  

Retrieves cached jobs across all folders. Since this does not call the Orchestrator Web API, previously retrieved jobs can be displayed quickly. To fetch the latest information, specify any parameter.  

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchJob -Last Day
```

  

Displays jobs from the past day in the current folder. The `-Last` parameter supports values like `Hour`, `Day`, `Week`, and `Month`.  

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchJob -State Pending,Suspended
```

  

Displays jobs in the current folder that are in the `Pending` or `Suspended` state. To display only failed jobs, specify `Faulted` for the `-State` parameter.  

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchJob -SourceType Queue
```

  

Displays jobs in the current folder that were started by a queue trigger.  

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchJob -CreationTimeAfter '2025/01/15 14:00:00' -CreationTimeBefore '2025/01/30 15:00:00'
```

  

Filters jobs based on their creation time using the `-CreationTimeAfter` and `-CreationTimeBefore` parameters. Other available time-based filters include `-StartTimeAfter`, `-StartTimeBefore`, `-EndTimeAfter`, and `-EndTimeBefore`.  

### Example 7
```powershell
PS Orch1:\Shared> Get-OrchJob | ? StopStrategy -eq Kill
```

  

Displays only cached jobs that were forcefully stopped.  

### Example 8
```powershell
PS Orch1:\Shared> Get-OrchJob | group ReleaseName -NoElement
```

  

Groups cached jobs by process name. The property names can be completed using tab completion. `group` is an alias for `Group-Object`.  

### Example 9
```powershell
PS Orch1:\Shared> Get-OrchJob | group HostMachineName -NoElement
```

  

Groups cached jobs by machine name.  

### Example 10
```powershell
PS Orch1:\Shared> Get-OrchJob | group HostMachineName,State -NoElement
```

  

Groups cached jobs by machine name and job status.  

### Example 11
```powershell
PS Orch1:\Shared> Get-OrchJob | group LocalSystemAccount,State -NoElement | Format-Table -AutoSize
```

  

Groups cached jobs by the account that executed them. `Format-Table -AutoSize` is used to ensure that the output is not truncated.  

### Example 12
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.Date } -NoElement
```

  

Groups cached jobs by creation date.  

### Example 13
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.ToString('yyyy/MM') } -NoElement
```

  

Groups cached jobs by creation month.  

### Example 14
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.DayOfWeek } -NoElement
```

  

Groups cached jobs by the day of the week they were created.  

### Example 15
```powershell
PS Orch1:\Shared> Get-OrchJob | group { $_.CreationTime.DayOfWeek },State -NoElement
```

  

Groups cached jobs by the day of the week they were created and their status.  

### Example 16
```powershell
PS Orch1:\Shared> Get-OrchJob | ? { $_.CreationTime.DayOfWeek -eq 'Sunday' }
```

  

Displays only cached jobs that were created on a Sunday.  

### Example 17
```powershell
PS Orch1:\Shared> Get-OrchJob | ? StartTime -ne $null | group { $_.StartTime.Hour },State -NoElement
```

  

Groups cached jobs by the hour they started and their status. This allows for quick analysis of when jobs were executed and their status trends.  

### Example 18
```powershell
PS Orch1:\Shared> Get-OrchJob | group ReleaseName -NoElement | sort Count -Descending
```

  

Displays grouped job information sorted by count in descending order. `sort` is an alias for `Sort-Object`.  

### Example 19
```powershell
PS Orch1:\Shared> Get-OrchJob -State Faulted | group ReleaseName | sort Count -Descending
```

  

Groups only failed jobs by process name and displays them in descending order of occurrence. This helps identify processes that fail frequently.  

Note: Using the `-State` parameter queries Orchestrator, so for faster processing with cached jobs, use `| ? State -eq Faulted` instead.  

### Example 20
```powershell
PS Orch1:\Shared> Get-OrchJob | sort { ($_.EndTime - $_.StartTime).TotalSeconds } -Descending
```

  

Sorts cached jobs in descending order by execution duration.  

### Example 21
```powershell
PS Orch1:\Shared> Get-OrchJob | select Id, ReleaseName, @{ Name='TotalSeconds'; Expression={ ($_.EndTime - $_.StartTime).TotalSeconds }} | sort TotalSeconds -Descending
```

  

Sorts cached jobs in descending order by execution duration and displays the execution time.  

### Example 22
```powershell
PS Orch1:\Shared> Get-OrchJob | % { ($_.EndTime - $_.StartTime).TotalMinutes } | Measure-Object -Average -Sum -Maximum -Minimum
```

  

Displays the average, longest, and shortest execution times of cached jobs. `%` is an alias for `ForEach-Object`.

## PARAMETERS

### -CreationTimeAfter
{{ Fill CreationTimeAfter Description }}

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
{{ Fill CreationTimeBefore Description }}

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
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included.

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
{{ Fill Last Description }}

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

### -Priority
{{ Fill Priority Description }}

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

### -Recurse
Specifies that the operation should include the target folder and all its subfolders.

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

### -SourceType
{{ Fill SourceType Description }}

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
{{ Fill State Description }}

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
Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

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
Gets only the specified number of objects.
Enter the number of objects to get.

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
{{ Fill ReleaseName Description }}

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
{{ Fill OrderAscending Description }}

```yaml
Type: SwitchParameter
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OrderBy
{{ Fill OrderBy Description }}

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
{{ Fill Id Description }}

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
{{ Fill Robot Description }}

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

## RELATED LINKS
