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
PS Orch1:\> Add-OrchUser DirectoryUser testuser1 -WhatIf
```

Adds the user testuser1 from the organization to the current tenant.

### Example 2
```powershell
PS C:\> Add-OrchUser -Path Orch1:, Orch2: DirectoryUser testuser2 Developer -WhatIf
```

Adds the user testuser2 to both Orch1 and Orch2 tenants with the Developer role.

### Example 3
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser testuser3 Administrator, Developer -MayHaveUserSession True -WhatIf
```

Adds testuser3 with Administrator and Developer roles, and enables Orchestrator web access to the user.

### Example 4
```powershell
PS Orch1:\> Add-OrchUser DirectoryRobot RobotAccount1 -WhatIf
```

Adds RobotAccount1 as a DirectoryRobot user type.

### Example 5
```powershell
PS C:\> Import-Csv users.csv | Add-OrchUser -WhatIf
```

Shows what would happen when adding users from a CSV file to Orch1 tenant. This CSV file can be generated with Get-OrchUser -ExportCsv.
Minimal CSV format:
Path,Type,UserName,Roles
Orch1:,DirectoryUser,john.doe,Developer
Orch1:,DirectoryUser,jane.smith,Administrator

### Example 6
```powershell
PS Orch1:\> Get-PmUser *@contoso.com | Add-OrchUser -Roles Developer -WhatIf
```

Gets all organization users with contoso.com email domain and adds them to the current tenant with Developer role.

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

### -ES_AutoDownloadProcess
[PLACEHOLDER - requires verification of ES_AutoDownloadProcess parameter description]

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
{{ Fill ES_FontSmoothing Description }}

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
{{ Fill ES_LoginToConsole Description }}

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
{{ Fill ES_ResolutionDepth Description }}

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
{{ Fill ES_ResolutionHeight Description }}

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
{{ Fill ES_ResolutionWidth Description }}

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
{{ Fill ES_StudioNotifyServer Description }}

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
{{ Fill ES_TracingLevel Description }}

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
{{ Fill MayHavePersonalWorkspace Description }}

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
{{ Fill MayHaveRobotSession Description }}

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
{{ Fill MayHaveUnattendedSession Description }}

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
{{ Fill MayHaveUserSession Description }}

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
{{ Fill RestrictToPersonalWorkspace Description }}

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
{{ Fill UpdatePolicyType Description }}

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
{{ Fill UpdatePolicyVersion Description }}

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
{{ Fill UR_CredentialStore Description }}

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
{{ Fill UR_CredentialType Description }}

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
{{ Fill UR_LimitConcurrentExecution Description }}

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
{{ Fill UR_Password Description }}

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
{{ Fill UR_UserName Description }}

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
{{ Fill UR_CredentialExternalName Description }}

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
{{ Fill IsExternalLicensed Description }}

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
