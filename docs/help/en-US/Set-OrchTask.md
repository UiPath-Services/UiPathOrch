---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchTask.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Set-OrchTask
---

# Set-OrchTask

## SYNOPSIS

Updates Title, Priority, Note, or TaskCatalog association on existing tasks.

## SYNTAX

### __AllParameterSets

```
Set-OrchTask [-Path <string[]>] [-Id] <long[]> [-Confirm] [-NoteText <string>]
 [-Priority <string>] [-Task <OrchTask>] [-TaskCatalog <string>] [-Title <string>]
 [-UnsetTaskCatalog] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Edits the metadata of one or more existing action-center tasks. Only the fields you specify are updated; omitted fields are left untouched. Pipeline-friendly with Get-OrchTask:

```powershell
Get-OrchTask -Status Pending -Title 'Invoice*' | Set-OrchTask -Priority High
```

The -Id parameter supports tab completion (Press [Ctrl+Space] or [Tab] to see Task Ids in the current folder, with Status/Priority/Title shown as a tooltip). The -TaskCatalog parameter also tab-completes against catalog names in the current folder.

To clear an existing catalog assignment, pass `-UnsetTaskCatalog`. -UnsetTaskCatalog is mutually exclusive with -TaskCatalog.

Primary Endpoint: POST /odata/Tasks/UiPath.Server.Configuration.OData.EditTaskMetadata

OAuth required scopes: OR.Tasks or OR.Tasks.Write

Required permissions: Tasks.Edit

## EXAMPLES

### Example 1: Bump priority on a single task

```powershell
PS Orch1:\Shared> Set-OrchTask -Id 8144232 -Priority High
```

Sets the task's Priority to High; Title, NoteText, and TaskCatalog are unchanged.

### Example 2: Rename and add a note in one call

```powershell
PS Orch1:\Shared> Set-OrchTask -Id 8144232 -Title 'Invoice approval (urgent)' -NoteText 'Escalated by finance.'
```

Renames the task and attaches a note describing the change.

### Example 3: Bulk-update priority via pipeline

```powershell
PS Orch1:\> Get-OrchTaskAcrossFolder -Status Pending |
              Set-OrchTask -Priority High -Confirm:$false
```

Pipes every Pending task in the tenant into Set-OrchTask.

### Example 4: Disassociate a catalog

```powershell
PS Orch1:\Shared> Set-OrchTask -Id 8144232 -UnsetTaskCatalog
```

Clears the task's TaskCatalog association.

## PARAMETERS

### -Id

Specifies the Int64 Id(s) of the task(s) to update. Tab completion suggests Task Ids from the current folder.

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

### -Task

Accepts an OrchTask object via the pipeline (e.g., from Get-OrchTask). The task's Path is used for folder context.

```yaml
Type: UiPath.PowerShell.Entities.OrchTask
DefaultValue: None
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

### -Title

New title text. Maximum 512 characters.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -Priority

New priority. Valid values: Low, Medium, High, Critical.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- Low
- Medium
- High
- Critical
HelpMessage: ''
```

### -NoteText

A note recorded with this edit. Maximum 512 characters.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -TaskCatalog

Name of the action catalog to associate with the task. Tab completion suggests catalog names from the current folder.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -UnsetTaskCatalog

Clears any existing TaskCatalog association on the task. Mutually exclusive with -TaskCatalog.

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

This cmdlet does not emit objects on success; the underlying API returns 200 with no body. Errors surface as ErrorRecord via the standard error stream.

## NOTES

The /odata/Tasks endpoint behaves with caller-scoped visibility, so the cache populated by Get-OrchTask might not contain Unassigned tasks visible only to admins. The -Id parameter still works directly against the API even when Tab completion shows nothing — pass the Id manually (e.g., from Get-OrchTaskAcrossFolder).

## RELATED LINKS

[Get-OrchTask](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTask.md)

[Get-OrchTaskAcrossFolder](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskAcrossFolder.md)

[Remove-OrchTask](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTask.md)
