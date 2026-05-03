---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUpdateSetting.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchUpdateSetting
---

# Get-OrchUpdateSetting

## SYNOPSIS

Gets the update settings from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchUpdateSetting [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the update settings from UiPath Orchestrator. Update settings define the update server configuration and package management settings used by the Orchestrator instance.

The cmdlet uses multi-threaded processing to retrieve update settings from multiple drives in parallel.

Update settings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetUpdateSettings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get update settings

```powershell
PS Orch1:\> Get-OrchUpdateSetting
```

Gets the update settings from the current Orchestrator tenant.

### Example 2: Get update settings from a specific drive

```powershell
PS C:\> Get-OrchUpdateSetting Orch1:\
```

Gets the update settings from the Orch1 tenant.

### Example 3: Get update settings from multiple drives

```powershell
PS C:\> Get-OrchUpdateSetting Orch1:\,Orch2:\
```

Gets the update settings from both Orch1 and Orch2 tenants.

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

You can pipe drive paths to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.UpdateSettings

Returns an UpdateSettings object representing the update settings configuration for the tenant.

## NOTES

Update settings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve update settings from multiple drives in parallel.

## RELATED LINKS

[Get-OrchSetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchSetting.md)

[Get-OrchActivitySetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchActivitySetting.md)
