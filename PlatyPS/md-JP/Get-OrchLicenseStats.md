---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLicenseStats

## SYNOPSIS
UiPath Orchestrator からライセンス使用統計を取得します。

## SYNTAX

`
Get-OrchLicenseStats [[-Last] <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-OrchLicenseStats コマンドレットは、UiPath Orchestrator から履歴ライセンス使用統計を取得します。これにより、時間の経過に伴うライセンス消費パターンに関する貴重な洞察が提供され、管理者が使用傾向を監視し、容量を計画し、ライセンスコンプライアンスを確保するのに役立ちます。

このコマンドレットは、さまざまな期間にわたってロボットタイプ（Unattended、RpaDeveloperPro など）別のライセンス使用数を示す時系列データを返します。このデータは、ライセンスパターンの理解、ピーク使用期間の特定、およびライセンス割り当てに関する情報に基づいた意思決定を行うために不可欠です。

統計には、ライセンス使用の日次スナップショットが含まれており、管理者がさまざまなタイプのライセンスが時間の経過とともにどのように消費されるかを追跡し、自動化活動の傾向を特定できます。

-Path パラメーターには、カンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /api/Stats/GetLicenseStats?tenantId={tenantId}&days={days}

OAuth 必須スコープ: OR.Monitoring または OR.Monitoring.Read

必要な権限: License.View

## EXAMPLES

### Example 1
`powershell
PS C:\> Get-OrchLicenseStats
`

現在の Orchestrator インスタンスのデフォルトライセンス使用統計を取得します。

### Example 2
`powershell
PS Orch1:\> Get-OrchLicenseStats -Last Week
`

過去 7 日間のライセンス使用統計を取得します。

### Example 3
`powershell
PS Orch1:\> Get-OrchLicenseStats -Last Month | Group-Object robotType | Select-Object Name, Count
`

過去 30 日間のライセンス使用統計を取得し、ロボットタイプ別にグループ化して使用分布を確認します。

### Example 4
`powershell
PS Orch1:\>  = Get-OrchLicenseStats -Last Month
PS Orch1:\>  | Where-Object robotType -eq "Unattended" | Measure-Object count -Sum
`

過去 30 日間の統計を取得し、Unattended ライセンスの総使用量を計算します。

### Example 5
`powershell
PS Orch1:\> Get-OrchLicenseStats -Last Day | ConvertTo-Json -Depth 5
`

過去 24 時間のライセンス使用統計を取得し、数値ロボットタイプ値を含む完全なオブジェクト構造を確認するために JSON 形式に変換します。

## PARAMETERS

### -Last
最近の統計の期間を指定します。有効な値: Hour、Day、Week、Month、3Months、6Months、Year、3Years。

`yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

### -Path
ターゲットドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットになります。このパラメーターは、複数の Orchestrator インスタンスを指定するためのパイプライン入力を受け取ります。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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

### UiPath.PowerShell.Entities.LicenseStatsModel

## NOTES
- ライセンス統計は、容量計画とコンプライアンス監視に役立つ履歴使用データを提供します
- robotType プロパティは "Unattended"、"RpaDeveloperPro" などのライセンスタイプを表示しますが、内部的には数値として格納されています
- データは、リアルタイムの消費ではなく、ライセンス使用の日次スナップショットを表します
- データ処理スケジュールに応じて、統計に現在の日が含まれない場合があります
- count プロパティは、指定された日にそのタイプのライセンスが使用されている数を示します
- 数値ロボットタイプ値を含む完全なオブジェクト構造を確認するには、ConvertTo-Json を使用してください
- 有効な -Last パラメーター値は大文字と小文字を区別します: 'Day'、'Week'、'Month'、'3Months'、'6Months'、'Year'、'3Years'
- タイムスタンプは UTC 形式で提供されます

主要エンドポイント: GET /api/Stats/GetLicenseStats
OAuth 必須スコープ: OR.Monitoring または OR.Monitoring.Read
必要な権限: License.View

## RELATED LINKS

[Get-OrchLicense](Get-OrchLicense.md)
[Get-OrchLicenseRuntime](Get-OrchLicenseRuntime.md)
[Get-OrchLicenseNamedUser](Get-OrchLicenseNamedUser.md)
[Get-OrchJobStats](Get-OrchJobStats.md)
[about_UiPathOrch](about_UiPathOrch.md)
