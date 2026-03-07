---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLogLocation
---

# Get-OrchLogLocation

## SYNOPSIS

Gets the log folder path for an Orchestrator drive.

## SYNTAX

### __AllParameterSets

```
Get-OrchLogLocation [[-Path] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the file system path of the log folder for the specified Orchestrator drive. If the drive-specific log path cannot be determined, the base log folder path is returned instead.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Get the log location for the current drive

```powershell
PS Orch1:\> Get-OrchLogLocation
```

Gets the log folder path for the current Orchestrator drive.

### Example 2: Get the log location for a specific drive

```powershell
PS C:\> Get-OrchLogLocation Orch1:\
```

Gets the log folder path for the Orch1 drive.

## PARAMETERS

### -Path

Specifies the Orchestrator drive to get the log location for. If not specified, the current drive is used.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
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

## OUTPUTS

### System.String

Returns the file system path of the log folder.

## NOTES

This cmdlet does not make any API calls. It returns the local file system path where logs are stored for the specified drive.

## RELATED LINKS

Open-OrchLogLocation
