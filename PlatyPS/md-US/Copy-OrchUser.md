---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchUser

## SYNOPSIS
Copies users between tenants.

## SYNTAX

```
Copy-OrchUser [-UserName] <String[]> [-FullName <String[]>] [-Type <String[]>] [-Destination] <String[]>
 [-Path <String>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchUser cmdlet copies users from source tenants to destination tenants within UiPath Orchestrator. This cmdlet creates copies of user accounts, including their configurations and metadata, enabling user management across multiple tenant environments.

The cmdlet supports copying users to multiple destination tenants simultaneously. Users can be identified by either UserName or FullName parameters, and filtered by Type for targeted user management operations.

Use the -UserName parameter to specify which users to copy and the -Destination parameter to specify the target tenants. The -Type parameter allows filtering by user types, and the -Path parameter enables working with multiple source tenants.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target tenant drives where users should be copied.

Primary Endpoint: GET /odata/Users, GET /api/DirectoryService/SearchForUsersAndGroups, POST /odata/Users

OAuth required scopes: OR.Users

Required permissions: Users.View, Users.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchUser john.doe Orch2:
```

Copies user john.doe from the current tenant (Orch1) to Orch2 tenant.

### Example 2
```powershell
PS C:\> Copy-OrchUser -Path Orch1: jane.smith Orch2:, Orch3:
```

Copies user jane.smith from Orch1 to both Orch2 and Orch3 tenants.

### Example 3
```powershell
PS Orch1:\> Copy-OrchUser admin.user, developer.user Orch2: -WhatIf
```

Shows what would happen when copying admin.user and developer.user from the current tenant to Orch2.

### Example 4
```powershell
PS C:\> Copy-OrchUser -Path Orch1: *admin* Orch2: -Type User
```

Copies all users with usernames containing admin from Orch1 to Orch2, filtering by User type.

### Example 5
```powershell
PS Orch1:\> Get-OrchUser *developer* | Copy-OrchUser -Destination Orch2:, Orch3:
```

Gets all users with usernames containing developer and copies them to both Orch2 and Orch3 tenants using pipeline input.

### Example 6
```powershell
PS C:\> Copy-OrchUser -Path Orch1: -FullName "John Smith" Orch2: -Confirm
```

Copies the user with full name "John Smith" from Orch1 to Orch2 with confirmation prompts.

## PARAMETERS

### -Destination
Specifies the destination tenant drives where users should be copied.

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

### -FullName
Specifies the FullName of the users to be copied. Alternative to UserName parameter.

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

### -Path
Specifies the source tenant drive. If not specified, the current tenant will be used as the source.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### -UserName
Specifies the UserName of the users to be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### -Type
Specifies the user types to filter by when copying users.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination tenants.

Users can be identified by either UserName or FullName. Use -Type parameter to filter by specific user types. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchUser](Get-OrchUser.md)

[Add-OrchUser](Add-OrchUser.md)

[Remove-OrchUser](Remove-OrchUser.md)

[Set-OrchUser](Set-OrchUser.md)
