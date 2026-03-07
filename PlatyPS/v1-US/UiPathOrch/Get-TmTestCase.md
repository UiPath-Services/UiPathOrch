---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmTestCase
---

# Get-TmTestCase

## SYNOPSIS

Gets the test cases from projects in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmTestCase [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves test cases from Test Manager projects.
Results are filtered by the `-Name` parameter using wildcard matching and returned sorted by object key.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/testcases

OAuth Required scopes: TM.TestCases or TM.TestCases.Read

Permission(s): TestCase.Read

## EXAMPLES

### Example 1: Get all test cases in the current project

```powershell
PS Orch1Tm:\MTP> Get-TmTestCase
```

Gets all test cases from the current Test Manager project.

### Example 2: Get test cases by name with wildcards

```powershell
PS Orch1Tm:\MTP> Get-TmTestCase 手動*
```

Gets test cases whose name starts with "手動" from the current project.

### Example 3: Get test cases from a specific project

```powershell
PS C:\> Get-TmTestCase -Path Orch1Tm:\MTP -Recurse
```

Gets all test cases from the specified project and all its subfolders.

## PARAMETERS

### -Path

Specifies the target folder.
If not specified, the current folder will be targeted.

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

Specifies that the operation should include the target folder and all its subfolders.

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

### -Name

Specifies the name of the test cases to be retrieved.
Wildcard characters are permitted.

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

You can pipe test case names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmTestCase

This cmdlet returns TmTestCase objects representing Test Manager test cases.

## NOTES



## RELATED LINKS

Remove-TmTestCase

Get-TmRequirement

Get-TmTestSet
