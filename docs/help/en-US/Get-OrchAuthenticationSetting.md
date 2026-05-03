---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAuthenticationSetting.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchAuthenticationSetting
---

# Get-OrchAuthenticationSetting

## SYNOPSIS

Gets authentication settings from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchAuthenticationSetting [[-Key] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets authentication setting information from UiPath Orchestrator. Authentication settings define the authentication and security configuration for the Orchestrator instance (e.g., password complexity, lockout policies, external identity providers).

The -Key parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available authentication setting keys. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet uses multi-threaded processing to retrieve authentication settings from multiple drives in parallel.

Authentication settings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get all authentication settings

```powershell
PS Orch1:\> Get-OrchAuthenticationSetting
```

Gets all authentication settings from the current Orchestrator tenant.

### Example 2: Get authentication settings by key

```powershell
PS Orch1:\> Get-OrchAuthenticationSetting Auth.*
```

Gets authentication settings whose keys match `Auth.*`.

### Example 3: Get authentication settings from multiple drives

```powershell
PS C:\> Get-OrchAuthenticationSetting -Path Orch1:\,Orch2:\
```

Gets all authentication settings from both Orch1 and Orch2 tenants.

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

### -Key

Specifies the keys of the authentication settings to retrieve. Supports wildcards. Tab completion dynamically suggests authentication setting keys from the target drives.

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

You can pipe authentication setting keys to this cmdlet via the Key property.

## OUTPUTS

### UiPath.PowerShell.Entities.ResponseDictionaryItem

Returns ResponseDictionaryItem objects representing the authentication settings with Key and Value properties.

## NOTES

Authentication settings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve authentication settings from multiple drives in parallel.

## RELATED LINKS

[Get-OrchSetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchSetting.md)

[Get-OrchWebSetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebSetting.md)
