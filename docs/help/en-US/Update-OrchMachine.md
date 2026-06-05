---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchMachine
---

# Update-OrchMachine

## SYNOPSIS

Updates machine definitions in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchMachine [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [-AutomationType <string>]
 [-Confirm] [-Description <string>] [-MaintenanceCron <string>]
 [-MaintenanceDuration <int>] [-MaintenanceEnabled <string>]
 [-MaintenanceTimeZone <string>] [-MaintenanceTimeZoneId <string>]
 [-NonProductionSlots <int>] [-RobotUsers <string[]>] [-Tags <string[]>]
 [-TargetFramework <string>] [-TestAutomationSlots <int>] [-UnattendedSlots <int>]
 [-UpdatePolicyType <string>] [-UpdatePolicyVersion <string>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates existing machine definitions in UiPath Orchestrator. Only the parameters that are explicitly specified are modified; all other properties are preserved from the current machine definition. The cmdlet deep copies the current machine definition before applying changes to ensure existing values are not inadvertently lost.

Machines with a Scope of 'AutomationCloudRobot' cannot be updated through this cmdlet and will generate a warning.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text that includes wildcards.

The -Tags parameter supports tab completion showing existing machine tags that can be reused.

The -AutomationType parameter supports tab completion with values: Any, Foreground, Background.

The -TargetFramework parameter supports tab completion with values: Any, Windows, Portable.

The -UpdatePolicyType parameter supports tab completion with values: None, LatestPatch, LatestVersion, SpecificVersion.

Machines are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: PATCH /odata/Machines({machineId})

OAuth required scopes: OR.Machines

Required permissions: Machines.Edit

## EXAMPLES

### Example 1: Update a machine description

```powershell
PS Orch1:\> Update-OrchMachine aiai -Description "Updated machine description"
```

Updates the description of the machine named 'aiai'. All other properties remain unchanged.

### Example 2: Update slot allocations

```powershell
PS Orch1:\> Update-OrchMachine pool -UnattendedSlots 5 -NonProductionSlots 2
```

Updates the unattended and non-production slot counts for the specified machine template.

### Example 3: Configure maintenance window

```powershell
PS Orch1:\> Update-OrchMachine aiai -MaintenanceEnabled true -MaintenanceCron "0 0 2 ? * SUN" -MaintenanceDuration 120 -MaintenanceTimeZone *Tokyo*
```

Enables a maintenance window for the specified machine, scheduled every Sunday at 2:00 AM Tokyo time for 120 minutes.

### Example 4: Update multiple machines with wildcards

```powershell
PS Orch1:\> Update-OrchMachine m* -TargetFramework Portable -WhatIf
```

Shows which machines matching 'm*' would be updated to use the Portable target framework, without actually making changes.

### Example 5: Update robot user assignments

```powershell
PS Orch1:\> Update-OrchMachine pool -RobotUsers ytsuda@gmail.com,ytsuda+c@gmail.com
```

Updates the robot user assignments for the specified machine. Wildcard patterns are supported for user full names.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -AutomationType

Specifies the automation type for the machine. Tab completion suggests: Any, Foreground, Background.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Description

Specifies a new description for the machine.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaintenanceCron

Specifies the cron expression for the maintenance window schedule (e.g., "0 0 2 ? * SUN" for every Sunday at 2:00 AM).

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaintenanceDuration

Specifies the duration of the maintenance window in minutes.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -MaintenanceEnabled

Specifies whether the maintenance window is enabled. Tab completion suggests "true" and "false" values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaintenanceTimeZone

Specifies the time zone for the maintenance window. Tab completion suggests available system time zones by display name. Supports wildcards for partial matching (e.g., *Eastern*).

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
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

### -MaintenanceTimeZoneId

Specifies the time zone ID for the maintenance window directly (e.g., "Eastern Standard Time"). This is an alternative to -MaintenanceTimeZone for pipeline/CSV scenarios.

```yaml
Type: System.String
DefaultValue: ''
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

Specifies the names of the machines to update. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests machine names from the target drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -NonProductionSlots

Specifies the number of non-production runtime slots allocated to this machine.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -RobotUsers

Specifies the robot users to assign to this machine. Supports wildcards matching against user full names. Tab completion suggests available robot users.

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

### -Tags

Specifies tags to assign to the machine. Tab completion suggests existing machine tags for reuse.

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

### -TargetFramework

Specifies the target framework for the machine. Tab completion suggests: Any, Windows, Portable.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TestAutomationSlots

Specifies the number of test automation runtime slots allocated to this machine.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -UnattendedSlots

Specifies the number of unattended runtime slots allocated to this machine.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -UpdatePolicyType

Specifies the update policy type for the machine. Tab completion suggests: None, LatestPatch, LatestVersion, SpecificVersion.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UpdatePolicyVersion

Specifies the specific version for the update policy when UpdatePolicyType is set to SpecificVersion. Tab completion suggests available versions.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

### System.String[]

You can pipe machine names to this cmdlet via the Name property.

### System.String

You can pipe the Path, Description, and other string properties to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output. Machine definitions are updated in place on the Orchestrator server.

## NOTES

Machines are tenant-scoped entities. They are not associated with specific folders.

Only explicitly specified parameters are modified. The cmdlet deep copies the current machine definition before applying changes, ensuring that unspecified properties retain their existing values.

Machines with Scope 'AutomationCloudRobot' cannot be updated through this cmdlet.

When specifying -RobotUsers, the entire list of robot user assignments is replaced. The cmdlet resolves user full names (with wildcard support) to robot IDs from the tenant's robot list.

## RELATED LINKS

[Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)

[New-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchMachine.md)

[Remove-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchMachine.md)

[Copy-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchMachine.md)
