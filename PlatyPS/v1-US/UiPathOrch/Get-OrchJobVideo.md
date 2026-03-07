---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchJobVideo
---

# Get-OrchJobVideo

## SYNOPSIS

Gets jobs that have video recordings attached.

## SYNTAX

### __AllParameterSets

```
Get-OrchJobVideo [[-Path] <string[]>] [[-Skip] <ulong>] [[-First] <ulong>] [-Recurse]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets jobs that have video recordings from UiPath Orchestrator. This function filters jobs from the last week that have the HasVideoRecorded property set to true, returning only process-type jobs with attached video recordings.

This is a convenience wrapper around Get-OrchJob that applies the following filters: -ProcessType Process, -Last Week, and HasVideoRecorded -eq $true.

Primary Endpoint: GET /odata/Jobs

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1: Get jobs with video recordings

```powershell
PS Orch1:\Shared> Get-OrchJobVideo
```

Gets all jobs with video recordings from the last week in the current folder.

### Example 2: Get jobs with video recordings recursively

```powershell
PS Orch1:\> Get-OrchJobVideo -Recurse
```

Gets all jobs with video recordings from the last week across all folders.

### Example 3: Get the first 10 jobs with video recordings

```powershell
PS Orch1:\Shared> Get-OrchJobVideo -First 10
```

Gets the first 10 jobs with video recordings from the last week in the current folder.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
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

### -First

Gets only the specified number of jobs. Enter the number of jobs to get.

```yaml
Type: System.UInt64
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Skip

Skips the specified number of jobs and then gets the remaining jobs. Enter the number of jobs to skip.

```yaml
Type: System.UInt64
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
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

### System.String[]

You can pipe folder paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.Job

Returns Job objects that have video recordings attached, with properties including HasVideoRecorded set to true.

## NOTES

This is a PowerShell function that wraps Get-OrchJob with the filters -ProcessType Process, -Last Week, and HasVideoRecorded -eq $true.

## RELATED LINKS

Get-OrchJob
