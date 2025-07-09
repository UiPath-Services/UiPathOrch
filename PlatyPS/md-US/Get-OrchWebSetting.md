---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchWebSetting

## SYNOPSIS
Gets web configuration settings.

## SYNTAX

```
Get-OrchWebSetting [[-Key] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchWebSetting cmdlet retrieves web configuration settings from UiPath Orchestrator. Web settings control various aspects of the web interface, user experience, security policies, and system behavior.

This is a tenant entity cmdlet. The -Path parameter specifies target tenants using drive names (e.g., Orch1:, Orch2:). If not specified, the current tenant will be targeted.

Web settings include authentication configurations, user interface preferences, security policies, feature toggles, and integration settings. These settings affect how users interact with Orchestrator and control system-wide behaviors.

Primary Endpoint: GET /odata/WebSettings

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.View

## EXAMPLES

### Example 1
```powershell
Get-OrchWebSetting
```

Gets all web settings in the current tenant.

### Example 2
```powershell
Get-OrchWebSetting Authentication
```

Gets the web setting named "Authentication".

### Example 3
```powershell
Get-OrchWebSetting *Auth*
```

Gets all web settings whose names contain "Auth".

### Example 4
```powershell
Get-OrchWebSetting -Path Orch1:, Orch2: SessionTimeout
```

Gets the "SessionTimeout" web setting across multiple tenants.

### Example 5
```powershell
Get-OrchWebSetting | Where-Object {$_.Category -eq "Security"}
```

Gets all web settings in the Security category.

### Example 6
```powershell
Get-OrchWebSetting | Select-Object Name, Value, Category, Description | Format-Table
```

Gets all web settings and displays them in a formatted table with key properties.

### Example 7
```powershell
Get-OrchWebSetting | Where-Object {$_.IsModified -eq $true} | ConvertTo-Json
```

Gets all web settings that have been modified from their default values and exports them to JSON format.

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
Specifies the name of the target tenants using drive names. If not specified, the current tenant will be targeted.

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

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings
OAuth required scopes: OR.Settings or OR.Settings.Read
Required permissions: Settings.View

## RELATED LINKS
