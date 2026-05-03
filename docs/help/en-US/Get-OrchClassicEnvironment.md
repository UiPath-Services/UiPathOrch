---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchClassicEnvironment.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchClassicEnvironment
---

# Get-OrchClassicEnvironment

## SYNOPSIS

Gets environments from classic folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchClassicEnvironment [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets environment information from classic folders in UiPath Orchestrator. Classic environments are used in the legacy provisioning model (ProvisionType = "Manual") to group robots for process execution. Only folders with manual provisioning are queried; modern folders are automatically skipped.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available environment names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Environments?$expand=Robots

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Environments.View

## EXAMPLES

### Example 1: Get all classic environments in the current folder

```powershell
PS Orch1:\Shared> Get-OrchClassicEnvironment
```

Gets all environments from the current classic folder.

### Example 2: Get environments by name

```powershell
PS Orch1:\Shared> Get-OrchClassicEnvironment Production*
```

Gets environments whose names match 'Production*' from the current folder.

### Example 3: Get environments recursively

```powershell
PS Orch1:\> Get-OrchClassicEnvironment -Recurse
```

Gets all environments from classic folders across the entire folder hierarchy.

### Example 4: Get environments from a specific folder

```powershell
PS C:\> Get-OrchClassicEnvironment -Path Orch1:\Shared
```

Gets all environments from the Shared folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

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

### -Name

Specifies the names of the environments to retrieve. Supports wildcards. Tab completion dynamically suggests environment names from the target folders.

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

You can pipe environment names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Environment

Returns Environment objects representing classic folder environments with properties including Name and associated Robots.

## NOTES

This cmdlet only queries classic folders (ProvisionType = "Manual"). Modern folders are automatically skipped.

Classic environments are a legacy concept. In modern folders, machine templates and folder-machine assignments replace the classic environment model.

## RELATED LINKS

Get-OrchClassicRobot

Get-OrchRobot

Get-OrchMachine
