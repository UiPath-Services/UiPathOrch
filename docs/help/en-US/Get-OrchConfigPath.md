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

Returns the file path to the UiPathOrch module configuration file that is currently in effect. If the default configuration file does not exist, it is created automatically before the path is returned.

The configuration file stores drive definitions and other module settings that are loaded when the UiPathOrch module is imported.

The location is the built-in per-user path unless the `UIPATHORCH_CONFIG_PATH` environment variable overrides it (see `Import-OrchConfig`). Use `-Verbose` to see which of the two the returned path came from. An overridden file is never created automatically — only the default location gets a template.

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

The configuration file at the default location is automatically created with default content if it does not already exist. This ensures that the module always has a valid configuration file to reference. A path supplied through `UIPATHORCH_CONFIG_PATH` is returned as-is whether or not the file exists, and is never created — that location is typically shared, and creating an empty configuration there would overwrite what other machines are pointed at.

## RELATED LINKS

[Import-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md)

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)

[Set-OrchLocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchLocation.md)
