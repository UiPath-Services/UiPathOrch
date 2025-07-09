---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-PmUser

## SYNOPSIS
Creates users in Platform Management.

## SYNTAX

```
New-PmUser [-Email] <String> [-Name <String>] [-SurName <String>] [-DisplayName <String>] [-Type <String>]
 [-BypassBasicAuthRestriction <String>] [-InvitationAccepted <String>] [-GroupName <String[]>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The New-PmUser cmdlet creates users in UiPath Platform Management. Users created at the Platform Management level have organization-wide access and can be managed centrally across multiple tenants.

**This is an organization entity cmdlet.** It operates at the Platform Management level and manages users across the entire organization. Use the -Path parameter to specify target tenants if needed.

Users created through Platform Management benefit from shared caches across tenants within the same organization, as mentioned in the UiPathOrch 0.9.13.0 release notes, providing improved performance and consistent behavior.

Primary Endpoint: POST /api/platformmanagement/users
OAuth required scopes: OR.Users or OR.Users.Write
Required permissions: Users.Create

## EXAMPLES

### Example 1
```powershell
New-PmUser user@company.com
```

Creates a new user with the specified email address using positional parameters.

### Example 2
```powershell
New-PmUser admin@company.com -Name \"admin\" -DisplayName \"System Administrator\" -SurName \"Administrator\" -Type \"User\"
```

Creates a user with complete profile information including names and user type.

### Example 3
```powershell
New-PmUser developer@company.com -GroupName \"Developers\", \"Automation Express\" -InvitationAccepted \"True\"
```

Creates a user and assigns them to multiple groups with invitation pre-accepted.

### Example 4
```powershell
New-PmUser -Path Orch1: finance@company.com -DisplayName \"Finance Manager\" -BypassBasicAuthRestriction \"True\" -WhatIf
```

Shows what would happen when creating a user in a specific tenant with authentication bypass enabled.

## PARAMETERS

### -BypassBasicAuthRestriction
{{ Fill BypassBasicAuthRestriction Description }}

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
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisplayName
{{ Fill DisplayName Description }}

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

### -Email
Specifies the email address of the user to create. This serves as the primary identifier and must be unique.

```yaml
Type: String
Parameter Sets: (All)
Aliases: UserName

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -GroupName
Specifies the group(s) to which the user should be assigned upon creation.

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

### -InvitationAccepted
{{ Fill InvitationAccepted Description }}

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
Specifies the username for the user account.

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

### -Path
Specifies the path(s) to target tenant(s). Use drive names like Orch1:, Orch2: to specify tenant context.

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

### -SurName
{{ Fill SurName Description }}

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

### -Type
{{ Fill Type Description }}

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
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
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

### System.String
### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmUser
## NOTES

## RELATED LINKS

[Get-PmUser](Get-PmUser.md)
[Update-PmUser](Update-PmUser.md)
[Remove-PmUser](Remove-PmUser.md)
[Copy-PmUser](Copy-PmUser.md)
