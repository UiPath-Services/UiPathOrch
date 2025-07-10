---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchCalendar

## SYNOPSIS
テナント間でカレンダーをコピーします。

## SYNTAX

```
Copy-OrchCalendar [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchCalendar コマンドレットは、UiPath Orchestrator 内のソーステナントから宛先テナントにカレンダーをコピーします。このコマンドレットは、設定、除外日、メタデータを含むカレンダーの完全なコピーを作成します。

このコマンドレットは、複数の宛先テナントに同時にカレンダーをコピーすることをサポートしています。カレンダーには、トリガーのスケジューリングやプロセス自動化のタイミングに使用されるビジネススケジュール情報が含まれています。

-Name パラメーターを使用してコピーするカレンダーを指定し、-Destination パラメーターを使用してターゲットテナントを指定します。-Path パラメーターを使用すると、複数の Orchestrator インスタンスで作業する際にソーステナントを指定できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:、Orch2:）を指定し、-Destination はカレンダーをコピーするターゲットテナントドライブを指定します。

プライマリエンドポイント: GET /odata/Calendars, GET /odata/Calendars({calendarId}), POST /odata/Calendars

OAuth 必要スコープ: OR.Settings

必要な権限: Settings.View, Settings.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchCalendar BusinessHours Orch2:
```

現在のテナント（Orch1）から Orch2 テナントに BusinessHours カレンダーをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: HolidaySchedule Orch2:, Orch3:
```

Orch1 から Orch2 と Orch3 の両方のテナントに HolidaySchedule カレンダーをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-OrchCalendar BusinessHours, HolidaySchedule Orch2: -WhatIf
```

現在のテナントから Orch2 に BusinessHours と HolidaySchedule カレンダーをコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: *Schedule* Orch2:
```

ワイルドカードを使用して、Orch1 から Orch2 に名前に Schedule を含むすべてのカレンダーをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-OrchCalendar *Business* | Copy-OrchCalendar -Destination Orch2:, Orch3:
```

名前に Business を含むすべてのカレンダーを取得し、Orch2 と Orch3 の両方のテナントにコピーします。

### Example 6
```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: WorkingDays Orch2: -Confirm
```

確認プロンプトを表示して、Orch1 から Orch2 に WorkingDays カレンダーをコピーします。

## PARAMETERS

### -Destination
カレンダーをコピーする宛先テナントドライブを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
コピーするカレンダーの名前を指定します。

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
ソーステナントドライブを指定します。指定しない場合、現在のテナントがソースとして使用されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
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

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedCalendar

## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先テナントのドライブ名（例：Orch1:、Orch2:）を指定します。

効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。カレンダーには、ビジネススケジュールと除外日情報が含まれています。

## RELATED LINKS

[Get-OrchCalendar](Get-OrchCalendar.md)

[Set-OrchCalendar](Set-OrchCalendar.md)

[Remove-OrchCalendar](Remove-OrchCalendar.md)
