---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmGroup

## SYNOPSIS
Copies groups between organizations.

## SYNTAX

```
Copy-PmGroup [[-GroupName] <String[]>] [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-PmGroup cmdlet copies groups from source organizations to destination organizations within UiPath Process Mining. This cmdlet creates copies of group configurations, including their member associations and permissions, enabling group management across multiple organization environments.

The cmdlet supports copying groups to multiple destination organizations simultaneously. Groups can be identified by their GroupName parameter, and the cmdlet supports wildcard patterns for copying multiple groups efficiently.

This cmdlet automatically adds entities such as local users, robot accounts, external applications, directory users, and directory groups with the same name to the copied group. However, if such entities with the same names do not exist in the destination organization, they will not be created automatically - only existing entities with matching names will be added to the copied group.

Use the -GroupName parameter to specify which groups to copy and the -Destination parameter to specify the target organizations. The -Path parameter enables working with multiple source organizations when not operating from within a specific organization context.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target organization drives where groups should be copied.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: [PLACEHOLDER]

Required permissions: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmGroup AdminGroup Orch2:
```

Copies the AdminGroup from the current organization (Orch1) to Orch2 organization.

### Example 2
```powershell
PS C:\> Copy-PmGroup -Path Orch1: AnalystTeam Orch2:, Orch3:
```

Copies the AnalystTeam group from Orch1 to both Orch2 and Orch3 organizations.

### Example 3
```powershell
PS Orch1:\> Copy-PmGroup DeveloperGroup, ViewerGroup Orch2: -WhatIf
```

Shows what would happen when copying DeveloperGroup and ViewerGroup from the current organization to Orch2.

### Example 4
```powershell
PS C:\> Copy-PmGroup -Path Orch1: *Admin* Orch2:
```

Copies all groups with names containing Admin from Orch1 to Orch2 using wildcard patterns.

### Example 5
```powershell
PS Orch1:\> Get-PmGroup *Manager* | Copy-PmGroup -Destination Orch2:, Orch3:
```

Gets all groups with names containing Manager and copies them to both Orch2 and Orch3 organizations using pipeline input.

### Example 6
```powershell
PS C:\> Copy-PmGroup -Path Orch1: Orch2: -Confirm
```

Copies all groups from Orch1 to Orch2 with confirmation prompts (when -GroupName is not specified, all groups are copied).

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
Specifies the destination organization drives where groups should be copied.

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

### -GroupName
Specifies the GroupName of the groups to be copied. If not specified, all groups will be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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
### System.String
## OUTPUTS

### System.Object
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination organizations.

Groups contain member associations and permissions. When copying groups, entities with the same names (local users, robot accounts, external applications, directory users, and directory groups) are automatically added to the copied group if they exist in the destination organization. Entities that do not exist in the destination will not be created automatically. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-PmGroup](Get-PmGroup.md)

[New-PmGroup](New-PmGroup.md)

[Remove-PmGroup](Remove-PmGroup.md)

[Set-PmGroup](Set-PmGroup.md)
