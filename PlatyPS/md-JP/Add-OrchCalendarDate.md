---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchCalendarDate

## SYNOPSIS
非稼働日カレンダーに日付を追加します。

## SYNTAX

```
Add-OrchCalendarDate [-Name] <String[]> [[-ExcludedDate] <DateTime[]>] [-IncludePastDate] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-OrchCalendarDate コマンドレットは、UiPath Orchestrator の非稼働日カレンダーに指定された日付を追加します。このコマンドレットを使用すると、自動化プロセスを実行しない休日、メンテナンス期間、その他のビジネス固有の日付を非稼働日として定義できます。

-Name パラメーターを使用して更新するカレンダーを指定し、-ExcludedDate パラメーターを使用して非稼働日として追加する日付を指定します。このコマンドレットは、複数のカレンダーに複数の日付を同時に追加することをサポートしています。

デフォルトでは、カレンダーの精度を維持するために過去の日付の追加を防ぎます。カレンダーのセットアップやデータ移行シナリオで履歴日付の追加が必要な場合は、-IncludePastDate パラメーターを使用してください。

このコマンドレットは、テナントレベルのカレンダーエンティティで動作し、カレンダー名のワイルドカードパターンをサポートしているため、複数のカレンダーにわたる一括操作が可能です。

主要エンドポイント: GET /odata/Calendars, GET /odata/Calendars({calendarId}), PUT /odata/Calendars({calendarId})

OAuth 必要スコープ: OR.Settings または OR.Settings.Read または OR.Settings.Write

必要なアクセス許可: Settings.Edit

## EXAMPLES

### EXAMPLES 1
```powershell
PS Orch1:\> Add-OrchCalendarDate MyCalendar1 (Get-Date).AddDays(7)
```

今日から7日後の日付を "MyCalendar1" カレンダーに非稼働日として位置パラメーターを使用して追加します。

### EXAMPLES 2
```powershell
PS Orch1:\> Add-OrchCalendarDate MyCalendar1 2025-12-25, 2025-12-26
```

クリスマスとボクシングデーを "MyCalendar1" カレンダーに非稼働日として追加します。

### EXAMPLES 3
```powershell
PS Orch1:\> Add-OrchCalendarDate My*, Your* (Get-Date).AddDays(14)
```

ワイルドカードを使用して "My*" と "Your*" のパターンに一致するすべてのカレンダーに今日から14日後の日付を追加します。

### EXAMPLES 4
```powershell
PS Orch1:\> Add-OrchCalendarDate MyCalendar1 2025-01-01 -IncludePastDate -WhatIf
```

-IncludePastDate パラメーターを使用して過去の日付をカレンダーに追加する際の動作を -WhatIf を使用して安全に確認します。

### EXAMPLES 5
```powershell
PS Orch1:\> $holidays = 2025-12-25, 2025-12-31, 2026-01-01
PS Orch1:\> Add-OrchCalendarDate MyCalendar $holidays
```

変数に格納された複数の休日をカレンダーに追加します。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認を求めます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludedDate
カレンダーに追加する非稼働日を指定します。

```yaml
Type: DateTime[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
非稼働日を追加するカレンダーの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。

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

### -WhatIf
コマンドレットを実行した場合の動作を表示します。
コマンドレットは実行されません。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
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

### -IncludePastDate
昨日以前の日付の追加を許可します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
### System.DateTime[]
## OUTPUTS

### System.Object
## NOTES
このコマンドレットは、テナントレベルのカレンダーエンティティで動作します。デフォルトでは、カレンダーの精度を維持するために過去の日付を追加することはできません。履歴日付の追加が必要な場合は、-IncludePastDate パラメーターを使用してください。

Get-OrchCalendar を使用して利用可能なカレンダーを一覧表示し、日付の追加が成功したことを確認してください。

## RELATED LINKS

[Get-OrchCalendar](Get-OrchCalendar.md)

[Remove-OrchCalendarDate](Remove-OrchCalendarDate.md)

[Copy-OrchCalendar](Copy-OrchCalendar.md)
