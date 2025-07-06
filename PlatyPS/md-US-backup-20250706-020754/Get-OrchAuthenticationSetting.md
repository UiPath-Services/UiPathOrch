---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAuthenticationSetting

## SYNOPSIS
Gets authentication settings.

## SYNTAX

```
Get-OrchAuthenticationSetting [[-Key] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchAuthenticationSetting` cmdlet retrieves authentication configuration settings from UiPath Orchestrator. These settings control various aspects of user authentication, password policies, external authentication integration, and access token management.

The cmdlet returns authentication-related settings including password change policies, self-service capabilities, external logout configuration, and token authentication enablement for different types of access (user, robot, and service-to-service authentication). Additionally, it includes basic application settings, build information, localization settings, and telemetry configuration.

Key authentication settings include controls for password management (Auth.AllowChangePassword), user self-service features (Auth.AllowSelfEmailUpdate), external authentication integration (Auth.ExternalLogoutUrl), and access token authentication enablement for various client types (UserAccessTokenAuthentication.Enabled, RobotAccessTokenAuthentication.Enabled, S2SAccessTokenAuthentication.Enabled).

These settings are essential for understanding the authentication landscape of the Orchestrator instance, configuring security policies, troubleshooting authentication issues, and ensuring proper integration with external identity providers and authentication systems.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings

OAuth required scopes: [PLACEHOLDER - Authentication settings scopes]

Required permissions: [PLACEHOLDER - Authentication settings view permissions]

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Key
Specifies the Key of the settings to be retrieved.

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
Specifies the name of the target drives. If not specified, the current drive will be targeted.

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ResponseDictionaryItem
## NOTES

## RELATED LINKS
