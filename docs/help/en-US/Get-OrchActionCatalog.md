---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchActionCatalog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchActionCatalog
---

# Get-OrchActionCatalog

## SYNOPSIS

Gets action catalogs from Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchActionCatalog [-Path <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets action catalog information from UiPath Orchestrator. Action catalogs define the available actions (tasks) that can be assigned to users for human-in-the-loop automation scenarios.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available action catalog names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TaskCatalogs

OAuth required scopes: OR.Tasks or OR.Tasks.Read

Required permissions: TaskCatalogs.View

## EXAMPLES

### Example 1: Get all action catalogs in the current folder

```powershell
PS Orch1:\Shared> Get-OrchActionCatalog
```

Gets all action catalogs from the current folder.

### Example 2: Get action catalogs by name

```powershell
PS Orch1:\Shared> Get-OrchActionCatalog xxx*
```

Gets action catalogs whose names match `xxx*` from the current folder.

### Example 3: Get action catalogs recursively

```powershell
PS Orch1:\> Get-OrchActionCatalog -Recurse
```

Gets all action catalogs from the current folder and all its subfolders.

### Example 4: Get action catalogs from a specific folder

```powershell
PS C:\> Get-OrchActionCatalog -Path Orch1:\Production
```

Gets all action catalogs from the Production folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

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

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the names of the action catalogs to retrieve. Supports wildcards. Tab completion dynamically suggests action catalog names from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe action catalog names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Bucket

Returns Bucket objects representing action catalogs with properties including Name and Id.

## NOTES

The cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders.

## RELATED LINKS

[Remove-OrchActionCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchActionCatalog.md)

[Copy-OrchActionCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchActionCatalog.md)
