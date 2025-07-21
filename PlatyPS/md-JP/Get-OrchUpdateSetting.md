---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUpdateSetting

## SYNOPSIS
UiPath Orchestratorからアップデート設定を取得します。

## SYNTAX

```
Get-OrchUpdateSetting [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchUpdateSettingコマンドレットは、UiPath Orchestratorからアップデート構成設定を取得します。これには、Orchestratorがアップデートを確認し管理する方法を制御するアップデートサーバーソース、アップデートサーバーURL、およびポーリング間隔設定が含まれます。

これはテナントスコープのエンティティです。-Pathパラメータは、特定のOrchestratorテナントを対象とするドライブ名（例：Orch1:、Orch2:）を指定します。

プライマリ エンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetUpdateSettings

OAuth 必要スコープ: OR.Settings または OR.Settings.Read

必要な権限: Settings.View

## EXAMPLES

### Example 1: 現在のテナントのアップデート設定を取得
```powershell
PS C:\> Get-OrchUpdateSetting -Path Orch1:
```

Orch1テナントのアップデート設定を取得します。

### Example 2: 複数のテナントからアップデート設定を取得
```powershell
PS C:\> Get-OrchUpdateSetting -Path Orch1:, Orch2:, Orch3:
```

複数のOrchestratorテナントからアップデート設定を取得します。

### Example 3: 特定のプロパティを表示
```powershell
PS C:\> Get-OrchUpdateSetting -Path Orch1: | Select-Object Path, UpdateServerSource, PollingInterval
```

アップデート設定を取得し、主要な構成プロパティのみを表示します。

### Example 4: 詳細な分析のためのJSON変換
```powershell
PS C:\> Get-OrchUpdateSetting -Path Orch1:, Orch2: | ConvertTo-Json
```

複数のテナントからアップデート設定を取得し、詳細な分析のためにJSONに変換します。

## PARAMETERS

### -Path
特定のOrchestratorテナントを対象とするドライブ名を指定します。カンマ区切りの値をサポートします。[Ctrl+Space]または[Tab]で自動補完が利用できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進捗更新にPowerShellがどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.UpdateSettings
## NOTES
- UpdateServerSourceはアップデートソースのタイプを示します（通常は"Orchestrator"）
- UpdateServerUrlにはアップデートサーバーの完全なURLが含まれます
- PollingIntervalはアップデートを確認する頻度（分単位）を指定します
- これらの設定は自動アップデート確認動作を制御します



プライマリ エンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetUpdateSettings
OAuth 必要スコープ: OR.Settings または OR.Settings.Read
必要な権限: Settings.View

## RELATED LINKS

[Get-OrchSetting](Get-OrchSetting.md)
[about_UiPathOrch](about_UiPathOrch.md)
