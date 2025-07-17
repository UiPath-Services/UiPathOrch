---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Redo-OrchQueueItem

## SYNOPSIS
指定されたキューの失敗したトランザクションアイテムを再試行します。

## SYNTAX

```
Redo-OrchQueueItem [-Name] <String[]> [-Id] <Int64[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
このコマンドレットは、キュー名とアイテムIDを指定してトランザクションアイテムを再試行します。再試行可能なアイテムのみを再試行します。再試行可能なアイテムとは、ステータスがFailedで、リビジョンがNoneまたはInReviewのものとして定義されます。

プライマリ エンドポイント: POST /odata/QueueItems/UiPathODataSvc.SetItemReviewStatus

OAuth 必要なスコープ: OR.Queues

必要な権限: Queues.View および Transactions.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Redo-OrchQueueItem YourQueueName <item IDs>
```

キュー内の指定されたアイテムを再試行します。<item IDs>を指定する-Idパラメータは、オートコンプリートをサポートしています。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose
```

指定されたキューYourQueueName内のすべての失敗したアイテムを再試行します。
-Verboseパラメータは、再試行されたアイテムのIDを表示します。

Get-OrchQueueItemコマンドレットは一度に最大1000個のアイテムを取得できることに注意してください。
キューに1,000個を超える再試行可能なアイテムがある場合は、-Verboseパラメータが出力を生成しなくなるまでこのコマンドを繰り返して、これらのアイテムをすべて再試行してください。

### Example 3
```powershell
PS Orch1:\> Get-OrchQueueItem -Recurse * -Status Failed | Redo-OrchQueueItem
```

テナント内のすべてのキューにわたって、すべての再試行可能なキューアイテムを再試行します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。

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

### -Id
再試行するキューアイテムのIDを指定します。

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
再試行するキューアイテムを含むキューの名前を指定します。

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
ターゲットフォルダを指定します。指定されていない場合は、現在のフォルダがターゲットになります。

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

### -WhatIf
コマンドレットを実行した場合の結果を表示します。
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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
### System.Int64[]
## OUTPUTS

### UiPath.PowerShell.Entities.BulkOperationResponse
## NOTES

## RELATED LINKS
