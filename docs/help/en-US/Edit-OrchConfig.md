---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Edit-OrchConfig.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Edit-OrchConfig
---

# Edit-OrchConfig

## SYNOPSIS

Opens the UiPathOrchConfig.json configuration file in an editor.

## SYNTAX

### __AllParameterSets

```
Edit-OrchConfig [-UseDefaultEditor] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Opens the UiPathOrchConfig.json configuration file in a text editor for manual editing. This file defines the Orchestrator drives (PSDrives) that are mounted when the UiPathOrch module is imported.

On Windows, the cmdlet launches Notepad by default. Specify -UseDefaultEditor to launch the application associated with .json files instead (Notepad is then used as a fallback if the association launch fails).

On Linux and macOS, the cmdlet does not launch an editor. It changes the current location to the directory containing the configuration file and prints a warning prompting you to edit the file manually. The -UseDefaultEditor switch has no effect on these platforms. Use `popd` to return to the previous location after saving.

If the configuration file does not exist, the cmdlet creates a default configuration file before opening it.

After saving changes to the configuration file, run `Import-OrchConfig` to reload the configuration and mount the configured Orchestrator tenants as PSDrives.

## EXAMPLES

### Example 1: Open the configuration file with Notepad (Windows default)

```powershell
PS C:\> Edit-OrchConfig
```

Opens UiPathOrchConfig.json in Notepad on Windows. On Linux/macOS, changes the current location to the configuration file's directory and prints an edit-then-Import-OrchConfig prompt.

### Example 2: Open with the .json file association

```powershell
PS C:\> Edit-OrchConfig -UseDefaultEditor
```

On Windows, opens UiPathOrchConfig.json using the application associated with .json files (for example, Visual Studio Code if it is registered as the .json handler). Falls back to Notepad if the association launch fails.

On Linux/macOS, this switch is silently ignored; the cmdlet behaves the same as Example 1 on those platforms.

## PARAMETERS

### -UseDefaultEditor

Windows only. Launches the application associated with .json files instead of Notepad. If the association launch fails, the cmdlet falls back to Notepad. Has no effect on Linux or macOS.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
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

On Linux and macOS, the cmdlet changes the current location to the configuration file directory but does not write any object output.

## NOTES

The configuration file path can be retrieved using the Get-OrchConfigPath cmdlet.

The obsolete -EditorType parameter was removed. Its only practical use was `-EditorType Default`, which is now `-UseDefaultEditor`. Any other value (including "Notepad") behaved identically to passing no argument and is replaced by simply omitting the switch.

## RELATED LINKS

[Import-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md)

[Get-OrchConfigPath](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchConfigPath.md)
