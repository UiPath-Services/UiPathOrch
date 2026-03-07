---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmTestExecution
---

# Get-TmTestExecution

## SYNOPSIS

Gets the test executions from projects in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmTestExecution [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves test executions from Test Manager projects.
Results are filtered by the `-Name` parameter using wildcard matching and returned in alphabetical order by name.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/testexecutions

OAuth Required scopes: TM.TestSetExecutions or TM.TestSetExecutions.Read

Permission(s): TestSetExecution.Read

## EXAMPLES

### Example 1: Get all test executions in the current project

```powershell
PS Orch1Tm:\SKK> Get-TmTestExecution
```

Gets all test executions from the current Test Manager project.

### Example 2: Get test executions by name with wildcards

```powershell
PS Orch1Tm:\SKK> Get-TmTestExecution OCテスト*
```

Gets test executions whose name starts with "OCテスト" from the current project.

### Example 3: Get test executions from a specific project

```powershell
PS C:\> Get-TmTestExecution -Path Orch1Tm:\SKK -Recurse
```

Gets all test executions from the specified project and all its subfolders.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Name

Specifies the name of the test executions to be retrieved.
Wildcard characters are permitted.

```yaml
Type: System.String[]
DefaultValue: ''
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

You can pipe test execution names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmTestExecution

This cmdlet returns TmTestExecution objects representing Test Manager test executions.

## NOTES



## RELATED LINKS

Get-TmTestSet

Get-TmTestCase
