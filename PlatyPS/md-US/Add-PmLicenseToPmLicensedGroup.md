---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-PmLicenseToPmLicensedGroup

## SYNOPSIS
Adds licenses to licensed groups.

## SYNTAX

```
Add-PmLicenseToPmLicensedGroup [-GroupName] <String[]> [-License] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-PmLicenseToPmLicensedGroup cmdlet assigns licenses to licensed groups within UiPath Platform Management. This cmdlet manages license allocation at the organization level, allowing you to distribute UiPath licenses to groups that can then assign them to their members.

Licensed groups are special groups that can hold and distribute UiPath licenses (such as Studio, Attended Robot, Unattended Robot licenses) to their members. Adding licenses to these groups makes those licenses available for automatic assignment to group members based on their roles and requirements.

Use the -GroupName parameter to specify which licensed groups to assign licenses to, and the -License parameter to specify which license types to assign. The -Path parameter enables working with multiple platform instances.

This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for targeting specific platform instances when working with multiple environments.

Primary Endpoint: PUT /api/license/accountant/UserLicense/group

OAuth required scopes: [PLACEHOLDER]

Required permissions: License management permissions at the organization level

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Add-PmLicenseToPmLicensedGroup Developers StudioPro
```

Adds StudioPro licenses to the Developers licensed group in the current platform instance.

### Example 2
```powershell
PS C:\> Add-PmLicenseToPmLicensedGroup -Path Orch1:, Orch2: "Automation Team" UnattendedRobot
```

Adds UnattendedRobot licenses to the "Automation Team" licensed group in both Orch1 and Orch2 platform instances.

### Example 3
```powershell
PS Orch1:\> Add-PmLicenseToPmLicensedGroup TestTeam StudioPro, AttendedRobot -WhatIf
```

Shows what would happen when adding both StudioPro and AttendedRobot licenses to the TestTeam licensed group.

### Example 4
```powershell
PS C:\> Add-PmLicenseToPmLicensedGroup -GroupName "Production Bots" -License UnattendedRobot -Confirm
```

Adds UnattendedRobot licenses to the "Production Bots" licensed group with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Add-PmLicenseToPmLicensedGroup "Business Users", "Power Users" StudioX
```

Adds StudioX licenses to both "Business Users" and "Power Users" licensed groups.

### Example 6
```powershell
PS C:\> Get-PmLicensedGroup | Where-Object {$_.Name -like "*Dev*"} | Add-PmLicenseToPmLicensedGroup -License StudioPro
```

Gets all licensed groups with names containing Dev and adds StudioPro licenses to them using pipeline input.

## PARAMETERS

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

### -GroupName
Specifies the name of the licensed groups to assign licenses to.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -License
Specifies the license types to assign to the licensed groups.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the platform instances to target.

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

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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
## OUTPUTS

### UiPath.PowerShell.Entities.UpdateLicensedGroupResponse
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for targeting specific platform instances.

This cmdlet operates at the organization level through Platform Management for license allocation. Licensed groups can automatically distribute assigned licenses to their members. Common license types include StudioPro, StudioX, AttendedRobot, and UnattendedRobot. Use -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-PmLicensedGroup](Get-PmLicensedGroup.md)

[Remove-PmLicenseFromPmLicensedGroup](Remove-PmLicenseFromPmLicensedGroup.md)


