---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPersonalWorkspace
---

# Get-OrchPersonalWorkspace

## SYNOPSIS

Gets personal workspaces from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchPersonalWorkspace [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets personal workspace information from UiPath Orchestrator. Personal workspaces are special folders assigned to individual users for their private automations and resources.

The cmdlet returns PersonalWorkspace objects containing the workspace name, owner name, owner ID, and folder ID. Results are returned sorted by workspace name.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completions are dynamically populated from actual personal workspaces on the target drives. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: GET /odata/PersonalWorkspaces

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: Units.View

## EXAMPLES

### Example 1: Get all personal workspaces

```powershell
PS Orch1:\> Get-OrchPersonalWorkspace
```

Gets all personal workspaces from the current Orchestrator drive.

### Example 2: Get a specific personal workspace by name

```powershell
PS Orch1:\> Get-OrchPersonalWorkspace "ytsuda@gmail.com's workspace"
```

Gets the personal workspace with the specified name.

### Example 3: Get personal workspaces using wildcards

```powershell
PS Orch1:\> Get-OrchPersonalWorkspace *ytsuda*
```

Gets all personal workspaces whose names contain "ytsuda".

### Example 4: Get personal workspaces from a specific drive

```powershell
PS C:\> Get-OrchPersonalWorkspace -Path Orch1:
```

Gets all personal workspaces from the Orch1: drive.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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

Specifies the name of the personal workspaces to retrieve. Accepts wildcard patterns.

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

You can pipe drive names to this cmdlet via the Path property, or workspace names via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.PersonalWorkspace

Returns PersonalWorkspace objects containing the workspace Name, OwnerName, OwnerId, and folder Id.

## NOTES

When multiple drives are specified, queries are executed in parallel using the thread pool.

## RELATED LINKS

Enable-OrchPersonalWorkspace

Disable-OrchPersonalWorkspace

Remove-OrchPersonalWorkspace
