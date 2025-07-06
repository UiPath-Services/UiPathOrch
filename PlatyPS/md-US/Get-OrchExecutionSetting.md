---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchExecutionSetting

## SYNOPSIS
Gets the execution settings configuration.

## SYNTAX

```
Get-OrchExecutionSetting [[-Scope] <String[]>] [[-DisplayName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Gets the execution settings configuration (display name, value type, etc.). If scope is Global, the default values will be the initial ones. If scope is Robot, then the default values will be the actual values set globally. e.g., Resolution width Assume it was set globally to 720. Then within the config returned by this function, the default value for this setting will be 0 if the scope is Global and 720 if the scope is Robot.

Primary Endpoint: GET /odata/Settings/UiPath.Server.Configuration.OData.GetExecutionSettingsConfiguration(scope={scope})

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.Edit or Robots.Create or Robots.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -DisplayName
{{ Fill DisplayName Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
{{ Fill Path Description }}

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

### -Scope
{{ Fill Scope Description }}

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

## OUTPUTS

### UiPath.PowerShell.Entities.ExecutionSettingDefinition

## NOTES

## RELATED LINKS
