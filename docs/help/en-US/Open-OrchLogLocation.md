---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Open-OrchLogLocation.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Open-OrchLogLocation
---

# Open-OrchLogLocation

## SYNOPSIS

Opens the UiPathOrch module log folder location.

## SYNTAX

### __AllParameterSets

```
Open-OrchLogLocation [[-Path] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Opens the log folder location for the UiPathOrch module. On Windows, the cmdlet opens the log folder in File Explorer. On Linux, the cmdlet changes the current location to the log folder directory.

The log folder path is determined from the drive's API session configuration. If the drive cannot be resolved, the default log folder base path is used.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available drives.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Open the log location for the current drive

```powershell
PS Orch1:\> Open-OrchLogLocation
```

Opens the log folder for the current Orchestrator drive in File Explorer (Windows) or changes to the log directory (Linux).

### Example 2: Open the log location for a specific drive

```powershell
PS C:\> Open-OrchLogLocation Orch1:
```

Opens the log folder for the Orch1: drive.

## PARAMETERS

### -Path

Specifies the name of the target drive to retrieve the log folder path from.
If not specified, the current drive will be used.

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

### None

This cmdlet does not produce pipeline output. On Windows, it opens File Explorer. On Linux, it changes the current location.

## NOTES

On Windows, the cmdlet launches explorer.exe to open the log folder. On Linux, the current location is pushed to the location stack before changing to the log directory, allowing you to return with Pop-Location. Other platforms display a warning that the operation is not supported.

## RELATED LINKS

Get-OrchPSDrive

Get-OrchConfigPath
