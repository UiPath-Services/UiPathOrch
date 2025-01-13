---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: uiPathOrch
online version:
schema: 2.0.0
---

# Redo-OrchQueueItem

## SYNOPSIS
指定したキューに含まれる、失敗したトランザクションアイテムをリトライします。

## SYNTAX

```
Redo-OrchQueueItem [-Name] <String[]> [-Id] <Int64[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
このコマンドレットは、キュー名とアイテム ID を指定してトランザクションアイテムをリトライします。リトライの対象となるのは、Status が Failed であり、かつ Revision が None または InReview のアイテムのみです。

主に呼び出すエンドポイント: POST /odata/QueueItems/UiPathODataSvc.SetItemReviewStatus

OAuth に必要なスコープ: OR.Queues

必要な権限: Queues.View and Transactions.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Redo-OrchQueueItem YourQueueName <item IDs>
```

指定されたキュー YourQueueName 内の指定されたアイテムをリトライします。<item IDs> を指定する -Id パラメータは自動補完できます。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose
```

指定されたキュー YourQueueName 内のすべての失敗したアイテムをリトライします。
-Verbose パラメータを指定すると、リトライしたアイテムの ID を表示します。

Get-OrchQueueItem cmdlet が一度に取得できるアイテムの数は、最大で 1000 個であることに注意してください。
キュー内にリトライ可能なアイテムが 1,000 個以上ある場合、これらのアイテムをすべてリトライするには、このコマンドを -Verbose パラメータが何も出力しなくなるまで繰り返し実行してください。

### Example 3
```powershell
PS Orch1:\> Get-OrchQueueItem -Recurse * -Status Failed -Revision None,InReview | Redo-OrchQueueItem
```

テナント内のすべてのキューにおいて、リトライ可能なキューアイテムをすべてリトライします。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に、あなたの確認を求めます。

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
リトライするキューアイテムの Id を指定します。

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
リトライするキューアイテムを含むキューの Name を指定します。

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
ターゲットとするフォルダーを指定します。指定しない場合は、現在のフォルダーをターゲットとします。

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
コマンドレットを実行すると、何が起こるかを表示します。
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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.Int64[]
## OUTPUTS

### UiPath.PowerShell.Entities.BulkOperationResponse
## NOTES

## RELATED LINKS
