---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLicenseStats

## SYNOPSIS
UiPath Orchestratorからライセンス使用統計を取得します。

## SYNTAX

```
Get-OrchLicenseStats [[-Last] <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchLicenseStats コマンドレットは、UiPath Orchestratorから履歴ライセンス使用統計を取得します。これは、時間の経過に伴うライセンス消費パターンについての貴重な洞察を提供し、管理者が使用傾向を監視し、容量を計画し、ライセンス準拠を確保するのに役立ちます。

このコマンドレットは、異なる時間期間にわたってロボットタイプ（例：Unattended、RpaDeveloperPro）別のライセンス使用数を示す時系列データを返します。このデータは、ライセンスパターンを理解し、ピーク使用期間を特定し、ライセンス割り当てについて情報に基づいた決定を行うために不可欠です。

統計には、ライセンス使用の日次スナップショットが含まれており、管理者は時間の経過とともに異なるタイプのライセンスがどのように消費されているかを追跡し、自動化活動の傾向を特定できます。

-Pathパラメーターの複数の値は、カンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /api/Stats/GetLicenseStats?tenantId={tenantId}&days={days}

OAuth必須スコープ：OR.Monitoring または OR.Monitoring.Read

必須権限：License.View

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-OrchLicenseStats
```

現在のOrchestratorインスタンスのデフォルトライセンス使用統計を取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchLicenseStats -Last Week
```

過去7日間のライセンス使用統計を取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchLicenseStats -Last Month | Group-Object robotType | Select-Object Name, Count
```

過去30日間のライセンス使用統計を取得し、ロボットタイプ別にグループ化して使用分布を表示します。

### Example 4
```powershell
PS Orch1:\> $stats = Get-OrchLicenseStats -Last Month
PS Orch1:\> $stats | Where-Object robotType -eq "Unattended" | Measure-Object count -Sum
```

過去30日間の統計を取得し、Unattendedライセンスの総使用量を計算します。

### Example 5
```powershell
PS Orch1:\> Get-OrchLicenseStats -Last Day | ConvertTo-Json -Depth 5
```

過去24時間のライセンス使用統計を取得し、数値ロボットタイプ値を含む完全なオブジェクト構造を表示するためにJSON形式に変換します。

## PARAMETERS

### -Last
最近の統計の期間を指定します。有効な値：Hour、Day、Week、Month、3Months、6Months、Year、3Years。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
対象ドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、複数のOrchestratorインスタンスを指定するためのパイプライン入力を受け入れます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.LicenseStatsModel
## NOTES
- ライセンス統計は、容量計画とコンプライアンス監視を支援するための履歴使用データを提供します
- robotTypeプロパティは、"Unattended"、"RpaDeveloperPro"などのライセンスタイプを表示しますが、内部的には数値として保存されます
- データは、リアルタイムの消費ではなく、ライセンス使用の日次スナップショットを表します
- データ処理スケジュールによって、統計には現在の日が含まれない場合があります
- countプロパティは、指定された日付でそのタイプのライセンスが使用されている数を示します
- 数値ロボットタイプ値を含む完全なオブジェクト構造を表示するには、ConvertTo-Jsonを使用してください
- 有効な-Lastパラメーター値は大文字小文字を区別します：'Day'、'Week'、'Month'、'3Months'、'6Months'、'Year'、'3Years'
- タイムスタンプはUTC形式で提供されます

主要エンドポイント：GET /api/Stats/GetLicenseStats
OAuth必須スコープ：OR.Monitoring または OR.Monitoring.Read
必須権限：License.View

## RELATED LINKS

[Get-OrchLicense](Get-OrchLicense.md)
[Get-OrchLicenseRuntime](Get-OrchLicenseRuntime.md)
[Get-OrchLicenseNamedUser](Get-OrchLicenseNamedUser.md)
[Get-OrchJobStats](Get-OrchJobStats.md)
[about_UiPathOrch](about_UiPathOrch.md)
