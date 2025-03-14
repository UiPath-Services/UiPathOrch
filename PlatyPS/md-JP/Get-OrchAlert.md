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
アラートはテナントエンティティなので、ドライブ名をターゲットとして指定してください。このコマンドレットは、必ず最新の状態を Orchestrator に問い合わせます。そのため、このコマンドレットの出力は、キャッシュされないことに注意してください。

主に呼び出すエンドポイント: GET /odata/Alerts

OAuth に必要なスコープ: OR.Monitoring or OR.Monitoring.Read

必要な権限: Alerts.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchAlert
```

現在のドライブ Orch1: のアラートをすべて表示します。

### Example 2
```powershell
PS C:\> Get-OrchAlert -Path Orch1:
```

Orch1: ドライブのアラートをすべて表示します。任意のドライブで実行できます。

### Example 3
```powershell
PS Orch1:\> Get-OrchAlert -Last Day
```

過去24時間のアラートを表示します。さらに、複数のパラメーターを同時に指定して、取得するアラートを絞り込むことができます。たとえば、-CreationTimeAfter、-Component、-Severity などのパラメーターが利用できます。

### Example 4
```powershell
PS Orch1:\> Get-OrchAlert -Skip 3 -First 5
```

このコマンドは、最初の3つのアラートをスキップした後の最初の5つのアラートを取得します。多数のアラートがある場合に結果をページングして、特定のサブセットのみを表示できます。

## PARAMETERS

### -Component
取得したいアラートのコンポーネントを指定します。指定できるコンポーネントには、Folders、Robots、Process などがあります。

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
指定された日時以降に作成されたアラートのみを返すように指定します。

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
指定された日時以前に作成されたアラートのみを返すように指定します。

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
アラートの作成日時に基づいてアラートをフィルタリングするための時間枠を指定します。指定できる値には、Hour、Day、Week などがあります。

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
ターゲットとするドライブの名前を指定します。指定しない場合は、現在のドライブをターゲットとします。

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

### -Severity
取得するアラートの重要度レベルを指定します。指定できる値は Info、Success、Warn、Error、Fatal です。

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
指定された数のエンティティを無視して、残りのエンティティを取得します。
スキップするエンティティの数を指定してください。

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
指定された数のエンティティのみを取得します。
取得するエンティティの数を指定してください。

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
Required Scope: OR.Monitoring.Read

## RELATED LINKS
