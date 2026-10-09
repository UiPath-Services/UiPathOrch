---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Wait-OrchJob.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/08/2026
PlatyPS schema version: 2024-05-01
title: Wait-OrchJob
---

# Wait-OrchJob

## SYNOPSIS

Waits until jobs end and returns each job as it ends.

## SYNTAX

### FromCommandLine (Default)

```
Wait-OrchJob [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 [-Id] <long[]> [-Timeout <int>] [<CommonParameters>]
```

### ByBatch

```
Wait-OrchJob [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 -BatchExecutionKey <string[]> [-Timeout <int>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Wait-OrchJob` cmdlet waits until the specified jobs end and writes each job, read again from Orchestrator, as soon as it ends. A job counts as ended when it is Successful, Faulted, Stopped or Suspended. Suspended is included because a suspended job waits for something outside it, such as an Action Center task, and would otherwise hold the caller for as long as nobody acts, as `Wait-Job` does for a suspended PowerShell job.

A Faulted job is written like any other ended job, not as an error: check the State of the output, for example with `Where-Object State -ne Successful`.

You can pass the jobs to wait for in three ways:

- Pipe Job objects, from `Start-OrchJob` or `Get-OrchJob`. Their folders are known, so -Path is not needed.
- Give their ids with -Id, separated by commas. The cmdlet looks for each id in the target folders.
- Give the BatchExecutionKey of a Start-OrchJob call with -BatchExecutionKey. Every job one call started for one process shares it.

The job list of Orchestrator can lag a job just created by a few seconds. When -Id or -BatchExecutionKey finds nothing, the cmdlet looks again every 2 seconds, for about 8 seconds, before it reports the job as not found.

The cmdlet reads the state of the jobs every 5 seconds, with one request per folder for all the jobs waited for in it, so waiting for many jobs does not multiply the requests. The progress bar shows how many are running, pending and not yet ended. Press Ctrl+C to stop waiting; the jobs keep running.

`Start-OrchJob -Wait` waits in the same way and writes the same output as `Start-OrchJob | Wait-OrchJob`.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Jobs

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1: Start a job and wait for it

```powershell
PS Orch1:\Shared> Start-OrchJob BlankProcess19 | Wait-OrchJob
```

Starts a job and returns it when it has ended, with its final State, EndTime and Info. `Start-OrchJob BlankProcess19 -Wait` does the same.

### Example 2: Wait for jobs by id

```powershell
PS Orch1:\Shared> Wait-OrchJob -Id 192490047,192490048
```

Waits for the two jobs in the current folder and returns each as it ends.

### Example 3: Wait with a time limit

```powershell
PS Orch1:\Shared> Start-OrchJob Report* | Wait-OrchJob -Timeout 3600
```

Starts a job for every process whose name begins with Report and waits up to an hour. A job still not ended after an hour is reported as an OperationTimeout error carrying the job; it is left running.

### Example 4: Wait for the jobs running on a machine before stopping it

```powershell
PS C:\> Enable-OrchMaintenanceMode -Path Orch1: -HostMachineName PC01
PS C:\> Get-OrchJob -Path Orch1:\ -Recurse -State Running,Pending | Where-Object HostMachineName -eq PC01 | Wait-OrchJob -Timeout 7200
PS C:\> Stop-Computer -ComputerName PC01
```

Stops new jobs from going to PC01, waits up to two hours for the jobs already there to end, then shuts the machine down.

### Example 5: Wait for the jobs of one Start-OrchJob call

```powershell
PS Orch1:\Shared> Wait-OrchJob -BatchExecutionKey be1712a1-cb92-4b5f-9952-1416874d2129
```

Waits for every job the Start-OrchJob call with that BatchExecutionKey started.

## PARAMETERS

### -Id

Specifies the ids of the jobs to wait for, separated by commas. Tab completion suggests the jobs of the target folders that have not ended.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: FromCommandLine
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -BatchExecutionKey

Specifies the BatchExecutionKey (GUID) of the jobs to wait for. Every job one Start-OrchJob call started for one process shares it; a call matching several processes gives each process its own key.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByBatch
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Timeout

Specifies, in seconds, how long to wait. When it elapses, each job not yet ended is reported as an OperationTimeout error whose target object is the job as last read, and the cmdlet returns; the jobs keep running. When omitted or negative, the cmdlet waits without a limit.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
SupportsWildcards: false
Aliases:
- TimeoutSec
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

### -Path

Specifies the folders to look in for -Id and -BatchExecutionKey. If not specified, the current folder is used. Not needed for piped jobs.

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

Includes the target folder and all its subfolders when looking for -Id and -BatchExecutionKey.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### UiPath.PowerShell.Entities.Job

You can pipe Job objects from Start-OrchJob or Get-OrchJob.

### System.Int64[]

You can pipe objects with an **Id** property to the **Id** parameter.

## OUTPUTS

### UiPath.PowerShell.Entities.Job

Each job, read again from Orchestrator, as soon as it has ended: Successful, Faulted, Stopped or Suspended.

## NOTES

A job that is not found, and a job still running when -Timeout elapses, are reported as non-terminating errors (ObjectNotFound, OperationTimeout); the other jobs are still waited for and written.

## RELATED LINKS

[Start-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchJob.md)

[Get-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJob.md)

[Stop-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchJob.md)
