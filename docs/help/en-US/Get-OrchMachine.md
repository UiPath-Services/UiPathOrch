---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchMachine
---

# Get-OrchMachine

## SYNOPSIS

Gets machines from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchMachine [[-Name] <string[]>] [-Path <string[]>] [-ExpandRobotUser] [-ExportCsv <string>]
 [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets machine information from UiPath Orchestrator. Machines are entities that represent the physical or virtual hosts on which UiPath Robots run. Each machine has properties including Name, Type (Template or Standard), Scope (Default, Serverless, or AutomationCloudRobot), runtime slots (Unattended, NonProduction, TestAutomation), AutomationType, TargetFramework, associated RobotUsers, update policy, maintenance window, and tags.

This cmdlet supports filtering by machine name using wildcards, expanding robot user assignments, and exporting results to CSV files. When -ExpandRobotUser is specified, the cmdlet outputs RobotUser objects associated with each machine instead of the machine objects themselves.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completions are dynamically populated from actual machines in the target Orchestrator instances.

Machines are tenant-scoped entities. The -Path parameter accepts Orch drive paths (e.g., Orch1:\) to target specific Orchestrator instances.

Primary Endpoint: GET /odata/Machines&$expand=UpdateInfo

OAuth required scopes: OR.Machines or OR.Machines.Read

Required permissions: Machines.View

## EXAMPLES

### Example 1: Get all machines on the current Orchestrator drive

```powershell
PS Orch1:\> Get-OrchMachine
```

Gets all machines from the current Orchestrator instance.

### Example 2: Get machines by name

```powershell
PS Orch1:\> Get-OrchMachine machine1
```

Gets the machine named "machine1" from the current Orchestrator instance. The -Name parameter is positional (position 0), so the parameter name can be omitted.

### Example 3: Get machines using a wildcard filter

```powershell
PS Orch1:\> Get-OrchMachine machine*
```

Gets all machines whose names start with "machine" from the current Orchestrator instance.

### Example 4: Get machines from a specific Orchestrator instance

```powershell
PS C:\> Get-OrchMachine -Path Orch1:\ MachineTemp1
```

Gets the machine named "MachineTemp1" from the Orch1 instance. When -Path uses an absolute path (Orch1:\), the command can be run from any location.

### Example 5: Expand robot user assignments

```powershell
PS Orch1:\> Get-OrchMachine -ExpandRobotUser
```

Gets all machines and outputs the RobotUser objects associated with each machine, showing user/machine assignments instead of machine objects.

### Example 6: Export machines to CSV

```powershell
PS Orch1:\> Get-OrchMachine -ExportCsv C:\temp\machines.csv
```

Exports all machines to a CSV file. The CSV includes columns for Path, Name, Description, Type, Scope, slot counts, AutomationType, TargetFramework, RobotUsers, update policy, maintenance window, and Tags.

### Example 7: Get machines from multiple Orchestrator instances

```powershell
PS C:\> Get-OrchMachine -Path Orch1:\,Orch2:\
```

Gets all machines from both Orch1 and Orch2 instances. Multiple paths are processed in parallel for performance.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted. Supports comma-separated values for multiple drives. Tab completion dynamically suggests available Orch drives.

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

### -ExpandRobotUser

Expands robot user assignments for each machine, outputting RobotUser objects instead of machine objects. Each RobotUser shows the user/machine association including UserName and RobotId.

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

### -ExportCsv

Exports machines to the specified CSV file path. The CSV includes columns for Path, Name, Description, Type, Scope, UnattendedSlots, NonProductionSlots, TestAutomationSlots, AutomationType, TargetFramework, RobotUsers, UpdatePolicyType, UpdatePolicyVersion, MaintenanceCron, MaintenanceDuration, MaintenanceEnabled, MaintenanceTimezoneId, and Tags. Requires a filesystem path (not an Orch: drive path).

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

Specifies the names of machines to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests machine names from the target Orchestrator instances. This parameter is positional (position 0), so the parameter name can be omitted.

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

You can pipe machine names to this cmdlet via the Name property, and Orch drive paths via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedMachine

Returns ExtendedMachine objects with properties including Name, Description, Type, Scope, UnattendedSlots, NonProductionSlots, TestAutomationSlots, AutomationType, TargetFramework, RobotUsers, LicenseKey, UpdatePolicy, MaintenanceWindow, and Tags.

### UiPath.PowerShell.Entities.RobotUser

When -ExpandRobotUser is specified, returns RobotUser objects with properties including UserName and RobotId, representing the user/machine assignments for each machine.

## NOTES

Machines are tenant-scoped entities. You must be on an Orch: drive or use -Path to specify target Orchestrator instances.

When -ExportCsv is specified, no objects are written to the pipeline; all output goes to the CSV file. The RobotUsers column in the CSV resolves robot IDs to user full names.

Multiple Orchestrator drives are queried in parallel for improved performance.

## RELATED LINKS

New-OrchMachine

Update-OrchMachine

Remove-OrchMachine

Copy-OrchMachine
