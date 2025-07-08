---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUserSession

## SYNOPSIS
Gets user session information.

## SYNTAX

```
Get-OrchUserSession [-State <String[]>] [-Type <String[]>] [-OrderBy <String[]>] [-Skip <UInt64>]
 [-First <UInt64>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchUserSession cmdlet retrieves active and historical user session information from UiPath Orchestrator. User sessions represent interactive connections between users and Orchestrator, including login times, session duration, and connection details.

This is a tenant entity cmdlet. The -Path parameter specifies target tenants using drive names (e.g., Orch1:, Orch2:). If not specified, the current tenant will be targeted.

Session information is useful for monitoring user activity, security auditing, license usage tracking, and troubleshooting connection issues. The cmdlet provides insights into user access patterns and system utilization.

Primary Endpoint: GET /odata/UserSessions

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1
```powershell
Get-OrchUserSession
```

Gets session information for all users in the current tenant.

### Example 2
```powershell
Get-OrchUserSession john.doe
```

Gets session information for the user "john.doe".

### Example 3
```powershell
Get-OrchUserSession *admin*
```

Gets session information for all users whose names contain "admin".

### Example 4
```powershell
Get-OrchUserSession -Path Orch1:, Orch2: developer
```

Gets session information for the "developer" user across multiple tenants.

### Example 5
```powershell
Get-OrchUserSession | Where-Object {$_.IsActive -eq $true}
```

Gets all currently active user sessions.

### Example 6
```powershell
Get-OrchUserSession | Select-Object UserName, LoginTime, LastActivity, IsActive, SessionDuration
```

Gets all user sessions and displays key timing and status information.

### Example 7
```powershell
Get-OrchUser | Get-OrchUserSession | Where-Object {$_.LastActivity -gt (Get-Date).AddHours(-1)}
```

Gets session information for all users who have been active within the last hour. User information is passed via pipeline using ByPropertyName binding.

## PARAMETERS

### -OrderBy
Specifies the item to sort the sessions retrieved by.

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

### -State
Specifies the State of the sessions to be retrieved.

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

### -Type
Specifies the Type of the sessions to be retrieved.

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

### -Skip
Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip.

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
Gets only the specified number of objects. Enter the number of objects to get.

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### UiPath.PowerShell.Entities.UserSession

## NOTES



Primary Endpoint: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetGlobalSessions
OAuth required scopes: OR.Robots or OR.Robots.Read
Required permissions: Robots.View

## RELATED LINKS
