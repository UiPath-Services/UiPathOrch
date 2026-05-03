---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmConfiguration.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-TmConfiguration
---

# Get-TmConfiguration

## SYNOPSIS

Gets the configuration of Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmConfiguration [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves the Test Manager configuration settings for the specified drives.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/configuration

## EXAMPLES

### Example 1: Get the Test Manager configuration

```powershell
PS Orch1Tm:\> Get-TmConfiguration
```

Gets the Test Manager configuration for the current drive.

### Example 2: Get the configuration from a specific drive

```powershell
PS C:\> Get-TmConfiguration Orch1Tm:
```

Gets the Test Manager configuration from the specified drive.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe drive names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmConfig

This cmdlet returns a TmConfig object representing the Test Manager configuration.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Ensure the configuration file includes "TM." scopes so that the PSDrive is automatically created.


## RELATED LINKS

[Get-TmServerInfo](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmServerInfo.md)
