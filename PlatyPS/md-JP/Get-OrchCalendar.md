---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchCalendar

## SYNOPSIS
UiPath Orchestrator で構成されたビジネスカレンダーを取得します。

## SYNTAX

```
Get-OrchCalendar [[-Name] <String[]>] [-ExpandExcludedDate] [-IncludePastDate] [-Path <String[]>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchCalendar コマンドレットは、UiPath Orchestrator 内で構成されたビジネスカレンダーを取得します。ビジネスカレンダーは、自動化スケジューリング用の稼働日、祝日、除外日を定義し、自動化プロセスがいつ実行されるべきかを正確に制御できます。

カレンダーには、Name、TimeZoneId、ExcludedDates 配列、一意のキー（GUID 識別子）などのプロパティが含まれます。これらのカレンダーは、ビジネスルールと祝日スケジュールに基づいて実行タイミングを決定するために、トリガーとスケジュールによって使用されます。

ビジネスカレンダーは、テナント全体のスコープで動作するテナントエンティティです。ドライブ名（例：Orch1:、Orch2:）でターゲットテナントを指定するには、-Path パラメーターを使用します。

このコマンドレットはテナントレベルエンティティ操作として動作し、指定された Orchestrator 環境からカレンダーを取得します。-ExpandExcludedDate パラメーターは除外日に関する詳細情報を提供し、-IncludePastDate は過去の日付情報を含めるかどうかを制御します。

主要エンドポイント: GET /odata/Calendars

OAuth 必須スコープ: OR.Jobs または OR.Jobs.Read

必要なアクセス許可: Schedules.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchCalendar
```

現在のテナントからすべてのビジネスカレンダーを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchCalendar -Name "*Holiday*" -ExpandExcludedDate
```

名前に "Holiday" を含むカレンダーを取得し、除外日の詳細情報を表示します。

### Example 3
```powershell
PS C:\> Get-OrchCalendar -Path Orch1:, Orch2: -IncludePastDate
```

複数のテナントから過去の日付情報を含むカレンダーを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchCalendar | Select-Object Path, Name, TimeZoneId, Key
```

すべてのカレンダーを取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchCalendar -ExportCsv "calendars-report.csv" -CsvEncoding UTF8
```

すべてのカレンダー情報を UTF8 エンコーディングで CSV ファイルにエクスポートします。

### Example 6
```powershell
PS Orch1:\> Get-OrchCalendar -ExpandExcludedDate | Where-Object {$_.ExcludedDates.Count -gt 0}
```

除外日が設定されているカレンダーのみを取得し、詳細な除外日情報を表示します。

## PARAMETERS

### -ExpandExcludedDate
カレンダーの除外日情報を展開して詳細な情報を取得します。このパラメーターを指定すると、各除外日の詳細プロパティが表示されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
取得するビジネスカレンダーの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのカレンダーが返されます。

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
ターゲットテナントドライブを指定します。指定されていない場合、現在のテナントがターゲットになります。テナントレベル操作用。

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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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

### -CsvEncoding
CSV エクスポート時の文字エンコーディングを指定します。-ExportCsv パラメーターと組み合わせて使用します。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
カレンダー情報をエクスポートする CSV ファイルのパスを指定します。このパラメーターが指定された場合、通常のオブジェクト出力の代わりに CSV ファイルが作成されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludePastDate
過去の日付情報をカレンダー情報に含めるかどうかを指定します。デフォルトでは、現在および将来の日付のみが含まれます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

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

### UiPath.PowerShell.Entities.ExtendedCalendar
### UiPath.PowerShell.Entities.ExcludedDateNamed
## NOTES

このコマンドレットはテナントレベルエンティティ操作として動作し、組織全体のビジネスカレンダーにアクセスします。カレンダーは自動化スケジューリングの重要な構成要素であり、トリガーとスケジュールで使用されてビジネスルールに基づいた実行タイミングを決定します。

-ExpandExcludedDate パラメーターは除外日の詳細情報を提供しますが、大量の除外日があるカレンダーではパフォーマンスに影響を与える可能性があります。必要に応じて -IncludePastDate を使用して過去の日付情報を含めることができます。

カレンダーの TimeZoneId プロパティは、日付計算と除外日判定において重要な役割を果たします。

主要エンドポイント: GET /odata/Calendars
OAuth 必須スコープ: OR.Jobs または OR.Jobs.Read
必要なアクセス許可: Schedules.View

## RELATED LINKS

[Copy-OrchCalendar](Copy-OrchCalendar.md)

[Remove-OrchCalendar](Remove-OrchCalendar.md)

[Add-OrchCalendarDate](Add-OrchCalendarDate.md)

[Remove-OrchCalendarDate](Remove-OrchCalendarDate.md)
