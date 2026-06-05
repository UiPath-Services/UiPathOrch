---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTask.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTask
---

# Get-OrchTask

## SYNOPSIS

Gets action-center tasks from a folder.

## SYNTAX

### __AllParameterSets

```
Get-OrchTask [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Title] <string[]>]
 [-Priority <string[]>] [-Status <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns action-center tasks (FormTask, ExternalTask, DocumentValidationTask, DocumentClassificationTask, DataLabelingTask, AppTask) from the specified folder. Supports wildcard filtering on Title, plus exact-match filters on Status and Priority.

The folder-scoped /odata/Tasks endpoint returns tasks visible to the caller within the folder context (typically tasks assigned to the current user, or all tasks if the caller has admin permission). For a tenant-wide admin view that includes Unassigned tasks regardless of folder permissions, use Get-OrchTaskAcrossFolder instead.

Primary Endpoint: GET /odata/Tasks

OAuth required scopes: OR.Tasks or OR.Tasks.Read

Required permissions: Tasks.View

## EXAMPLES

### Example 1: Get all tasks in the current folder

```powershell
PS Orch1:\Shared> Get-OrchTask
```

Returns every task in the Shared folder visible to the caller.

### Example 2: Filter by title and status

```powershell
PS Orch1:\Shared> Get-OrchTask -Title 'Invoice*' -Status Pending
```

Gets Pending tasks whose Title starts with "Invoice".

### Example 3: Recurse with priority filter

```powershell
PS Orch1:\> Get-OrchTask -Path Orch1:\ -Recurse -Priority High, Critical
```

Gets High and Critical tasks across every subfolder of the Orch1 drive.

## PARAMETERS

### -Title

Specifies the task title(s) to match. Supports wildcards and multiple comma-separated values.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Status

Filters by task status. Valid values: Unassigned, Pending, Completed.

```yaml
Type: System.String[]
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
- Unassigned
- Pending
- Completed
HelpMessage: ''
```

### -Priority

Filters by task priority. Valid values: Low, Medium, High, Critical.

```yaml
Type: System.String[]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe Title, Status, or Priority values to this cmdlet via property name binding.

## OUTPUTS

### UiPath.PowerShell.Entities.OrchTask

Returns OrchTask objects with properties including Id, Key, Title, Type, Priority, Status, Action, AssignedToUserId, TaskAssigneeName, TaskCatalogName, OrganizationUnitId, CreationTime, LastModificationTime, CompletionTime, IsCompleted, IsDeleted, and Tags.

## NOTES

The C# entity class is named OrchTask (not Task) to avoid clash with System.Threading.Tasks.Task; the cmdlet noun is still "OrchTask".

The /odata/Tasks endpoint returns only tasks the caller can see within the folder. For an admin's tenant-wide view, use Get-OrchTaskAcrossFolder.

## RELATED LINKS

[Get-OrchTaskAcrossFolder](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskAcrossFolder.md)

[Set-OrchTask](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchTask.md)

[Remove-OrchTask](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTask.md)
