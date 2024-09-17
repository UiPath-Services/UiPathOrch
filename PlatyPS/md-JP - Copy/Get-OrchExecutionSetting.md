---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchExecutionSetting

## SYNOPSIS
実行設定を取得します。

## SYNTAX

```
Get-OrchExecutionSetting [[-Scope] <String[]>] [[-DisplayName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
実行設定の構成（表示名、値の種類など）を取得します。スコープが Global の場合、デフォルト値は初期値になります。スコープが Robot の場合、デフォルト値はグローバルに設定された実際の値になります。例えば、解像度の幅がグローバルに 720 に設定されていたとします。この関数から返される構成内で、この設定のデフォルト値は、スコープが Global の場合は 0、スコープが Robot の場合は 720 になります。

主に呼び出すエンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetExecutionSettingsConfiguration(scope={scope})

OAuth に必要なスコープ: OR.Settings or OR.Settings.Read

必要な権限: Settings.Edit or Robots.Create or Robots.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -DisplayName
取得する設定の DisplayName を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
ターゲットとするドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Scope
設定を取得する Scope を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
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

### UiPath.PowerShell.Entities.ExecutionSettingDefinition
## NOTES

## RELATED LINKS
