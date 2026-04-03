---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchSetting
---

# Get-OrchSetting

## SYNOPSIS

Gets general settings from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchSetting [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets general setting information from UiPath Orchestrator. Settings control the behavior and configuration of the Orchestrator instance (e.g., deployment, security, and other system-wide settings).

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available setting names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet uses multi-threaded processing to retrieve settings from multiple drives in parallel.

Settings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get all settings

```powershell
PS Orch1:\> Get-OrchSetting
```

Gets all general settings from the current Orchestrator tenant.

### Example 2: Get settings by name

```powershell
PS Orch1:\> Get-OrchSetting Deployment*
```

Gets settings whose names match 'Deployment*'.

### Example 3: Get settings from multiple drives

```powershell
PS C:\> Get-OrchSetting -Path Orch1:\,Orch2:\
```

Gets all general settings from both Orch1 and Orch2 tenants.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

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

### -Name

Specifies the names of the settings to retrieve. Supports wildcards. Tab completion dynamically suggests setting names from the target drives.

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

You can pipe setting names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Settings

Returns Settings objects representing the general settings with properties including Name and Value.

## NOTES

Settings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve settings from multiple drives in parallel.

## RELATED LINKS

Get-OrchActivitySetting

Get-OrchAuthenticationSetting

Get-OrchExecutionSetting

Get-OrchWebSetting

Get-OrchUpdateSetting
