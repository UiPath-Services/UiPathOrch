---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmProjectPermission.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmProjectPermission
---

# Get-TmProjectPermission

## SYNOPSIS

Gets the permissions of projects in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmProjectPermission [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves the project-level permissions for Test Manager projects.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/permissions/project

OAuth Required scopes: TM.Projects or TM.Projects.Read

Permission(s): ProjectSettings.Update

## EXAMPLES

### Example 1: Get permissions for the current project

```powershell
PS Orch1Tm:\MTP> Get-TmProjectPermission
```

Gets the permissions for the current Test Manager project.

### Example 2: Get permissions for a specific project

```powershell
PS C:\> Get-TmProjectPermission -Path Orch1Tm:\MTP
```

Gets the permissions for the specified Test Manager project.

### Example 3: Get permissions for all projects

```powershell
PS Orch1Tm:\> Get-TmProjectPermission -Recurse
```

Gets the permissions for all projects recursively.

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

### UiPath.PowerShell.Entities.TmProjectPermission

This cmdlet returns TmProjectPermission objects representing the permissions configured for Test Manager projects.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Ensure the configuration file includes "TM." scopes so that the PSDrive is automatically created.


## RELATED LINKS

[Get-TmProjectSetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmProjectSetting.md)
