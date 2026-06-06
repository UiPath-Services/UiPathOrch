---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJob.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchJob
---

# Get-OrchJob

## SYNOPSIS

Gets jobs from UiPath Orchestrator.

## SYNTAX

### JobId (Default)

```
Get-OrchJob [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Id] <long[]>]
 [<CommonParameters>]
```

### Filter

```
Get-OrchJob [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-CreationTimeAfter <datetime>]
 [-CreationTimeBefore <datetime>] [-EndTimeAfter <datetime>] [-EndTimeBefore <datetime>]
 [-First <ulong>] [-Last <string>] [-OrderAscending] [-OrderBy <string>]
 [-Priority <string>] [-ProcessType <string[]>] [-ReleaseName <string[]>]
 [-ResumeTimeAfter <datetime>] [-ResumeTimeBefore <datetime>] [-Robot <string[]>]
 [-Skip <ulong>] [-SourceType <string[]>] [-StartTimeAfter <datetime>]
 [-StartTimeBefore <datetime>] [-State <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchJob` cmdlet gets jobs from UiPath Orchestrator. You can retrieve jobs by specifying one or more job IDs using the **JobId** parameter set, or by using filter parameters with the **Filter** parameter set.

You must specify at least one filter parameter to query Orchestrator. If no filter parameters are specified, the cmdlet outputs the cached job contents and displays a warning.

When filtering by robot name, the cmdlet batches requests in groups of 10 due to an API limitation.

The cmdlet supports multi-threaded folder processing when targeting multiple folders with `-Recurse`.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Jobs?{filter}&$expand=Robot,Machine,Release&$orderby=CreationTime%20desc

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1: Get jobs from the last day

```powershell
PS Orch1:\Shared> Get-OrchJob -Last Day
```

Gets all jobs created within the last day from the current folder.

### Example 2: Get a specific job by ID

```powershell
PS C:\> Get-OrchJob -Path Orch1:\Shared -Id 147426438
```

Gets the job with the specified ID from the Shared folder.

### Example 3: Get running jobs

```powershell
PS Orch1:\Shared> Get-OrchJob -State Running
```

Gets all currently running jobs from the current folder.

### Example 4: Filter by process name and state

```powershell
PS Orch1:\Shared> Get-OrchJob -ReleaseName Blank* -State Faulted,Successful
```

Gets jobs for processes matching the wildcard pattern `Blank*` that are in the Faulted or Successful state.

### Example 5: Get jobs with a time range filter

```powershell
PS Orch1:\Shared> Get-OrchJob -CreationTimeAfter 2026-03-01 -CreationTimeBefore 2026-03-05
```

Gets jobs created between March 1 and March 5, 2026.

### Example 6: Get jobs from a specific folder

```powershell
PS C:\> Get-OrchJob -Path Orch1:\Production -Last Week
```

Gets jobs created within the last week from the Production folder.

### Example 7: Get jobs recursively with paging

```powershell
PS C:\> Get-OrchJob -Path Orch1:\Shared -Last Month -Recurse -First 50 -Skip 100
```

Gets jobs created within the last month from the Shared folder and all its subfolders, skipping the first 100 results and returning the next 50.

### Example 8: Extract a nested property (the robot name) from each job

```powershell
PS Orch1:\Shared> Get-OrchJob -First 5 | Select-Object Path, EndTime, @{Name='RobotName'; Expression = { $_.Robot.Name }}
```

`Robot.Name` is a nested property, so it cannot be selected as a plain column — surface it with a calculated property (a script block). To discover which fields (and nested fields) a job exposes, dump one as JSON first: `Get-OrchJob -First 1 | ConvertTo-Json -Depth 5`. `Get-OrchJob` is folder-scoped, so run it from a folder (`cd Orch1:\Shared`) or pass `-Path`; it cannot run at the drive root.

### Example 9: Audit Unattended robots by their last successful job

```powershell
PS Orch1:\Shared> Get-OrchJob -Robot (Get-OrchRobot | Where-Object Type -eq Unattended | Select-Object -ExpandProperty Name) -State Successful -OrderBy EndTime | Select-Object @{N='RobotName';E={$_.Robot.Name}}, EndTime
```

Lists the most recent successful jobs for the Unattended robots, newest first — useful for spotting licensed robots that have not run in a long time. `Get-OrchRobot` supplies the robot names to `-Robot`, and `-OrderBy EndTime` sorts by completion time. Add `-Recurse` to scan every folder.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -CreationTimeAfter

Filters jobs created after the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -CreationTimeBefore

Filters jobs created before the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -EndTimeAfter

Filters jobs that ended after the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -EndTimeBefore

Filters jobs that ended before the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
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

Gets only the specified number of objects.
Enter the number of objects to get.

```yaml
Type: System.Nullable`1[System.UInt64]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
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

Specifies the job ID or IDs to retrieve. Tab completion suggests job IDs from the local cache.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: JobId
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Last

Filters jobs by a predefined time range relative to the current time. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -OrderAscending

Specifies that the results should be sorted in ascending order. By default, results are sorted in descending order.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
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

Specifies the field to sort the results by. Valid values are: CreationTime, Release/Name, State, SpecificPriorityValue, StartTime, EndTime, SourceType.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
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

Filters jobs by priority level. Valid values are: Critical, Highest, VeryHigh, High, MediumHigh, Medium, MediumLow, Low, VeryLow, Lowest.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ProcessType

Filters jobs by process type. Wildcard characters are permitted. Valid values are: Undefined, Process, TestAutomationProcess. The default value is `Process`.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ReleaseName

Filters jobs by the release (process) name. Wildcard characters are permitted. Tab completion suggests release names from Orchestrator.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ResumeTimeAfter

Filters jobs that were resumed after the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ResumeTimeBefore

Filters jobs that were resumed before the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
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

Filters jobs by robot name. Wildcard characters are permitted. Tab completion suggests robot names from Orchestrator. When multiple robots are specified, requests are batched in groups of 10 due to an API limitation.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: Filter
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

Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

```yaml
Type: System.Nullable`1[System.UInt64]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -SourceType

Filters jobs by the source that triggered them. Wildcard characters are permitted. Valid values are: Manual, Schedule, Agent, Queue, Event, Studio, Apps, ApiTrigger.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartTimeAfter

Filters jobs that started after the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartTimeBefore

Filters jobs that started before the specified date and time.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -State

Filters jobs by execution state. Valid values are: Pending, Running, Stopping, Terminating, Faulted, Successful, Stopped, Suspended, Resumed.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
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

### System.Int64[]

You can pipe job IDs to this cmdlet.

### System.String

You can pipe a folder path to the **Path** parameter.

### System.DateTime

You can pipe DateTime values to the time filter parameters.

### System.String[]

You can pipe string arrays to filter parameters such as **ReleaseName**, **SourceType**, **State**, **ProcessType**, and **Robot**.

### System.UInt64

You can pipe values to the **Skip** and **First** parameters.

## OUTPUTS

### UiPath.PowerShell.Entities.Job

This cmdlet returns Job objects representing UiPath Orchestrator jobs.

## NOTES

If no filter parameters are specified, the cmdlet outputs the contents of the local job cache and writes a warning. You must specify at least one filter parameter to query Orchestrator.

## RELATED LINKS

[Start-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchJob.md)

[Stop-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchJob.md)

[Open-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Open-OrchJob.md)
