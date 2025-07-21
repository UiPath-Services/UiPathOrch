---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchSetting

## SYNOPSIS
UiPath Orchestratorから設定情報を取得します。

## SYNTAX

```
Get-OrchSetting [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchSetting コマンドレットは、UiPath Orchestratorから設定情報を取得します。これには、ローカライゼーション、メール設定、認証設定、デプロイメント設定、機能トグル、その他の運用パラメーターなど、さまざまなシステム設定が含まれます。

設定は、メールサーバー構成、タイムゾーン設定、認証ポリシー、トリガー設定、機能の可用性など、Orchestratorの動作のさまざまな側面を制御します。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /odata/Settings

OAuth必須スコープ：OR.Settings または OR.Settings.Read

必須権限：Settings.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchSetting
```

現在のOrchestratorインスタンスからすべての設定情報を取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchSetting *Language*,*TimeZone*
```

ワイルドカードパターンを使用して、言語とタイムゾーンに関連する設定を取得します。

### Example 3
```powershell
PS C:\> Get-OrchSetting -Path Orch1:, Orch2: *Auth*
```

複数のテナントから認証関連の設定を取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchSetting *Email* | Select-Object Path, Name, Value
```

メール関連の設定を取得し、Pathを最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchSetting *Trigger* | ConvertTo-Json
```

トリガー関連の設定を取得し、詳細な構造をJSON形式で出力します。

## PARAMETERS

### -Name
取得する設定の名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべての設定が返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All settings
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
設定を検索するフォルダパスを指定します。このパラメーターは、パイプライン入力を受け入れ、複数のパスを指定するためのワイルドカードをサポートします。指定しない場合、現在の場所が使用されます。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新（Write-Progressコマンドレットによって生成される進行状況バーなど）にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### UiPath.PowerShell.Entities.Settings
## NOTES
- 設定には、さまざまなOrchestrator機能のシステム全体の設定が含まれます
- 一般的な設定カテゴリには次のものがあります：ローカライゼーション（Abp.Localization.*）、メール（Abp.Net.Mail.*）、認証（Auth.*）、デプロイメント（Deployment.*）、機能（Features.*）、トリガー（Triggers.*）
- 一部の設定値はセキュリティのためマスクされている場合があります（••••••••••••として表示）
- Scopeプロパティは、設定のスコープレベルを示します
- 設定は、Orchestratorの基本的な動作を制御するため、慎重に変更する必要があります

主要エンドポイント：GET /odata/Settings
OAuth必須スコープ：OR.Settings または OR.Settings.Read
必須権限：Settings.View

## RELATED LINKS

[Get-OrchActivitySetting](Get-OrchActivitySetting.md)
[Get-OrchUpdateSetting](Get-OrchUpdateSetting.md)
[Get-OrchWebSetting](Get-OrchWebSetting.md)
[about_UiPathOrch](about_UiPathOrch.md)
