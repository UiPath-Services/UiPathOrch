---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUserPrivilege

## SYNOPSIS
Retrieves user privileges based on the union of privileges model.

## SYNTAX

```
Get-OrchUserPrivilege [[-UserName] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchUserPrivilege cmdlet retrieves comprehensive user privilege information from UiPath Orchestrator. This cmdlet provides detailed insights into user permissions, role assignments, access levels, and session privileges across the Orchestrator environment.

This is a tenant entity cmdlet. The -Path parameter specifies target tenants using drive names (e.g., Orch1:, Orch2:). If not specified, the current tenant will be targeted.

The cmdlet returns extensive privilege information including explicit and inherited roles, access permissions, attended session privileges, personal workspace permissions, and update policies. Each privilege category shows explicit assignments, inherited permissions from group memberships, and the effective (final) permissions that apply to the user.

This information is essential for understanding the complete permission landscape for users, troubleshooting access issues, auditing user privileges, and ensuring proper security configuration.

Primary Endpoint: GET /api/Users/GetPrivileges?userId={userId}

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1
```powershell
Get-OrchUserPrivilege
```

Gets privilege information for all users in the current tenant.

### Example 2
```powershell
Get-OrchUserPrivilege john.doe
```

Gets privilege information for the user "john.doe" in the current tenant.

### Example 3
```powershell
Get-OrchUserPrivilege *admin*
```

Gets privilege information for all users whose names contain "admin".

### Example 4
```powershell
Get-OrchUserPrivilege -Path Orch1:, Orch2: administrator
```

Gets privilege information for the "administrator" user across multiple tenants.

### Example 5
```powershell
Get-OrchUserPrivilege | Where-Object {$_.ExplicitRoles.Count -gt 0}
```

Gets all users who have explicit role assignments (not just inherited from groups).

### Example 6
```powershell
Get-OrchUserPrivilege | Select-Object UserName, AccessLevel, @{Name="TotalRoles"; Expression={$_.EffectiveRoles.Count}}
```

Gets all user privileges and displays username, access level, and total effective role count.

### Example 7
```powershell
Get-OrchUser admin* | Get-OrchUserPrivilege | ConvertTo-Json -Depth 5
```

Gets privilege information for admin users via pipeline and exports detailed privilege structure to JSON.

## PARAMETERS

### -Path
Specifies the name of the target tenants using drive names. If not specified, the current tenant will be targeted.

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

### -UserName
Specifies the user names for which privilege information should be retrieved.

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
User names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.User
User objects can be piped to this cmdlet. The UserName property will be automatically mapped to the -UserName parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.UserPrivilege

## NOTES

## RELATED LINKS
