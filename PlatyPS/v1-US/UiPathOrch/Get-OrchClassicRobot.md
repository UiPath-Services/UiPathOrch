---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchClassicRobot
---

# Get-OrchClassicRobot

## SYNOPSIS

Gets robots from classic folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchClassicRobot [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets robot information from classic folders in UiPath Orchestrator. Classic folders use the legacy provisioning model (ProvisionType = "Manual") where robots are explicitly assigned to folders. Only folders with manual provisioning are queried; modern folders are automatically skipped.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available robot names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports CSV export with columns: Path, MachineName, Name, Description, Type, Username, RobotEnvironments.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Sessions?$expand=Robot($expand=License)

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Robots.View

## EXAMPLES

### Example 1: Get all classic robots in the current folder

```powershell
PS Orch1:\Shared> Get-OrchClassicRobot
```

Gets all robots from the current classic folder.

### Example 2: Get robots by name

```powershell
PS Orch1:\Shared> Get-OrchClassicRobot Robot*
```

Gets robots whose names match 'Robot*' from the current folder.

### Example 3: Get robots recursively

```powershell
PS Orch1:\> Get-OrchClassicRobot -Recurse
```

Gets all robots from classic folders across the entire folder hierarchy.

### Example 4: Export robots to CSV

```powershell
PS Orch1:\> Get-OrchClassicRobot -Recurse -ExportCsv C:\temp\classic-robots.csv
```

Exports all classic robots to a CSV file with columns: Path, MachineName, Name, Description, Type, Username, RobotEnvironments.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExportCsv

Exports classic robots to the specified CSV file path. The CSV includes columns: Path, MachineName, Name, Description, Type, Username, RobotEnvironments. Requires a filesystem path (not an Orch: drive path). If only a filename is specified, the default filename 'ExportedClassicRobots.csv' is used.

```yaml
Type: System.String
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

Specifies the names of the robots to retrieve. Supports wildcards. Tab completion dynamically suggests robot names from the target folders.

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

You can pipe robot names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Session

Returns Session objects containing robot information including Robot.Name, Robot.MachineName, Robot.Type, Robot.Username, and Robot.RobotEnvironments.

## NOTES

This cmdlet only queries classic folders (ProvisionType = "Manual"). Modern folders are automatically skipped.

For robots in modern folders, use Get-OrchRobot instead.

## RELATED LINKS

Get-OrchRobot

Get-OrchClassicEnvironment

Get-OrchMachine
