---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmUser

## SYNOPSIS
Copies organizational users between organizations.

## SYNTAX

```
Copy-PmUser [-Email] <String[]> [-Destination] <String[]> [-Path <String>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-PmUser cmdlet copies Platform Management users from source organizations to destination organizations. This cmdlet creates copies of user configurations, including their permissions, settings, and group associations, enabling user management across multiple UiPath organizations.

The cmdlet supports copying users to multiple destination organizations simultaneously. Users can be identified by their Email parameter (which has an alias of UserName), and the cmdlet supports wildcard patterns for copying multiple users efficiently.

If the groups to which the users belong do not exist in the destination organization, they are automatically created during the copy operation, ensuring complete user configuration transfer.

Use the -Email parameter to specify which users to copy and the -Destination parameter to specify the target environments. The -Path parameter enables working with multiple source organizations when not operating from within a specific organization context.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target organization drives where users should be copied.

Primary Endpoint: POST /api/User/BulkCreate

OAuth required scopes: PM.User

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-PmUser john.doe@uipath.com Orch2:
```

Copies the user john.doe@uipath.com from the current organization (Orch1) to Orch2 organization.

### Example 2
```powershell
PS C:\> Copy-PmUser -Path Orch1: admin@uipath.com Orch2:, Orch3:
```

Copies the user admin@uipath.com from Orch1 to both Orch2 and Orch3 organizations.

### Example 3
```powershell
PS Orch1:\> Copy-PmUser analyst@uipath.com, viewer@uipath.com Orch2: -WhatIf
```

Shows what would happen when copying analyst@uipath.com and viewer@uipath.com from the current organization to Orch2.

### Example 4
```powershell
PS C:\> Copy-PmUser -Path Orch1: *admin* Orch2:
```

Copies all users with email addresses containing admin from Orch1 to Orch2 using wildcard patterns.

### Example 5
```powershell
PS Orch1:\> Get-PmUser *manager* | Copy-PmUser -Destination Orch2:, Orch3:
```

Gets all users with email addresses containing manager and copies them to both Orch2 and Orch3 organizations using pipeline input.

### Example 6
```powershell
PS C:\> Copy-PmUser -Path Orch1: developer@uipath.com Orch2: -Confirm
```

Copies the user developer@uipath.com from Orch1 to Orch2 with confirmation prompts.

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
Specifies the destination organization drives where users should be copied.

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

### -Email
Specifies the Email addresses of the users to be copied. This parameter has an alias of UserName for compatibility.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: UserName

Required: True
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
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.PmUser
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination organizations.

Users contain permissions, settings, and group associations. If associated groups do not exist in the destination organization, they are automatically created. When copying across environments, verify that user configurations are appropriate for the destination environment. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-PmUser](Get-PmUser.md)

[New-PmUser](New-PmUser.md)

[Remove-PmUser](Remove-PmUser.md)

