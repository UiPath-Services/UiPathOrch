---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
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
Get-TmServerInfo [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Retrieves the Test Manager server information, such as version and build details, for the specified drives.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/serverinfo

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

Get-TmConfiguration
