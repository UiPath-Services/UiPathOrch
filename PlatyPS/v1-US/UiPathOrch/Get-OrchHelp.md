---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchHelp
---

# Get-OrchHelp

## SYNOPSIS

Displays help and documentation information for the UiPathOrch module.

## SYNTAX

### __AllParameterSets

```
Get-OrchHelp [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Displays an overview of the UiPathOrch module documentation, including the module installation path, available LLM documentation files, PDF manuals, and essential commands for getting started.

The output includes file listings from the module's Docs directory, showing both text documentation files (for LLM consumption) and PDF manuals (for human reference), along with their file sizes.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Display module help information

```powershell
PS C:\> Get-OrchHelp
```

Displays the UiPathOrch module documentation overview, including the module path, available documentation files, and essential commands.

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.String

Returns a formatted string containing module documentation information, file listings, and quick-start guidance.

## NOTES

The documentation files are located in the Docs subdirectory of the module installation path. Text files (.txt) are designed for LLM consumption, while PDF files (.pdf) are for human reference.

## RELATED LINKS

Get-OrchPSDrive

Get-OrchConfigPath
