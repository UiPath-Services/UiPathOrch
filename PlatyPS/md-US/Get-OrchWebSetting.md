---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchWebSetting

## SYNOPSIS
Gets the web settings.

## SYNTAX

```
Get-OrchWebSetting [[-Key] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchWebSetting` cmdlet retrieves web application configuration settings from UiPath Orchestrator. These settings control various aspects of the Orchestrator web interface, integrations, features, and operational behavior.

The cmdlet returns a comprehensive collection of web settings organized into multiple categories including AI Fabric integration, telemetry configuration, logging settings, deployment parameters, monitoring settings, feature toggles, licensing information, and user interface customization options.

These settings are essential for understanding how the Orchestrator instance is configured, troubleshooting integration issues, verifying feature availability, and managing operational parameters. The settings cover a wide range of functionality from basic application settings to advanced integration configurations.

Major setting categories include AiFabric (AI Center integration), Telemetry (monitoring and analytics), Deployment (package and library management), Features (feature toggles), General (basic application settings), Authentication, Monitoring, and many others.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings

OAuth required scopes: [PLACEHOLDER - Web settings scopes]

Required permissions: [PLACEHOLDER - Web settings view permissions]

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings

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
