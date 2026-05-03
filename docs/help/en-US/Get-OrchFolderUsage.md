---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderUsage.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchFolderUsage
---

# Get-OrchFolderUsage

## SYNOPSIS

Gets entity usage summaries for UiPath Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchFolderUsage [[-Path] <string[]>] [-Recurse] [-Depth <uint>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets entity usage summaries for specified Orchestrator folders. The cmdlet returns EntitySummary objects categorized as either DeletableEntity (entities that can be deleted from the folder) or StoppableJob (running jobs that can be stopped).

This cmdlet is useful for understanding what resources exist within a folder before performing operations such as removing a personal workspace or deleting a folder.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetEntitiesSummary

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: Units.View

## EXAMPLES

### Example 1: Get folder usage for the current folder

```powershell
PS Orch1:\Shared> Get-OrchFolderUsage
```

Gets the entity usage summary for the current folder.

### Example 2: Get folder usage for a specific path

```powershell
PS C:\> Get-OrchFolderUsage Orch1:\Shared
```

Gets the entity usage summary for the Shared folder on the Orch1: drive.

### Example 3: Get folder usage recursively

```powershell
PS Orch1:\> Get-OrchFolderUsage -Recurse
```

Gets entity usage summaries for the current folder and all subfolders recursively.

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
  Position: 0
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe folder paths to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.EntitySummary

Returns EntitySummary objects with a Category property indicating whether the entity is a DeletableEntity or a StoppableJob, along with the Path of the folder it belongs to.

## NOTES

This cmdlet queries each folder sequentially (single-threaded) to ensure stable results from the server.

## RELATED LINKS

Remove-OrchPersonalWorkspace

Get-OrchPersonalWorkspace
