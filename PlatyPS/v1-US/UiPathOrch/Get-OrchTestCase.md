---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestCase
---

# Get-OrchTestCase

## SYNOPSIS

Gets test case definitions from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestCase [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test case definition information from UiPath Orchestrator folders. A test case definition represents an automation workflow that has been published as a test case. Each test case has a Name and a PackageIdentifier that identifies the underlying package.

Results are ordered by PackageIdentifier and then by Name.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test case names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Folder processing is multi-threaded for improved performance when targeting multiple folders with -Recurse.

Primary Endpoint: GET /odata/TestCaseDefinitions

OAuth required scopes: OR.TestSets or OR.TestSets.Read

Required permissions: TestSets.View

## EXAMPLES

### Example 1: Get all test cases in the current folder

```powershell
PS Orch1:\Shared> Get-OrchTestCase
```

Gets all test case definitions from the current folder and returns their properties including Name, PackageIdentifier, and Id.

### Example 2: Get a test case by name

```powershell
PS Orch1:\Shared> Get-OrchTestCase TestCase.xaml
```

Gets the test case definitions named "TestCase.xaml" from the current folder. The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Get test cases by name with wildcards

```powershell
PS Orch1:\Shared> Get-OrchTestCase Test*
```

Gets all test case definitions whose name starts with "Test" from the current folder. The -Name parameter supports wildcards.

### Example 4: Get test cases from a specific folder

```powershell
PS C:\> Get-OrchTestCase -Path Orch1:\Production TestCase.xaml
```

Gets the test case definition named "TestCase.xaml" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get test cases recursively from all folders

```powershell
PS Orch1:\> Get-OrchTestCase -Recurse | Format-Table Name, PackageIdentifier
```

Gets all test case definitions from all folders recursively and displays them in a table format with selected properties.

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

### -Name

Specifies the Name of the test cases to be retrieved.

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

You can pipe test case names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestCaseDefinition

Returns TestCaseDefinition objects with properties including Name, PackageIdentifier, Id, and FolderPath.

## NOTES

Test cases are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspaces are excluded from enumeration.

## RELATED LINKS

Remove-OrchTestCase

Get-OrchTestCaseExecution

Get-OrchTestCaseAssertion

Get-OrchTestSet
