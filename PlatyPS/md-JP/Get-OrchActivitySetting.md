---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchActivitySetting

## SYNOPSIS
UiPath Orchestrator からアクティビティ設定を取得します。

## SYNTAX

```
Get-OrchActivitySetting [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchActivitySetting` コマンドレットは、UiPath Orchestrator からアクティビティ関連の設定を取得します。これには、アクティビティに使用される API バージョン、アクティビティとのリアルタイム通信用の SignalR 接続設定、および関連する設定オプションが含まれます。

これらの設定は、アクティビティが Orchestrator と通信する方法を制御し、アクティビティの実行と監視のリアルタイムシグナリングを管理します。

-Path パラメーターの複数の値は、ワイルドカードを含むコンマ区切りのテキストを使用して指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値のオートコンプリートを使用できます。

プライマリエンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetActivitySettings

OAuth 必須スコープ: OR.Settings または OR.Settings.Read

必要なアクセス許可: [PLACEHOLDER - アクティビティ設定表示のアクセス許可]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchActivitySetting
```

現在の Orchestrator インスタンスのアクティビティ設定を取得します。

### Example 2
```powershell
PS Orch1:\> $setting = Get-OrchActivitySetting; $setting.SignalR
```

アクティビティ設定を取得し、SignalR 設定の詳細を表示します。

### Example 3
```powershell
PS C:\> Get-OrchActivitySetting -Path Orch1:, Orch2:
```

複数のテナントからアクティビティ設定を取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchActivitySetting | ConvertTo-Json -Depth 2
```

アクティビティ設定を取得し、ネストされた SignalR プロパティを含む完全な構造を表示します。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定されていない場合、現在のドライブがターゲットになります。このパラメーターはパイプライン入力を受け入れ、複数のパスを指定するためのワイルドカードをサポートします。

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
Write-Progress コマンドレットによって生成される進行状況バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend です。

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

### UiPath.PowerShell.Entities.ActivitySettings
## NOTES
- ApiVersion プロパティは、アクティビティ通信に使用される API バージョンを示します
- SignalR.Url プロパティには、リアルタイム SignalR 通信の URL が含まれます
- SignalR.SkipNegotiation プロパティは、SignalR ネゴシエーションがバイパスされるかどうかを制御します
- これらの設定は、適切なアクティビティ実行とリアルタイム監視に不可欠です

プライマリエンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetActivitySettings
OAuth 必須スコープ: OR.Settings または OR.Settings.Read
必要なアクセス許可: Settings.View

## RELATED LINKS

[Get-OrchSetting](Get-OrchSetting.md)
[about_UiPathOrch](about_UiPathOrch.md)
