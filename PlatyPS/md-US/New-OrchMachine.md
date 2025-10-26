---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchMachine

## SYNOPSIS
Creates machines.

## SYNTAX

```
New-OrchMachine [-Name] <String[]> [-Description <String>] [-Type <String>] [-Scope <String>]
 [-UnattendedSlots <Int32>] [-NonProductionSlots <Int32>] [-TestAutomationSlots <Int32>]
 [-AutomationType <String>] [-TargetFramework <String>] [-RobotUsers <String[]>] [-Tags <String[]>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The New-OrchMachine cmdlet creates new machines in UiPath Orchestrator. Machines define the execution environments where robots can run automation processes. You can configure various types of machines including Standard machines for production workloads and Template machines for creating standardized configurations.

**This is a tenant entity cmdlet.** You can specify target tenants using the -Path parameter with drive names (e.g., Orch1:, Orch2:). If -Path is not specified, the machine will be created in the current location.

Machines can be configured with different slot types for various automation scenarios: UnattendedSlots for background automation, NonProductionSlots for development and testing, and TestAutomationSlots for automated testing scenarios. You can also specify automation types (Any, Foreground, Background) and target frameworks (Portable, Windows).

The cmdlet supports CSV import functionality. Use Get-OrchMachine -ExportCsv to obtain the format for bulk machine creation.

Primary Endpoint: POST /odata/Machines
OAuth required scopes: OR.Machines or OR.Machines.Write
Required permissions: Machines.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> New-OrchMachine Machine01 -Description "Test machine for verification" -WhatIf
```

Tests creating a new machine with basic settings using -WhatIf to preview the operation before execution.

### Example 2
```powershell
PS Orch1:\> New-OrchMachine ProductionMachine Standard -UnattendedSlots 3 -NonProductionSlots 1 -Description "Production environment machine"
```

Creates a Standard machine configured with 3 unattended slots and 1 non-production slot for mixed workloads.

### Example 3
```powershell
PS Orch1:\> New-OrchMachine TestAutomationMachine -TestAutomationSlots 2 -AutomationType Background -TargetFramework Windows -Description "Dedicated test automation machine"
```

Creates a machine specifically for test automation with background execution and Windows target framework.

### Example 4
```powershell
PS C:\> New-OrchMachine -Path Orch1:, Orch2: TemplateMachine -Type Template -UnattendedSlots 2 -Description "This is a template machine"
```

Creates a template machine across multiple tenants using the -Path parameter for standardized configurations.

### Example 5
```powershell
PS Orch1:\> New-OrchMachine DevelopmentMachine -Scope Folder -NonProductionSlots 2 -Tags Development,Testing -Description "Development environment machine"
```

Creates a folder-scoped machine for development with tags for organization and filtering.

### Example 6
```powershell
PS C:\> Import-Csv machines.csv | New-OrchMachine -WhatIf
```

Tests bulk machine creation from a CSV file. The CSV should contain columns: Name, Description, Type, UnattendedSlots, NonProductionSlots, TestAutomationSlots.

### Example 7
```powershell
PS Orch1:\> New-OrchMachine RobotHost -RobotUsers "DOMAIN\robot1","DOMAIN\robot2" -UnattendedSlots 2 -Description "Machine with pre-configured robot users"
```

Creates a machine with specific robot users assigned for unattended automation execution.

### Example 8
```powershell
PS Orch1:\> New-OrchMachine * "DynamicMachine" -Type Standard -AutomationType Any -TargetFramework Portable -Description "Portable machine for any automation type"
```

Creates a portable machine that can run any type of automation process across different platforms.

## PARAMETERS

### -AutomationType
Specifies the automation type supported by the machine. Valid values are Any (supports both foreground and background), Foreground (requires user interaction), or Background (runs without user interaction).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Specifies a description for the machine that explains its purpose or configuration.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the name(s) of the machine(s) to create. Machine names must be unique within the tenant.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -NonProductionSlots
Specifies the number of non-production slots for development and testing purposes. These slots are typically used in development environments.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the path(s) to the target tenant(s) where the machine(s) will be created. Use drive names like Orch1:, Orch2: to target specific tenants.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RobotUsers
Specifies the robot users who are authorized to execute automation on this machine. Use domain\username format for domain users.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Scope
Specifies the scope of the machine. This determines the visibility and access level of the machine within the organization.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Tags
Specifies tags to associate with the machine for organization, categorization, and management purposes.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TargetFramework
Specifies the target framework for the machine. Valid values include Portable (cross-platform) and Windows (Windows-specific framework).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TestAutomationSlots
Specifies the number of test automation slots for automated testing scenarios. These slots are dedicated to running test cases and test sets.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Type
Specifies the type of machine to create. Valid values are Standard for regular machines and Template for template machines used to standardize configurations.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UnattendedSlots
Specifies the number of unattended slots for background automation. These slots allow robots to run without user interaction.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Specifies how PowerShell responds to progress updates generated by the cmdlet.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.CreatedMachine
## NOTES
- Machine names must be unique within the tenant
- Consider using -WhatIf to preview the operation before actual creation
- Slot allocation affects licensing and should be planned according to your automation needs
- Template machines can be used to standardize machine configurations across environments
- Robot users must exist in the system before being assigned to machines
- Tags are useful for organizing machines and implementing governance policies

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)
[Update-OrchMachine](Update-OrchMachine.md)
[Remove-OrchMachine](Remove-OrchMachine.md)
[Copy-OrchMachine](Copy-OrchMachine.md)

