---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchConfigPath.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchConfigPath
---

# Get-OrchConfigPath

## SYNOPSIS

Gets the file path of the UiPathOrch module configuration file.

## SYNTAX

### __AllParameterSets

```
Get-OrchConfigPath [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns the file path to the UiPathOrch module configuration file. If the default configuration file does not exist, it is created automatically before the path is returned.

The configuration file stores drive definitions and other module settings that are loaded when the UiPathOrch module is imported.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Get the configuration file path

```powershell
PS C:\> Get-OrchConfigPath
```

Returns the full path to the UiPathOrch configuration file.

### Example 2: Open the configuration file in an editor

```powershell
PS C:\> notepad (Get-OrchConfigPath)
```

Opens the UiPathOrch configuration file in Notepad for editing.

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.String

Returns the full file path to the UiPathOrch configuration file as a string.

## NOTES

The configuration file is automatically created with default content if it does not already exist. This ensures that the module always has a valid configuration file to reference.

## RELATED LINKS

Get-OrchPSDrive

Set-OrchLocation
