---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmProjectSetting
---

# Get-TmProjectSetting

## SYNOPSIS

Gets the settings of projects in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmProjectSetting [-Path <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves the project settings for Test Manager projects.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/projectsettings

OAuth Required scopes: TM.Projects or TM.Projects.Read

Permission(s): Project.Read

## EXAMPLES

### Example 1: Get settings for the current project

```powershell
PS Orch1Tm:\MTP> Get-TmProjectSetting
```

Gets the project settings for the current Test Manager project.

### Example 2: Get settings for a specific project

```powershell
PS C:\> Get-TmProjectSetting -Path Orch1Tm:\MTP
```

Gets the project settings for the specified Test Manager project.

### Example 3: Get settings for all projects

```powershell
PS Orch1Tm:\> Get-TmProjectSetting -Recurse
```

Gets the project settings for all projects recursively.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe project paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmProjectSettings

This cmdlet returns a TmProjectSettings object representing the settings for a Test Manager project.

## NOTES



## RELATED LINKS

Get-TmProjectPermission
