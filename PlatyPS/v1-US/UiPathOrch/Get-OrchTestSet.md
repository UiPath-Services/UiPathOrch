---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestSet
---

# Get-OrchTestSet

## SYNOPSIS

Gets test sets from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestSet [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test set information from UiPath Orchestrator folders. A test set is a collection of test cases that can be executed together. Test sets are folder-scoped entities and are retrieved from the current folder unless -Path is specified.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test set names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TestSets?$filter=(SourceType eq 'User')&$expand=Environment

OAuth required scopes: OR.TestSets or OR.TestSets.Read

Required permissions: TestSets.View

## EXAMPLES

### Example 1: Get all test sets in the current folder

```powershell
PS Orch1:\Shared> Get-OrchTestSet
```

Gets all test sets from the current folder.

### Example 2: Get test sets by name with wildcards

```powershell
PS Orch1:\Shared> Get-OrchTestSet TestSet*
```

Gets all test sets whose name starts with "TestSet" from the current folder. The -Name parameter is positional (position 0) and supports wildcards.

### Example 3: Get a specific test set from a specific folder

```powershell
PS C:\> Get-OrchTestSet -Path Orch1:\Shared TestSet-TestProject-Migration-001
```

Gets the test set named "TestSet-TestProject-Migration-001" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 4: Get test sets recursively from all folders

```powershell
PS Orch1:\> Get-OrchTestSet -Recurse | Format-Table Name, Description, PackageCount
```

Gets all test sets from all folders recursively and displays them in a table format with selected properties.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

Specifies the names of the test sets to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests test set names from the target folders.

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

You can pipe test set names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSet

Returns TestSet objects with properties including Name, Description, Id, and PackageCount.

## NOTES

Test sets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

## RELATED LINKS

Remove-OrchTestSet

Start-OrchTestSet

Copy-OrchTestSet
