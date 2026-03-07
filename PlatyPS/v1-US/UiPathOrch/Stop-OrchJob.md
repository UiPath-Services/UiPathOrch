---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Stop-OrchJob
---

# Stop-OrchJob

## SYNOPSIS

Stops running jobs in UiPath Orchestrator.

## SYNTAX

### FromCommandLine (Default)

```
Stop-OrchJob [-Id] <long[]> [-Job <Job>] [-Force] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Stop-OrchJob` cmdlet stops one or more running jobs in UiPath Orchestrator. By default, the cmdlet sends a graceful stop request. Use the `-Force` parameter to kill jobs immediately instead of stopping them gracefully.

Jobs that are already in a terminal state (Terminating, Faulted, Successful, or Stopped) are automatically skipped.

The cmdlet groups jobs by folder before sending the stop request and batches the operations during end processing.

The cmdlet accepts job objects from the pipeline, allowing you to pipe output from `Get-OrchJob` directly to `Stop-OrchJob`.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.StopJobs (or KillJobs with -Force)

OAuth required scopes: OR.Jobs

Required permissions: Jobs.Edit

## EXAMPLES

### Example 1: Stop a specific job

```powershell
PS Orch1:\Shared> Stop-OrchJob -Id 12345
```

Sends a graceful stop request to the job with ID 12345.

### Example 2: Kill a job with -Force

```powershell
PS Orch1:\Shared> Stop-OrchJob -Id 12345 -Force
```

Immediately kills the job with ID 12345 without waiting for graceful shutdown.

### Example 3: Pipeline from Get-OrchJob

```powershell
PS Orch1:\Shared> Get-OrchJob -State Running | Stop-OrchJob
```

Gets all running jobs in the current folder and sends a graceful stop request to each.

### Example 4: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Stop-OrchJob -Id 12345, 12346 -WhatIf
```

Shows what would happen if the specified jobs were stopped, without actually stopping them.

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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

### -Force

Kills the job immediately instead of sending a graceful stop request. When specified, the cmdlet calls the KillJobs endpoint rather than StopJobs.

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

### -Id

Specifies the job ID or IDs to stop. Tab completion suggests IDs from running or stoppable jobs.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Job

Specifies a Job object to stop. This parameter accepts pipeline input from `Get-OrchJob`.

```yaml
Type: UiPath.PowerShell.Entities.Job
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### System.Int64

You can pipe a job ID to the **Id** parameter.

### UiPath.PowerShell.Entities.Job

You can pipe Job objects from `Get-OrchJob` to this cmdlet.

### System.String

You can pipe a folder path to the **Path** parameter.

### System.Int64[]

You can pipe an array of job IDs to the **Id** parameter.

### System.String[]

You can pipe an array of folder paths to the **Path** parameter.

## OUTPUTS

### None

This cmdlet does not generate output by default.

## NOTES

Jobs in terminal states (Terminating, Faulted, Successful, Stopped) are automatically skipped without error.

## RELATED LINKS

Get-OrchJob

Start-OrchJob

Open-OrchJob
