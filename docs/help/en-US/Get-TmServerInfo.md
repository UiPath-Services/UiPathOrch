---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmServerInfo.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmServerInfo
---

# Get-TmServerInfo

## SYNOPSIS

Gets the server information of Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmServerInfo [[-Path] <string[]>] [-LiteralPath <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves the Test Manager server information, such as version and build details, for the specified drives.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/serverinfo

OAuth required scopes: (Test Manager API - no per-endpoint scopes)

## EXAMPLES

### Example 1: Get the Test Manager server information

```powershell
PS Orch1Tm:\> Get-TmServerInfo
```

Gets the Test Manager server information for the current drive.

### Example 2: Get the server information from a specific drive

```powershell
PS C:\> Get-TmServerInfo Orch1Tm:
```

Gets the Test Manager server information from the specified drive.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

You can pipe drive names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmServerInfo

This cmdlet returns a TmServerInfo object containing Test Manager server details such as version information.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Returns server metadata such as version and build details.


## RELATED LINKS

[Get-TmConfiguration](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmConfiguration.md)
