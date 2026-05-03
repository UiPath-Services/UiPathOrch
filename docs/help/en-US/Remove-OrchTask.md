---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTask.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchTask
---

# Remove-OrchTask

## SYNOPSIS

Deletes action-center tasks by Id.

## SYNTAX

### FromCommandLine (Default)

```
Remove-OrchTask [-Id] <long[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### FromPipeline

```
Remove-OrchTask [-Task <OrchTask>] [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Deletes one or more action-center tasks. Pipeline-friendly with Get-OrchTask:

```powershell
Get-OrchTask -Status Completed | Remove-OrchTask
```

The -Id parameter supports tab completion (Press [Ctrl+Space] or [Tab] to see Task Ids in the current folder, with Status/Priority/Title shown as a tooltip).

Primary Endpoint: DELETE /odata/Tasks({key})

OAuth required scopes: OR.Tasks or OR.Tasks.Write

Required permissions: Tasks.Delete

## EXAMPLES

### Example 1: Delete a single task

```powershell
PS Orch1:\Shared> Remove-OrchTask -Id 8144232
```

Deletes the task with the specified Id from the current folder, prompting for confirmation.

### Example 2: Bulk-delete completed tasks via pipeline

```powershell
PS Orch1:\> Get-OrchTaskAcrossFolder -Status Completed |
              Remove-OrchTask -Confirm:$false
```

Pipes every Completed task in the tenant into Remove-OrchTask without prompting.

### Example 3: Preview without deleting

```powershell
PS Orch1:\Shared> Remove-OrchTask -Id 8144232 -WhatIf
```

Shows what would happen without actually deleting the task.

## PARAMETERS

### -Id

Specifies the Int64 Id(s) of the task(s) to delete. Tab completion suggests Task Ids from the current folder.

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

### -Task

Accepts an OrchTask object via the pipeline (e.g., from Get-OrchTask). The task's Path is used for folder context.

```yaml
Type: UiPath.PowerShell.Entities.OrchTask
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

Task Ids from the pipeline.

### UiPath.PowerShell.Entities.OrchTask

OrchTask objects from the pipeline (e.g., output of Get-OrchTask).

## OUTPUTS

### System.Object

This cmdlet does not emit objects on success. Errors surface as ErrorRecord via the standard error stream.

## NOTES

Deleted tasks are typically marked IsDeleted=true on the server side rather than physically removed; check the tenant's retention policy if you need permanent deletion.

## RELATED LINKS

Get-OrchTask

Get-OrchTaskAcrossFolder

Set-OrchTask
