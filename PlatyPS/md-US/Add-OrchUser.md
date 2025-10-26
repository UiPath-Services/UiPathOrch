---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchUser

## SYNOPSIS
Adds users to tenants.

## SYNTAX

```
Add-OrchUser [[-Type] <String[]>] [-UserName] <String[]> [[-Roles] <String[]>] [-IsExternalLicensed <String>]
 [-MayHaveUserSession <String>] [-MayHaveRobotSession <String>] [-MayHaveUnattendedSession <String>]
 [-MayHavePersonalWorkspace <String>] [-RestrictToPersonalWorkspace <String>] [-UpdatePolicyType <String>]
 [-UpdatePolicyVersion <String>] [-UR_UserName <String>] [-UR_CredentialStore <String>] [-UR_Password <String>]
 [-UR_CredentialExternalName <String>] [-UR_CredentialType <String>] [-UR_LimitConcurrentExecution <String>]
 [-ES_TracingLevel <String>] [-ES_StudioNotifyServer <String>] [-ES_LoginToConsole <String>]
 [-ES_ResolutionWidth <Int32>] [-ES_ResolutionHeight <Int32>] [-ES_ResolutionDepth <Int32>]
 [-ES_FontSmoothing <String>] [-ES_AutoDownloadProcess <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-OrchUser cmdlet assigns existing users from the organization to UiPath Orchestrator tenants. This cmdlet does not create new users; instead, it assigns users that already exist in the organization or in Active Directory that is federated with the organization or tenant.

Use Get-PmUser to retrieve organization users that can be assigned to tenants. Use Search-OrchDirectory to find users from federated Active Directory domains.

You can specify various tenant-specific settings for the assigned users, including roles, session permissions, execution settings (ES_*), and unattended robot credentials (UR_*). The cmdlet allows you to configure licensing, workspace restrictions, and update policies for the assigned users.

Use the -Path parameter to target specific Orchestrator drives when working with multiple instances. The -UserName parameter accepts usernames of existing organization or directory users.

Primary Endpoint: GET /odata/Users?$expand=OrganizationUnits,UserRoles, GET /odata/Roles?$expand=Permissions, GET /api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All, POST /odata/Users

OAuth required scopes: OR.Users or OR.Users.Read or OR.Users.Write

Required permissions: (Users.View or Units.Edit or SubFolders.Edit), (Roles.View or Units.Edit or SubFolders.Edit)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser user@domain.com -WhatIf
```

Tests adding the user user@domain.com as a DirectoryUser to the current tenant without actually performing the operation.

### Example 2
```powershell
PS C:\> Add-OrchUser -Path Orch1:, Orch2: DirectoryUser user@domain.com -Roles "Automation Developer" -WhatIf
```

Tests adding user@domain.com to both Orch1 and Orch2 tenants with the "Automation Developer" tenant role.

### Example 3
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser user@domain.com "Orchestrator Administrator" -MayHaveUserSession True -WhatIf
```

Tests adding user@domain.com with the Orchestrator Administrator role and enables Orchestrator web access.

### Example 4
```powershell
PS Orch1:\> Add-OrchUser DirectoryRobot robot@domain.com -WhatIf
```

Tests adding robot@domain.com as a DirectoryRobot user type for unattended automation scenarios.

### Example 5
```powershell
PS C:\> Import-Csv users.csv | Add-OrchUser -WhatIf
```

Tests adding users from a CSV file to the current tenant. The CSV file should contain columns: Type, UserName, Roles, Path. Minimal CSV format: "Type,UserName,Roles,Path" with entries like "DirectoryUser,john.doe@domain.com,Allow to be Automation Developer,Orch1:".

### Example 6
```powershell
PS Orch1:\> Get-PmUser *@uipath.com | Add-OrchUser DirectoryUser -Roles "Automation Developer" -WhatIf
```

Tests getting all organization users with uipath.com email domain and adding them to the current tenant with the "Automation Developer" role.

### Example 7
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser user@domain.com -UR_UserName "DOMAIN\user" -UR_Password Password123 -UR_CredentialType Default -WhatIf
```

Tests adding a user with unattended robot credentials for automation execution.

### Example 8
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser user@domain.com -ES_TracingLevel Verbose -ES_ResolutionWidth 1920 -ES_ResolutionHeight 1080 -WhatIf
```

Tests adding a user with specific execution settings including logging level and screen resolution for robot execution.

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

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted.

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

### -Roles
Specifies the Roles to add to the users.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: TenantRoles

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Type
Specifies the Type of the users to be added. Valid Values: DirectoryUser, DirectoryRobot, DirectoryGroup, DirectoryExternalApplication

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

### -UserName
Specifies the UserName of the users to be added.

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
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider.

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

### -ES_AutoDownloadProcess
Specifies whether the robot should automatically download the latest process versions. Valid values: True, False.

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

### -ES_FontSmoothing
Specifies the font smoothing setting for the robot's execution environment. Valid values: True, False.

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

### -ES_LoginToConsole
Specifies whether the robot should log into the console session. Valid values: True, False.

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

### -ES_ResolutionDepth
Specifies the color depth (bits per pixel) for the robot's screen resolution. Common values: 16, 24, 32.

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

### -ES_ResolutionHeight
Specifies the screen height in pixels for the robot's execution environment. Common values: 720, 1080, 1440.

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

### -ES_ResolutionWidth
Specifies the screen width in pixels for the robot's execution environment. Common values: 1280, 1920, 2560.

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

### -ES_StudioNotifyServer
Specifies whether Studio should notify the server about process changes. Valid values: True, False.

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

### -ES_TracingLevel
Specifies the logging level for robot execution. Valid values: Off, Critical, Error, Warning, Information, Verbose.

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

### -MayHavePersonalWorkspace
Specifies whether the user can access personal workspace features. Valid values: True, False.

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

### -MayHaveRobotSession
Specifies whether the user can have robot sessions for attended automation. Valid values: True, False.

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

### -MayHaveUnattendedSession
Specifies whether the user can have unattended robot sessions for background automation. Valid values: True, False.

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

### -MayHaveUserSession
Specifies whether the user can access the Orchestrator web interface. Valid values: True, False.

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

### -RestrictToPersonalWorkspace
Specifies whether the user should be restricted to personal workspace only. Valid values: True, False.

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

### -UpdatePolicyType
Specifies the update policy type for the user. Defines how robot updates should be handled.

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

### -UpdatePolicyVersion
Specifies the specific version for the update policy.

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

### -UR_CredentialStore
Specifies the credential store for unattended robot credentials. Use Get-OrchCredentialStore to see available stores.

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

### -UR_CredentialType
Specifies the credential type for unattended robot authentication. Valid values: Default, SmartCard, WindowsCredentials.

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

### -UR_LimitConcurrentExecution
Specifies whether to limit concurrent execution for the unattended robot. Valid values: True, False.

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

### -UR_Password
Specifies the password for unattended robot authentication. This should be the domain/local account password.

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

### -UR_UserName
Specifies the username for unattended robot authentication. Format: DOMAIN\username or local username.

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

### -UR_CredentialExternalName
Specifies the external name for unattended robot credentials when using external credential stores.

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

### -IsExternalLicensed
Specifies whether the user should be licensed externally. Valid values: True, False.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES

## RELATED LINKS

[Get-PmUser](Get-PmUser.md)
[Get-OrchUser](Get-OrchUser.md)
[Get-OrchRole](Get-OrchRole.md)
[Remove-OrchUser](Remove-OrchUser.md)
[Search-OrchDirectory](Search-OrchDirectory.md)
