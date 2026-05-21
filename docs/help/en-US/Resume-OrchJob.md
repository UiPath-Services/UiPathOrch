---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Resume-OrchJob.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Resume-OrchJob
---

# Resume-OrchJob

## SYNOPSIS

Resumes a Suspended job.

## SYNTAX

### FromCommandLine (Default)

```
Resume-OrchJob [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Key] <string[]> [-Confirm]
 [-WhatIf] [<CommonParameters>]
```

### FromPipeline

```
Resume-OrchJob [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Confirm] [-Job <Job>]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Resumes a Suspended job — typically one that paused on a Wait/Form/Bookmark activity. The cmdlet identifies the job by its uuid Key (not the Int64 Id) because the Resume API requires the unique key.

The -Key parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see Suspended jobs in the current folder, with the job's creation/start timestamps shown as a tooltip so you can pick the right one.

You can also pipe Job objects directly from `Get-OrchJob`:

```powershell
Get-OrchJob -State Suspended | Resume-OrchJob
```

Primary Endpoint: POST /odata/Jobs/UiPath.Server.Configuration.OData.ResumeJob

OAuth required scopes: OR.Jobs or OR.Jobs.Write

Required permissions: Jobs.Edit

## EXAMPLES

### Example 1: Resume a single Suspended job by Key

```powershell
PS Orch1:\Shared> Resume-OrchJob -Key b8386273-f1a9-4804-af20-f0fce3eb3e42
```

Resumes the Suspended job with the specified Key.

### Example 2: Resume every Suspended job in the tenant

```powershell
PS Orch1:\> Get-OrchJob -State Suspended -Recurse | Resume-OrchJob
```

Pipes every Suspended job from across the tenant into Resume-OrchJob.

### Example 3: Preview without resuming

```powershell
PS Orch1:\Shared> Resume-OrchJob -Key b8386273-f1a9-4804-af20-f0fce3eb3e42 -WhatIf
```

Shows what would happen without actually resuming.

## PARAMETERS

### -Key

Specifies the uuid Key of the job(s) to resume. Tab completion suggests Suspended jobs in the current folder.

```yaml
Type: System.String[]
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

Accepts a Job object via the pipeline (e.g., from `Get-OrchJob`). The job's Key is used directly; folder context comes from the job's Path.

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

### System.String[]

Job Keys from the pipeline.

### UiPath.PowerShell.Entities.Job

Job objects from the pipeline (e.g., output of Get-OrchJob).

## OUTPUTS

### UiPath.PowerShell.Entities.Job

Returns the resumed Job object on success, with State transitioned to Resumed.

## NOTES

Only jobs in Suspended state can be resumed. Resumed (already-running-after-resume), Faulted, Stopped, Successful, or Pending jobs will be rejected by the server.

## RELATED LINKS

[Get-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJob.md)

[Restart-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Restart-OrchJob.md)

[Stop-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchJob.md)

[Start-OrchJob](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchJob.md)
