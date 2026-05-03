---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchActivitySetting.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchActivitySetting
---

# Get-OrchActivitySetting

## SYNOPSIS

Gets the activity settings from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchActivitySetting [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the activity settings configuration from UiPath Orchestrator. Activity settings define the secure activity types and their configurations used in automation workflows.

The cmdlet uses multi-threaded processing to retrieve activity settings from multiple drives in parallel.

Activity settings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetActivitySettings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get activity settings

```powershell
PS Orch1:\> Get-OrchActivitySetting
```

Gets the activity settings from the current Orchestrator tenant.

### Example 2: Get activity settings from a specific drive

```powershell
PS C:\> Get-OrchActivitySetting Orch1:\
```

Gets the activity settings from the Orch1 tenant.

### Example 3: Get activity settings from multiple drives

```powershell
PS C:\> Get-OrchActivitySetting Orch1:\,Orch2:\
```

Gets the activity settings from both Orch1 and Orch2 tenants.

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

### UiPath.PowerShell.Entities.ActivitySettings

Returns an ActivitySettings object representing the activity settings configuration for the tenant.

## NOTES

Activity settings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve activity settings from multiple drives in parallel.

## RELATED LINKS

Get-OrchSetting

Get-OrchExecutionSetting

Get-OrchWebSetting
