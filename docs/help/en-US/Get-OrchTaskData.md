---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskData.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTaskData
---

# Get-OrchTaskData

## SYNOPSIS

Gets the data of action-center tasks: the form values and the action taken.

## SYNTAX

### FromCommandLine (Default)

```
Get-OrchTaskData [-Path <string[]>] [-LiteralPath <string[]>] [-Id] <long[]> [<CommonParameters>]
```

### FromPipeline

```
Get-OrchTaskData [-Path <string[]>] [-LiteralPath <string[]>] [-Task <OrchTask>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns what Get-OrchTask does not carry: the task's data, which holds the values of its form (attachments among them), and the action the task was completed with. The data comes back as an object whose properties are the form's fields, so `.Data.<FieldName>` reads a value.

The request is sent in the task's folder, which the endpoint requires. Pipe tasks from Get-OrchTask or Get-OrchTaskAcrossFolder to read each one in its own folder.

Primary Endpoint: GET /tasks/GenericTasks/GetTaskDataById

OAuth required scopes: OR.Tasks or OR.Tasks.Read

Required permissions: Tasks.View

## EXAMPLES

### Example 1: Read one task's data

```powershell
PS Orch1:\Shared> Get-OrchTaskData 8144232

   Directory: Orch1:\Shared

Id             : 8144232
Title          : Invoice approval
Type           : ExternalTask
Status         : Completed
Action         : Approve
Data           : {"Customer":"ACME","Amount":1200}
CompletionTime : 2026/11/05 10:12:44
```

### Example 2: The data of every completed task in the tenant

```powershell
PS Orch1:\> Get-OrchTaskAcrossFolder -Status Completed | Get-OrchTaskData |
              Select-Object Title, Action, @{n='Amount'; e={$_.Data.Amount}}
```

### Example 3: Export the form values to CSV

```powershell
PS Orch1:\Shared> Get-OrchTask -Status Completed | Get-OrchTaskData |
                    ForEach-Object { $_.Data } | Export-Csv C:\tasks.csv
```

## PARAMETERS

### -Id

Specifies the Id(s) of the task(s) to read. Tab completion suggests Task Ids from the current folder.

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

Accepts an OrchTask object via the pipeline (e.g., from Get-OrchTask or Get-OrchTaskAcrossFolder). The task's Path gives the folder.

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

Specifies the folder of the task(s). If not specified, the current folder is targeted.

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

### UiPath.PowerShell.Entities.OrchTaskData

Returns OrchTaskData objects with Id, Key, Title, Type, Priority, Status, Action, Data, TaskAssignmentCriteria, AssignedToUserId, ClaimedByUserId, OrganizationUnitId, OrganizationUnitFullyQualifiedName, ExternalTag, CreatorJobKey, WaitJobKey, CreationTime, LastModificationTime and CompletionTime. The default view shows Data as compact JSON.

## NOTES

The same endpoint answers for every task type; this was checked with form tasks and external tasks.

An attachment is a field value inside Data; its shape depends on the form.

## RELATED LINKS

[Get-OrchTask](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTask.md)

[Get-OrchTaskAcrossFolder](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskAcrossFolder.md)
