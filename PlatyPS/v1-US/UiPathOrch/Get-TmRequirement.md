---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmRequirement
---

# Get-TmRequirement

## SYNOPSIS

Gets the requirements from projects in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmRequirement [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves requirements from Test Manager projects.
Results are filtered by the `-Name` parameter using wildcard matching and returned sorted by object key.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/requirements

OAuth Required scopes: TM.Requirements or TM.Requirements.Read

Permission(s): Requirement.Read

## EXAMPLES

### Example 1: Get all requirements in the current project

```powershell
PS Orch1Tm:\MTP> Get-TmRequirement
```

Gets all requirements from the current Test Manager project.

### Example 2: Get requirements by name with wildcards

```powershell
PS Orch1Tm:\MTP> Get-TmRequirement 要*
```

Gets requirements whose name starts with "要" from the current project.

### Example 3: Get requirements from a specific project

```powershell
PS C:\> Get-TmRequirement -Path Orch1Tm:\MTP -Recurse
```

Gets all requirements from the specified project and all its subfolders.

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

### -Name

Specifies the name of the requirements to be retrieved.
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

You can pipe requirement names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmRequirement

This cmdlet returns TmRequirement objects representing Test Manager requirements.

## NOTES



## RELATED LINKS

Remove-TmRequirement

Get-TmTestCase
