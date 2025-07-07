---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmRobotAccount

## SYNOPSIS
Copies robot accounts between organizations.

## SYNTAX

```
Copy-PmRobotAccount [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-PmRobotAccount cmdlet copies robot accounts from source organizations to destination organizations within UiPath Process Mining. This cmdlet creates copies of robot account configurations, including their authentication settings, permissions, and group associations, enabling robot account management across multiple organization environments.

The cmdlet supports copying robot accounts to multiple destination organizations simultaneously. Robot accounts can be identified by their Name parameter, and the cmdlet supports wildcard patterns for copying multiple robot accounts efficiently.

If the groups to which the robot accounts belong do not exist in the destination organization, they are automatically created during the copy operation, ensuring complete account configuration transfer.

Use the -Name parameter to specify which robot accounts to copy and the -Destination parameter to specify the target organizations. The -Path parameter enables working with multiple source organizations when not operating from within a specific organization context.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target organization drives where robot accounts should be copied.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: [PLACEHOLDER]

Required permissions: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmRobotAccount ProcessBot Orch2:
```

Copies the ProcessBot robot account from the current organization (Orch1) to Orch2 organization.

### Example 2
```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: DataCollector Orch2:, Orch3:
```

Copies the DataCollector robot account from Orch1 to both Orch2 and Orch3 organizations.

### Example 3
```powershell
PS Orch1:\> Copy-PmRobotAccount AutomationBot, AnalyticsBot Orch2: -WhatIf
```

Shows what would happen when copying AutomationBot and AnalyticsBot from the current organization to Orch2.

### Example 4
```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: *Service* Orch2:
```

Copies all robot accounts with names containing Service from Orch1 to Orch2 using wildcard patterns.

### Example 5
```powershell
PS Orch1:\> Get-PmRobotAccount *Monitor* | Copy-PmRobotAccount -Destination Orch2:, Orch3:
```

Gets all robot accounts with names containing Monitor and copies them to both Orch2 and Orch3 organizations using pipeline input.

### Example 6
```powershell
PS C:\> Copy-PmRobotAccount -Path Orch1: IntegrationBot Orch2: -Confirm
```

Copies the IntegrationBot robot account from Orch1 to Orch2 with confirmation prompts.

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

### -Destination
Specifies the destination organization drives where robot accounts should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the Name of the robot accounts to be copied.

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

### -Path
Specifies the source organization drive. If not specified, the current organization will be used as the source.

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
Robot account names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.RobotAccount
RobotAccount objects from Get-PmRobotAccount can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### System.Object
Returns information about the copied robot accounts.

## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination organizations.

Robot accounts contain authentication settings, permissions, and group associations. If associated groups do not exist in the destination organization, they are automatically created. When copying across environments, verify that the robot account configurations are appropriate for the destination environment. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-PmRobotAccount](Get-PmRobotAccount.md)

[New-PmRobotAccount](New-PmRobotAccount.md)

[Remove-PmRobotAccount](Remove-PmRobotAccount.md)

[Set-PmRobotAccount](Set-PmRobotAccount.md)
