---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchSetting

## SYNOPSIS
UiPath Orchestrator から構成設定を取得します。

## SYNTAX

`
Get-OrchSetting [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-OrchSetting コマンドレットは、UiPath Orchestrator から構成設定を取得します。これには、ローカライゼーション、メール構成、認証設定、デプロイメント設定、機能切り替え、およびその他の運用パラメーターなど、さまざまなシステム設定が含まれます。

設定は、メールサーバー構成、タイムゾーン設定、認証ポリシー、トリガー設定、および機能の可用性など、Orchestrator の動作のさまざまな側面を制御します。

-Name と -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /odata/Settings

OAuth 必須スコープ: OR.Settings または OR.Settings.Read

必要な権限: Settings.View

## EXAMPLES

### Example 1
`powershell
PS Orch1:\> Get-OrchSetting
`

現在の Orchestrator インスタンスからすべての構成設定を取得します。

### Example 2
`powershell
PS Orch1:\> Get-OrchSetting *Language*,*TimeZone*
`

ワイルドカードパターンを使用して、言語とタイムゾーンに関連する設定を取得します。

### Example 3
`powershell
PS Orch1:\> Get-OrchSetting -Path Orch1:, Orch2: *Auth*
`

複数のテナントから認証関連の設定を取得します。

### Example 4
`powershell
PS Orch1:\> Get-OrchSetting *Email* | Select-Object Path, Name, Value
`

メール関連の設定を取得し、Path を最初に表示してキープロパティを表示します。

### Example 5
`powershell
PS Orch1:\> Get-OrchSetting *Trigger* | ConvertTo-Json
`

トリガー関連の設定を取得し、詳細な構造を JSON 形式で出力します。

## PARAMETERS

### -Name
取得する設定の名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space] または [Tab] を押すことで自動補完を使用できます。指定しない場合、すべての設定が返されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All settings
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
設定を検索するフォルダーパスを指定します。このパラメーターはパイプライン入力を受け取り、複数のパスを指定するためのワイルドカードをサポートします。指定しない場合は、現在の場所が使用されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新 (Write-Progress コマンドレットによって生成される進行状況バーなど) に対して PowerShell が応答する方法を指定します。有効な値は次のとおりです: SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None

## OUTPUTS

### UiPath.PowerShell.Entities.Settings

## NOTES
- 設定には、さまざまな Orchestrator 機能のシステム全体の構成が含まれます
- 一般的な設定カテゴリには次があります: ローカライゼーション（Abp.Localization.*）、メール（Abp.Net.Mail.*）、認証（Auth.*）、デプロイメント（Deployment.*）、機能（Features.*）、およびトリガー（Triggers.*）
- 一部の設定値は、セキュリティのためにマスクされている場合があります（•••••••••••• として表示）
- Scope プロパティは、設定のスコープレベルを示します
- 設定は基本的な Orchestrator の動作を制御するため、慎重に変更する必要があります

主要エンドポイント: GET /odata/Settings
OAuth 必須スコープ: OR.Settings または OR.Settings.Read
必要な権限: Settings.View

## RELATED LINKS

[Get-OrchActivitySetting](Get-OrchActivitySetting.md)
[Get-OrchUpdateSetting](Get-OrchUpdateSetting.md)
[Get-OrchWebSetting](Get-OrchWebSetting.md)
[about_UiPathOrch](about_UiPathOrch.md)
