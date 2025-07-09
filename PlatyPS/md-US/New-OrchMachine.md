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
New-OrchMachine ProdMachine01
```

Creates a new machine named "ProdMachine01" using positional parameters.

### Example 2
```powershell
New-OrchMachine DevMachine -Type "Standard" -Description "Development environment machine" -UnattendedSlots 2
```

Creates a standard machine with 2 unattended slots for development work.

### Example 3
```powershell
New-OrchMachine TemplateMachine -Type "Template" -AutomationType "Background" -TargetFramework "Windows" -Tags Template, StandardConfig
```

Creates a template machine configured for background automation on Windows with organizational tags.

### Example 4
```powershell
New-OrchMachine TestMachine -UnattendedSlots 1 -NonProductionSlots 2 -TestAutomationSlots 1 -RobotUsers "domain\user1", "domain\user2"
```

Creates a machine with various slot types and specifies robot users who can execute on this machine.

### Example 5
```powershell
"Machine01", "Machine02", "Machine03" | ForEach-Object { New-OrchMachine $_ -WhatIf -Tags Production, Cluster-A }
```

Shows what would happen when creating multiple machines with common tags using pipeline processing.

### Example 6
```powershell
New-OrchMachine -Path Orch2: BackupMachine -Type "Standard" -Scope "Global" -NonProductionSlots 1 -Description "Backup processing machine"
```

Creates a machine in the Orch2 tenant with global scope for backup operations.

## PARAMETERS

### -AutomationType
Specifies the automation type supported by the machine. Valid values are "Any" (supports both foreground and background), "Foreground" (requires user interaction), or "Background" (runs without user interaction).

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
Specifies the target framework for the machine. Valid values include "Portable" (cross-platform) and "Windows" (Windows-specific framework).

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
Specifies the type of machine to create. Valid values are "Standard" for regular machines and "Template" for template machines used to standardize configurations.

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
{{ Fill ProgressAction Description }}

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
