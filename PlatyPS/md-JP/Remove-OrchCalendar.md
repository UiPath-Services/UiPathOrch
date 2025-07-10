---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchCalendar

## SYNOPSIS
Orchestratorからカレンダーを削除します。

## SYNTAX

```
Remove-OrchCalendar [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchCalendarコマンドレットは、Orchestrator環境からカレンダーを永続的に削除します。カレンダーは、オートメーションプロセスのビジネススケジュールとタイムゾーンを定義し、営業日、祝日、特定の時間帯を含むことができます。

カレンダーが削除されると、関連するすべてのスケジュール定義、営業日、祝日、およびタイムゾーン設定が永続的に削除されます。この操作は元に戻すことができないため、特にトリガーやスケジュールされたプロセスで積極的に使用されているカレンダーを削除する前に、慎重に検討する必要があります。

プライマリ エンドポイント: DELETE /odata/Calendars({calendarId})

OAuth 必要なスコープ: OR.Settings

必要な権限: Settings.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-OrchCalendar TestCalendar
```

現在のOrchestrator環境から"TestCalendar"という名前のカレンダーを削除します。

## PARAMETERS

### -Name
削除するカレンダーの名前を指定します。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedCalendar
## NOTES

## RELATED LINKS

[Get-OrchCalendar](Get-OrchCalendar.md)
[Copy-OrchCalendar](Copy-OrchCalendar.md)
[Add-OrchCalendarDate](Add-OrchCalendarDate.md)
[Remove-OrchCalendarDate](Remove-OrchCalendarDate.md)
[Get-OrchTrigger](Get-OrchTrigger.md)
