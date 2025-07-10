---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAlert

## SYNOPSIS
アラートを取得します。

## SYNTAX

```
Get-OrchAlert [[-Last] <String>] [[-Severity] <String>] [[-Component] <String[]>]
 [-CreationTimeAfter <DateTime>] [-CreationTimeBefore <DateTime>] [-Skip <UInt64>] [-First <UInt64>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
アラートはテナントエンティティです。したがって、このコマンドレットでは、ドライブ名をターゲットとして指定します。このコマンドレットは、最新の状態について常に Orchestrator にクエリを実行します。そのため、このコマンドレットの出力はキャッシュされないことに注意してください。

プライマリエンドポイント: GET /odata/Alerts

OAuth 必須スコープ: OR.Monitoring または OR.Monitoring.Read

必要なアクセス許可: Alerts.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchAlert
```

現在の場所である Orch1: ドライブのすべてのアラートを表示します。

### Example 2
```powershell
PS C:\> Get-OrchAlert -Path Orch1:
```

Orch1: ドライブのすべてのアラートを表示します。このコマンドはどのドライブからでも実行できます。

### Example 3
```powershell
PS Orch1:\> Get-OrchAlert -Last Day
```

過去 24 時間のアラートを表示します。さらに、複数のパラメーターを同時に指定して、取得するアラートを絞り込むことができます。たとえば、`-CreationTimeAfter`、`-Component`、`-Severity` などのパラメーターが利用できます。

### Example 4
```powershell
PS Orch1:\> Get-OrchAlert -Skip 3 -First 5
```

このコマンドは、最初の 3 つのアラートをスキップした後、最初の 5 つのアラートを取得します。多数のアラートがあり、特定のサブセットのみを表示する必要がある場合に、結果をページング処理するのに便利です。

### Example 5
```powershell
PS Orch1:\> Get-OrchAlert -Severity Fatal | Select-Object Path, CreationTime, Severity, Component
```

Fatal 重要度のアラートを取得し、Path を最初に表示して主要なプロパティを表示します。

## PARAMETERS

### -Component
取得するアラートのコンポーネントを指定します。指定できるコンポーネントには、Folders、Robots、Process などがあります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -CreationTimeAfter
指定された日時値より後に作成されたアラートのみが返されることを指定します。

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CreationTimeBefore
指定された日時値より前に作成されたアラートのみが返されることを指定します。

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Last
最近のアラートの期間を指定します。有効な値: Hour、Day、Week、Month、3Months、6Months、Year、3Years。

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
ターゲットドライブの名前を指定します。指定されていない場合、現在のドライブがターゲットになります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
{{ ProgressAction の説明を入力 }}

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

### -Severity
取得するアラートの重要度レベルを指定します。使用可能な値は、Info、Success、Warn、Error、Fatal です。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。
スキップするオブジェクトの数を入力してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
指定された数のオブジェクトのみを取得します。
取得するオブジェクトの数を入力してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Alert
## NOTES
必要なスコープ: OR.Monitoring.Read

プライマリエンドポイント: GET /odata/Alerts
OAuth 必須スコープ: OR.Monitoring または OR.Monitoring.Read
必要なアクセス許可: Alerts.View

## RELATED LINKS
