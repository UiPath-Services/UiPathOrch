---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchCalendarDate

## SYNOPSIS
非稼働日カレンダーから日付を削除します。

## SYNTAX

```
Remove-OrchCalendarDate [-Name] <String[]> [-ExcludedDate] <DateTime[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

プライマリ エンドポイント: GET /odata/Calendars, GET /odata/Calendars({calendarId}), PUT /odata/Calendars({calendarId})

OAuth 必要なスコープ: OR.Settings または OR.Settings.Read または OR.Settings.Write

必要な権限: Settings.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -ExcludedDate
カレンダーから削除する非稼働日を指定します。

```yaml
Type: DateTime[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
非稼働日が削除されるカレンダーの名前を指定します。

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

### System.String[]
### System.DateTime[]
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
