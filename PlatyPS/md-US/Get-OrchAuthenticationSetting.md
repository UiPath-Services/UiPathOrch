---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAuthenticationSetting

## SYNOPSIS
Retrieves authentication and system configuration settings from UiPath Orchestrator.

## SYNTAX

```
Get-OrchAuthenticationSetting [[-Key] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchAuthenticationSetting cmdlet retrieves authentication and system configuration settings from UiPath Orchestrator. These settings control various aspects of authentication, authorization, application behavior, and system configuration including build information, culture settings, and external service integrations.

Settings include authentication configurations (Auth.*), application properties (Application.*), build information (Build.*), localization settings, token authentication options, and telemetry configurations. Each setting is returned as a key-value pair showing the configuration parameter and its current value.

This cmdlet operates as a tenant-level entity operation, retrieving system-wide configuration settings from the specified Orchestrator environment. These settings are typically read-only and reflect the current system configuration.

Primary Endpoint: GET /odata/Settings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchAuthenticationSetting
```

Retrieves all authentication and system settings, displaying Key and Value pairs for all configuration parameters.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchAuthenticationSetting | Where-Object {$_.Key -like "Auth.*"}
```

Gets all authentication-related settings by filtering keys that start with "Auth.".

### Example 3
```powershell
PS C:\> Get-OrchAuthenticationSetting -Path Orch1: -Key "*Token*"
```

Gets all settings with keys containing "Token" in the Orch1 tenant.

### Example 4
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting | Where-Object {$_.Key -like "Build.*"}
```

Retrieves all build-related information including version and date.

### Example 5
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting | Select-Object Key, Value | Sort-Object Key
```

Displays all settings sorted alphabetically by key for organized viewing.

### Example 6
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting | Where-Object {$_.Value -eq "True"} | Select-Object Key
```

Shows all settings that are currently enabled (value = "True").

## PARAMETERS

### -Key
Specifies the keys of authentication settings to be retrieved. Supports wildcard patterns for flexible setting selection.

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
Specifies the target tenant drives. If not specified, the current tenant will be targeted. For tenant-level operations.

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

### System.String[]
Setting keys can be piped to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.AuthenticationSetting
Returns AuthenticationSetting objects containing system configuration information. Key properties include:
- Key: Configuration parameter name (e.g., "Auth.AllowChangePassword", "Build.Version")
- Value: Current configuration value

Common setting categories include:
- Auth.*: Authentication and authorization settings
- Application.*: Application-level configuration
- Build.*: Version and build information
- Localization.*: Culture and language settings
- Telemetry.*: Analytics and monitoring configuration
- *Authentication.Enabled: Token authentication options

## NOTES
This cmdlet is a tenant-level entity operation for accessing system configuration settings. These settings control authentication behavior, application properties, and system features. Most settings are read-only and reflect the current Orchestrator configuration. Use filtering by Key patterns to find specific configuration categories. This operation requires Settings.View permissions.

## RELATED LINKS

[Get-OrchSetting](Get-OrchSetting.md)

[Get-OrchExecutionSetting](Get-OrchExecutionSetting.md)

[Set-OrchSetting](Set-OrchSetting.md)

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
