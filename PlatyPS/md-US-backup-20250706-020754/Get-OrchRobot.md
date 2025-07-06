---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchRobot

## SYNOPSIS
Gets robots autoprovisioned from users in UiPath Orchestrator.

## SYNTAX

```
Get-OrchRobot [[-FullName] <String[]>] [[-Username] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchRobot` cmdlet retrieves information about robots that are autoprovisioned from users in UiPath Orchestrator. These robots are automatically created based on user accounts and can be either attended or unattended robots.

This cmdlet returns comprehensive robot information including robot details, associated user information, hosting type, provisioning type, license information, and configuration settings. The robots returned are those configured to be available for automation execution.

The cmdlet supports filtering by user full name and username using wildcards, making it easy to find robots associated with specific users or matching specific patterns.

Multiple values for the -FullName, -Username, and -Path parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

Primary Endpoint: GET /odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots?$expand=User

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Users.View and Robots.View

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1:\
PS Orch1:\> Get-OrchRobot
```

Gets all autoprovisioned robots in the current Orchestrator instance.

### Example 2
```powershell
PS Orch1:\> Get-OrchRobot -FullName "*John*"
```

Gets robots associated with users whose full name contains "John".

### Example 3
```powershell
PS Orch1:\> Get-OrchRobot -Username "*user*"
```

Gets robots associated with users whose username contains "user".

### Example 4
```powershell
PS Orch1:\> Get-OrchRobot | Where-Object Type -eq "Unattended"
```

Gets only unattended robots from all autoprovisioned robots.

### Example 5
```powershell
PS Orch1:\> $robot = Get-OrchRobot | Select-Object -First 1
PS Orch1:\> $robot | ConvertTo-Json -Depth 5
```

Gets the first robot and displays its complete structure in JSON format to see all available properties including nested User object details.

### Example 6
```powershell
PS Orch1:\> Get-OrchRobot | Select-Object Name, Type, HostingType, Enabled | Format-Table
```

Gets all robots and displays key properties in a formatted table.

### Example 7
```powershell
PS Orch1:\> Get-OrchRobot | Where-Object Enabled -eq $false
```

Gets robots that are currently disabled.

### Example 8
```powershell
PS Orch1:\> Get-OrchRobot | Group-Object Type | Select-Object Name, Count
```

Groups robots by type (StudioPro, Unattended, etc.) and shows the count for each type.

### Example 9
```powershell
PS Orch1:\> $robots = Get-OrchRobot
PS Orch1:\> $robots | ForEach-Object { 
>>     [PSCustomObject]@{
>>         RobotName = $_.Name
>>         UserFullName = $_.User.FullName
>>         UserType = $_.User.Type
>>         RobotType = $_.Type
>>         LastLogin = $_.User.LastLoginTime
>>         IsActive = $_.User.IsActive
>>         ProvisionType = $_.ProvisionType
>>     }
>> } | Format-Table
```

Creates a custom summary showing robot and associated user information.

### Example 10
```powershell
PS Orch1:\> Get-OrchRobot | Where-Object { $_.User.Type -eq "DirectoryRobot" }
```

Gets robots where the associated user type is "DirectoryRobot".

### Example 11
```powershell
PS Orch1:\> Get-OrchRobot -FullName "John Tsuda" | 
>> Select-Object Name, Type, @{N="UserEmail";E={$_.User.EmailAddress}}, @{N="CreationTime";E={$_.User.CreationTime}}
```

Gets robots for a specific user and displays custom properties including user email and creation time.

### Example 12
```powershell
PS Orch1:\> Get-OrchRobot | Where-Object { $_.HostingType -eq "Floating" }
```

Gets robots with floating hosting type.

### Example 13
```powershell
PS Orch1:\> Get-OrchRobot | Where-Object { $_.User.AuthenticationSource -eq "local" }
```

Gets robots associated with locally authenticated users.

## PARAMETERS

### -FullName
Specifies the full name(s) of the users whose robots should be retrieved. Supports wildcards and multiple values. You can use autocomplete by pressing [Ctrl+Space] or [Tab].

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: All users
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted. This parameter accepts pipeline input for specifying multiple Orchestrator instances.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider, such as the progress bars generated by the Write-Progress cmdlet. Valid values are: SilentlyContinue, Stop, Continue, Inquire, Ignore, Suspend.

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

### -Username
Specifies the username(s) of the users whose robots should be retrieved. Supports wildcards and multiple values. You can use autocomplete by pressing [Ctrl+Space] or [Tab].

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All users
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Robot
## NOTES
- This cmdlet returns robots that are autoprovisioned from user accounts
- The User property contains a nested object with complete user information including authentication details, session permissions, and licensing information
- Robot types include StudioPro (attended) and Unattended robots
- HostingType indicates whether the robot uses Floating or Standard licensing
- ProvisionType shows whether the robot was created Automatically or Manually
- User.Type values include DirectoryUser, DirectoryRobot, and others depending on the user account type
- Use ConvertTo-Json to explore the complete object structure including all nested User properties
- The MayHaveUserSession, MayHaveRobotSession, and MayHaveUnattendedSession properties in the User object indicate session permissions
- Both -FullName and -Username parameters support wildcard filtering and do not change the display format

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)
[Get-OrchUser](Get-OrchUser.md)
[Get-OrchClassicRobot](Get-OrchClassicRobot.md)
[about_UiPathOrch](about_UiPathOrch.md)



