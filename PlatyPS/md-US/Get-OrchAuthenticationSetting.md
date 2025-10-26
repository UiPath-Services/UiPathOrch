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

Authentication settings are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name (e.g., Orch1:, Orch2:).

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
PS Orch1:\> Get-OrchAuthenticationSetting Auth.*
```

Gets authentication-related settings using wildcard key filtering.

### Example 3
```powershell
PS C:\> Get-OrchAuthenticationSetting -Path Orch1:, Orch2: -Key *Token*
```

Gets settings with keys containing "Token" from multiple tenants.

### Example 4
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting | Select-Object Path, Key, Value | Sort-Object Key
```

Displays all settings sorted alphabetically by key with Path shown first.

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
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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
This cmdlet is a tenant-level entity operation for accessing system configuration settings. These settings control authentication behavior, application properties, and system features. Most settings are read-only and reflect the current Orchestrator configuration. Use filtering by Key patterns to find specific configuration categories. This operation requires Settings.View permissions.



Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings
OAuth required scopes: OR.Settings or OR.Settings.Read
Required permissions: Settings.View

## RELATED LINKS

[Get-OrchSetting](Get-OrchSetting.md)

[Get-OrchExecutionSetting](Get-OrchExecutionSetting.md)


[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
