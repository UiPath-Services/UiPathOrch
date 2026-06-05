---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProductVersion.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchProductVersion
---

# Get-OrchProductVersion

## SYNOPSIS

Gets the Orchestrator product version reported by one or more drives.

## SYNTAX

### __AllParameterSets

```
Get-OrchProductVersion [[-Path] <string[]>] [-LiteralPath <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns the Orchestrator product version information for one or more Orch drives. Useful for confirming which Orchestrator release a drive is talking to — for example, when troubleshooting version-specific behavior, validating a customer environment before reproducing an issue, or comparing versions across multiple environments in one command.

The cmdlet runs in parallel across the targeted drives and caches the result per drive, so repeated calls in the same session are cheap.

The -Path parameter selects which drive(s) to query. If omitted, all connected Orch drives are queried. Tab completion lists the available Orch drives.

Primary Endpoint: GET /api/Status/Version

OAuth required scopes: (none beyond the drive's existing authentication)

Required permissions: (none beyond the drive's existing authentication)

## EXAMPLES

### Example 1: Get the version of every connected Orch drive

```powershell
PS C:\> Get-OrchProductVersion
```

Returns one OrchProductVersion object per connected Orch drive. Useful for an at-a-glance summary across multiple environments.

### Example 2: Get the version of a specific drive

```powershell
PS C:\> Get-OrchProductVersion -Path Orch1:
```

Returns the OrchProductVersion for the Orch1: drive only.

### Example 3: Get the versions of several specific drives

```powershell
PS C:\> Get-OrchProductVersion -Path Orch1:,Orch2:
```

Returns the OrchProductVersion for the listed drives. -Path takes one or more
drive-qualified paths; drive names cannot be wildcarded (PowerShell interprets
-Path text as an item path, not a drive-name pattern). Omit -Path to target
every connected drive (see Example 1).

## PARAMETERS

### -Path

Specifies the Orch drive(s) to query. If not specified, all connected Orch drives are queried. Supports wildcards and comma-separated values. Tab completion lists the available Orch drives.

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

You can pipe drive name(s) to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.OrchProductVersion

Returns an OrchProductVersion object per targeted drive describing the Orchestrator product version. Use Get-Member on the output for the full property surface.

## NOTES

The version is cached per organization after the first query (one fetch is shared by all drives/tenants in the same org), so repeated calls in the same session do not re-hit the server. Use Clear-OrchCache to force a fresh fetch (e.g., after an in-place upgrade).

## RELATED LINKS

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)

[Import-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md)
