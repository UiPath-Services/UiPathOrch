---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebSetting.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchWebSetting
---

# Get-OrchWebSetting

## SYNOPSIS

Gets the web settings from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchWebSetting [-Path <string[]>] [-LiteralPath <string[]>] [[-Key] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the web setting information from UiPath Orchestrator. Web settings define the client-side configuration used by the Orchestrator web application.

The -Key parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available web setting keys. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet uses multi-threaded processing to retrieve web settings from multiple drives in parallel.

Web settings are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get all web settings

```powershell
PS Orch1:\> Get-OrchWebSetting
```

Gets all web settings from the current Orchestrator tenant.

### Example 2: Get web settings by key

```powershell
PS Orch1:\> Get-OrchWebSetting *Color*
```

Gets web settings whose keys match '*Color*'.

### Example 3: Get web settings from multiple drives

```powershell
PS C:\> Get-OrchWebSetting -Path Orch1:\,Orch2:\
```

Gets all web settings from both Orch1 and Orch2 tenants.

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

### -Key

Specifies the keys of the web settings to retrieve. Supports wildcards. Tab completion dynamically suggests web setting keys from the target drives.

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

You can pipe web setting keys to this cmdlet via the Key property.

## OUTPUTS

### UiPath.PowerShell.Entities.ResponseDictionaryItem

Returns ResponseDictionaryItem objects representing the web settings with Key and Value properties.

## NOTES

Web settings are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The cmdlet uses multi-threaded processing to retrieve web settings from multiple drives in parallel.

## RELATED LINKS

[Get-OrchSetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchSetting.md)

[Get-OrchAuthenticationSetting](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAuthenticationSetting.md)
