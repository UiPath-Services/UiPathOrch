---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Restart-OrchJob.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Restart-OrchJob
---

# Restart-OrchJob

## SYNOPSIS

Restarts a Faulted job.

## SYNTAX

### FromCommandLine (Default)

```
Restart-OrchJob [-Id] <long[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### FromPipeline

```
Restart-OrchJob [-Job <Job>] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Re-runs a Faulted job from the beginning. Orchestrator dispatches a fresh execution against the same release/process and folder context as the original. The cmdlet returns the new Job object that Orchestrator created so you can correlate the restart with the original failure.

The -Id parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see Faulted job IDs in the current folder, with each tooltip showing the job's creation/start/end timestamps so you can pick the right one to restart.

You can also pipe Job objects directly from `Get-OrchJob`:

```powershell
Get-OrchJob -State Faulted -Last Day | Restart-OrchJob
```

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.RestartJob

OAuth required scopes: OR.Jobs or OR.Jobs.Write

Required permissions: Jobs.Create

## EXAMPLES

### Example 1: Restart a single Faulted job

```powershell
PS Orch1:\Shared> Restart-OrchJob -Id 159874368
```

Restarts the job with the specified Id from the current folder.

### Example 2: Restart all Faulted jobs from yesterday

```powershell
PS Orch1:\Shared> Get-OrchJob -State Faulted -Last Day | Restart-OrchJob
```

Pipes every Faulted job from the last 24 hours into Restart-OrchJob.

### Example 3: Preview a restart without firing it

```powershell
PS Orch1:\Shared> Restart-OrchJob -Id 159874368 -WhatIf
```

Shows what would happen without actually restarting the job.

## PARAMETERS

### -Id

Specifies one or more job IDs to restart. Tab completion suggests Faulted jobs in the current folder; passing a non-Faulted job ID will be rejected by the server.

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

### -Job

Accepts a Job object via the pipeline (e.g., from `Get-OrchJob`). The job's Path is used for folder context.

```yaml
Type: UiPath.PowerShell.Entities.Job
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: FromPipeline
  Position: Named
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the target folder path(s). If not specified, the current folder is targeted.

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

Recursively traverse subfolders under the specified -Path.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

When -Recurse is used, limits recursion depth.

```yaml
Type: System.UInt32
DefaultValue: ''
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
DefaultValue: ''
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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### System.Int64[]

Job IDs from the pipeline.

### UiPath.PowerShell.Entities.Job

Job objects from the pipeline (e.g., output of Get-OrchJob).

## OUTPUTS

### UiPath.PowerShell.Entities.Job

Returns the newly created Job object on success.

## NOTES

Server-side restart eligibility depends on the job's process type and the licensing/runtime configuration of its folder. A common rejection is "Automation Developer runtime license selected" when the original job ran on a runtime template that has since been reconfigured.

## RELATED LINKS

Get-OrchJob

Resume-OrchJob

Stop-OrchJob

Start-OrchJob
