---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Edit-OrchConfig
---

# Edit-OrchConfig

## SYNOPSIS

Opens the UiPathOrchConfig.json configuration file in an editor.

## SYNTAX

### __AllParameterSets

```
Edit-OrchConfig [[-EditorType] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Opens the UiPathOrchConfig.json configuration file in a text editor for manual editing. This file defines the Orchestrator drives (PSDrives) that are mounted when the UiPathOrch module is imported.

On Windows, the cmdlet launches Notepad by default or the default application associated with .json files when EditorType is set to "Default". If the preferred editor fails, it falls back to the alternate option. On Linux, the cmdlet changes the current location to the directory containing the configuration file and displays a message prompting you to edit the file manually. Use `popd` to return to the previous location.

If the configuration file does not exist, the cmdlet creates a default configuration file before opening it.

After saving changes to the configuration file, restart the PowerShell session and run `Import-Module UiPathOrch` to apply the new settings and mount the configured Orchestrator tenants as PSDrives.

The -EditorType parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available editor types.

Primary Endpoint:

OAuth required scopes:

Required permissions:

## EXAMPLES

### Example 1: Open the configuration file with the default editor

```powershell
PS C:\> Edit-OrchConfig
```

Opens UiPathOrchConfig.json in Notepad (on Windows) or navigates to the configuration file directory (on Linux).

### Example 2: Open the configuration file with the system default application

```powershell
PS C:\> Edit-OrchConfig Default
```

Opens UiPathOrchConfig.json using the default application associated with .json files on Windows.

### Example 3: Open the configuration file with Notepad

```powershell
PS C:\> Edit-OrchConfig Notepad
```

Opens UiPathOrchConfig.json explicitly in Notepad on Windows.

## PARAMETERS

### -EditorType

Specifies the type of editor to use for opening the configuration file. On Windows, valid values are "Default" (uses the system's default .json file association) and "Notepad" (uses Notepad). Tab completion dynamically suggests available editor types based on the current platform.

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

On Linux, the cmdlet changes the current location to the configuration file directory. No object output is produced.

## NOTES

The configuration file path can be retrieved using the Get-OrchConfigPath cmdlet.

On Windows, if the preferred editor cannot be launched, the cmdlet automatically falls back to the alternate editor. On Linux, editors cannot be launched directly from the cmdlet; instead, the cmdlet navigates to the configuration file directory and prompts you to edit the file manually.

## RELATED LINKS

Get-OrchConfigPath
