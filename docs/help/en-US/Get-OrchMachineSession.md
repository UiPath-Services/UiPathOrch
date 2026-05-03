---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachineSession.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchMachineSession
---

# Get-OrchMachineSession

## SYNOPSIS

Gets machine runtime sessions from Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchMachineSession [[-Status] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets machine runtime session information from UiPath Orchestrator. Machine sessions show the runtime status of machines connected to specific folders, including their availability status.

The -Status parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available status values. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimesByFolderId(folderId={folderId})

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Machines.View or Jobs.Create

## EXAMPLES

### Example 1: Get all machine sessions in the current folder

```powershell
PS Orch1:\Shared> Get-OrchMachineSession
```

Gets all machine runtime sessions from the current Orchestrator folder.

### Example 2: Get sessions by status

```powershell
PS Orch1:\Shared> Get-OrchMachineSession Available
```

Gets machine sessions with 'Available' status from the current folder.

### Example 3: Get sessions recursively

```powershell
PS Orch1:\> Get-OrchMachineSession -Recurse
```

Gets all machine sessions from the current folder and all its subfolders.

### Example 4: Get sessions from a specific folder

```powershell
PS C:\> Get-OrchMachineSession -Path Orch1:\Production
```

Gets machine sessions from the Production folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
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

### -Status

Specifies the status of sessions to retrieve. Supports wildcards. Tab completion suggests available status values.

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

You can pipe the Status and Path values to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSessionRuntime

Returns MachineSessionRuntime objects representing the runtime status of machines connected to folders.

## NOTES

Machine sessions are folder-scoped. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

This cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders.

## RELATED LINKS

[Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)

[Get-OrchUnattendedSession](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUnattendedSession.md)

[Get-OrchUserSession](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUserSession.md)
