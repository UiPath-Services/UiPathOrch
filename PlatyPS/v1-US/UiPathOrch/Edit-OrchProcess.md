---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Edit-OrchProcess
---

# Edit-OrchProcess

## SYNOPSIS

Opens the process edit page in the default web browser.

## SYNTAX

### __AllParameterSets

```
Edit-OrchProcess [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Opens the UiPath Orchestrator process edit page in the default web browser. The cmdlet resolves the process by name, constructs the edit URL using the format `{base_url}/orchestrator_/processes/{release.Id}/edit?fid={folder.Id}`, and launches the browser.

This cmdlet does not modify the process. It provides a quick way to navigate to the process settings page in the Orchestrator web interface.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual process names in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path parameter, place it immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: N/A (opens browser URL)

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View

## EXAMPLES

### Example 1: Open the edit page for a specific process

```powershell
PS Orch1:\Shared> Edit-OrchProcess BlankProcess19
```

Opens the edit page for the process named "BlankProcess19" in the current folder using the default web browser.

### Example 2: Open the edit page using a wildcard

```powershell
PS Orch1:\Shared> Edit-OrchProcess Blank*
```

Opens the edit page for all processes matching the wildcard pattern "Blank*" in the current folder. Each matching process opens in a separate browser tab.

### Example 3: Open the edit page from a specific folder

```powershell
PS C:\> Edit-OrchProcess -Path Orch1:\Shared BlankProcess19
```

Opens the edit page for the process named "BlankProcess19" in the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the processes to open in the browser. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

You can pipe process names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce any output. It opens the process edit page in the default web browser.

## NOTES

This cmdlet requires an active connection to an Orchestrator instance via an Orch: drive. The browser URL is constructed from the Orchestrator base URL and the resolved process (release) ID and folder ID.

If multiple processes match the -Name wildcard pattern, each one opens in a separate browser tab.

## RELATED LINKS

Get-OrchProcess

Update-OrchProcess
