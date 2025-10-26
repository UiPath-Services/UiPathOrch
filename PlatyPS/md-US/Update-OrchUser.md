---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchUser

## SYNOPSIS
Updates users.

## SYNTAX

```
Update-OrchUser [-UserName] <String[]> [-Name <String>] [-Surname <String>] [-Roles <String[]>]
 [-IsExternalLicensed <String>] [-MayHaveUserSession <String>] [-MayHaveRobotSession <String>]
 [-MayHaveUnattendedSession <String>] [-MayHavePersonalWorkspace <String>]
 [-RestrictToPersonalWorkspace <String>] [-UpdatePolicyType <String>] [-UpdatePolicyVersion <String>]
 [-UR_UserName <String>] [-UR_CredentialStore <String>] [-UR_Password <String>]
 [-UR_CredentialExternalName <String>] [-UR_CredentialType <String>] [-UR_LimitConcurrentExecution <String>]
 [-ES_TracingLevel <String>] [-ES_StudioNotifyServer <String>] [-ES_LoginToConsole <String>]
 [-ES_ResolutionWidth <String>] [-ES_ResolutionHeight <String>] [-ES_ResolutionDepth <String>]
 [-ES_FontSmoothing <String>] [-ES_AutoDownloadProcess <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
[PLACEHOLDER - requires verification of description]

Primary Endpoint: GET /odata/Users, GET /odata/Users({userId}), PUT /odata/Users({userId})

OAuth required scopes: OR.Users

Required permissions: Users.View, Users.Edit or Robots.Create or Robots.Edit or Robots.Delete.

## EXAMPLES

### Example 1
```powershell
{{ Add example code here }}
```

{{ Add example description here }}

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

### -MayHavePersonalWorkspace
[PLACEHOLDER - requires verification of MayHavePersonalWorkspace parameter]

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
[PLACEHOLDER - requires verification of MayHaveRobotSession parameter]

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
MayHaveUnattendedSessionを指定します。

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
MayHaveUserSessionを指定します。

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

### -RestrictToPersonalWorkspace
RestrictToPersonalWorkspaceを指定します。

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

### -Roles
Rolesを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: TenantRoles

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UR_CredentialStore
UR_CredentialStoreを指定します。

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
UR_CredentialTypeを指定します。

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
UR_LimitConcurrentExecutionを指定します。

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
UR_Passwordを指定します。

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
UR_UserNameを指定します。

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
UpdatePolicyTypeを指定します。

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
UpdatePolicyVersionを指定します。

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

### -UserName
Specifies the UserName of the users to be updated.

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
ProgressActionを指定します。

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
ES_AutoDownloadProcessを指定します。

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
ES_FontSmoothingを指定します。

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
ES_LoginToConsoleを指定します。

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
ES_ResolutionDepthを指定します。

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

### -ES_ResolutionHeight
ES_ResolutionHeightを指定します。

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

### -ES_ResolutionWidth
ES_ResolutionWidthを指定します。

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

### -ES_StudioNotifyServer
ES_StudioNotifyServerを指定します。

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
ES_TracingLevelを指定します。

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
UR_CredentialExternalNameを指定します。

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
IsExternalLicensedを指定します。

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
Specifies the new Name (FirstName) of the user. Please note that this parameter is only effective in MSI Orchestrator.

```yaml
Type: String
Parameter Sets: (All)
Aliases: FirstName

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Surname
Specifies the new Surname (LastName) of the user. Please note that this parameter is only effective in MSI Orchestrator.

```yaml
Type: String
Parameter Sets: (All)
Aliases: LastName

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
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES

## RELATED LINKS
