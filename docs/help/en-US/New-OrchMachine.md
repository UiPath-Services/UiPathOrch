---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-OrchMachine
---

# New-OrchMachine

## SYNOPSIS

Creates a new machine in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchMachine [-Name] <string[]> [-Description <string>] [-Type <string>] [-Scope <string>]
 [-UnattendedSlots <int>] [-NonProductionSlots <int>] [-TestAutomationSlots <int>]
 [-AutomationType <string>] [-TargetFramework <string>] [-RobotUsers <string[]>] [-Tags <string[]>]
 [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new machine definition in UiPath Orchestrator. A machine represents a physical or virtual host on which UiPath Robots run. When creating a machine, you can specify its type, scope, runtime slot allocations, automation type, target framework, robot user assignments, and tags.

The -Type parameter defaults to "Template" if not specified. Tab completion suggests Template, Standard, and Serverless. The -Scope parameter supports Default, Serverless, and AutomationCloudRobot. When -Scope is set to Serverless, the -TargetFramework defaults to "Portable". Machines with a Scope of "PersonalWorkspace" cannot be created with this cmdlet; use Enable-OrchPersonalWorkspace instead.

The -Name parameter supports tab completion that suggests a new unique machine name based on existing machines. The -RobotUsers parameter supports wildcards and tab completion, matching users by their full name across all folders.

The -Name, -Type, -Scope, -AutomationType, -TargetFramework, -RobotUsers, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: POST /odata/Machines

OAuth required scopes: OR.Machines

Required permissions: Machines.Create

## EXAMPLES

### Example 1: Create a basic template machine

```powershell
PS Orch1:\> New-OrchMachine NewMachine1
```

Creates a new template machine named "NewMachine1" on the current Orchestrator instance. The default Type is "Template".

### Example 2: Create a machine with description and slots

```powershell
PS Orch1:\> New-OrchMachine NewMachine1 -Description "Unattended processing host" -UnattendedSlots 3 -NonProductionSlots 1
```

Creates a new machine named "NewMachine1" with a description, 3 unattended runtime slots, and 1 non-production slot.

### Example 3: Create a standard machine with specific automation type

```powershell
PS Orch1:\> New-OrchMachine NewMachine1 -Type Standard -AutomationType Background -TargetFramework Windows
```

Creates a standard machine with background automation type and Windows target framework.

### Example 4: Create a serverless machine

```powershell
PS Orch1:\> New-OrchMachine NewServerless1 -Type Serverless -Scope Serverless
```

Creates a serverless machine. When Scope is Serverless, the TargetFramework automatically defaults to "Portable".

### Example 5: Create a machine with robot user assignments

```powershell
PS Orch1:\> New-OrchMachine NewMachine1 -RobotUsers "Yoshifumi Tsuda","ytsuda+c"
```

Creates a template machine and associates it with the specified robot users. The -RobotUsers parameter matches users by their full name across all folders.

### Example 6: Create a machine on a specific Orchestrator instance

```powershell
PS C:\> New-OrchMachine -Path Orch1:\ -Name NewMachine1 -Tags Environment:Production
```

Creates a new machine on the Orch1 instance with a tag. When -Path uses an absolute path, the command can be run from any location.

### Example 7: Preview creation with -WhatIf

```powershell
PS Orch1:\> New-OrchMachine NewMachine1 -WhatIf
```

```output
What if: Performing the operation "New Machine" on target "Orch1:\NewMachine1".
```

Shows what would happen without actually creating the machine.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted. Supports comma-separated values for multiple drives. Tab completion dynamically suggests available Orch drives.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -AutomationType

Specifies the automation type for the machine. Tab completion suggests Any, Foreground, and Background. This determines the type of automation that can run on the machine.

```yaml
Type: System.String
DefaultValue: ''
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -Description

Specifies a description for the new machine. This text appears in the Orchestrator UI and helps identify the purpose of the machine.

```yaml
Type: System.String
DefaultValue: ''
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

### -Name

Specifies the name of the new machine. This is a mandatory parameter. Tab completion suggests a unique name based on existing machines. Multiple names can be specified to create multiple machines at once.

```yaml
Type: System.String[]
DefaultValue: ''
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

Specifies the number of non-production runtime slots allocated to the machine. Non-production slots are used for development and testing purposes.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

Specifies the robot users to associate with the machine. Supports wildcards and comma-separated values. Tab completion dynamically suggests user full names from all folders across the Orchestrator instance. Users are matched by their full name.

```yaml
Type: System.String[]
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

### -Scope

Specifies the scope of the machine. Tab completion suggests Default, Serverless, and AutomationCloudRobot. The scope determines how the machine is provisioned and managed. Machines with a Scope of "PersonalWorkspace" cannot be created with this cmdlet.

```yaml
Type: System.String
DefaultValue: ''
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

### -Tags

Specifies one or more tags to assign to the machine. Tags help categorize and filter machines in the Orchestrator UI.

```yaml
Type: System.String[]
DefaultValue: ''
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

Specifies the target framework for the machine. Tab completion suggests Any, Windows, and Portable. When -Scope is Serverless, the default is "Portable".

```yaml
Type: System.String
DefaultValue: ''
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

Specifies the number of test automation runtime slots allocated to the machine. Test automation slots are used for running test cases.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -Type

Specifies the type of the machine. Tab completion suggests Template, Standard, and Serverless. The default is "Template". Template machines can be connected by multiple robots, while Standard machines are bound to a single host.

```yaml
Type: System.String
DefaultValue: ''
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

Specifies the number of unattended runtime slots allocated to the machine. Unattended slots are used for running processes without user interaction.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the Description property to this cmdlet.

### System.Int32

You can pipe slot count values (UnattendedSlots, NonProductionSlots, TestAutomationSlots) to this cmdlet.

### System.String[]

You can pipe machine names via the Name property, robot user names via the RobotUsers property, tags via the Tags property, and Orch drive paths via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.CreatedMachine

Returns a CreatedMachine object for each newly created machine, including the machine's Id, Name, LicenseKey (ClientId), and other properties assigned by the server.

## NOTES

Machines are tenant-scoped entities. You must be on an Orch: drive or use -Path to specify target Orchestrator instances.

The -Type parameter defaults to "Template" if not specified. When -Scope is "Serverless", the -TargetFramework defaults to "Portable" if not explicitly provided.

Machines with a Scope of "PersonalWorkspace" cannot be created with this cmdlet. Use Enable-OrchPersonalWorkspace instead.

## RELATED LINKS

[Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)

[Update-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchMachine.md)

[Remove-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchMachine.md)

[Copy-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchMachine.md)
