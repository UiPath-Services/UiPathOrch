---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchQueueItem

## SYNOPSIS
Newステータスのキューアイテムを、他の場所にある同じ名前のキューにコピーします。

## SYNTAX

```
Copy-OrchQueueItem [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchQueueItem コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースキューから宛先の同じ名前のキューに"New"ステータスのキューアイテムをコピーします。このコマンドレットは、データと優先度を保持しながら、未処理の作業項目を環境間で移動させます。

このコマンドレットは"New"ステータスのキューアイテムのみをコピーし、すでに処理済みまたは現在処理中の作業が重複しないようにします。宛先キューは、ソースキューと同じ名前でなければならず、ターゲットフォルダに存在する必要があります。

このコマンドレットは、正常にコピーされたアイテムを出力します。この出力は Remove-OrchQueueItem にパイプして、ソースキューからそれらのアイテムを削除し、完全な移動操作を作成して重複したトランザクション処理を防ぐことができます。このパターンは、キューアイテムの移行ワークフローに不可欠です。

-Name パラメーターを使用してコピーするキューのアイテムを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のキューからアイテムを効率的にコピーするためのワイルドカードパターンをサポートしています。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからキューアイテムをコピーし、宛先でフォルダ構造を維持できます。

主要エンドポイント: GET /odata/QueueDefinitions, GET /odata/QueueItems, POST /odata/Queues/UiPathODataSvc.AddQueueItem

OAuth 必要なスコープ: OR.Queues

必要な権限: Queues.View, Transactions.View, Transactions.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst
```

現在のフォルダ（\Shared）のMyQueueからすべての"New"キューアイテムを、同じテナント内の\dstフォルダのMyQueueにコピーします。

### Example 2
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem | Export-Csv c:\itemsToRemove.csv -Encoding utf8BOM
```

\SharedのMyQueueからすべての"New"キューアイテムを\dstのMyQueueに移動します。コピーされたアイテムはソースキューから削除され、削除に失敗したアイテムは後で再試行するためにCSVにエクスポートされます。

### Example 3
```powershell
PS Orch1:\> Copy-OrchQueueItem -Recurse * Orch2:\ | Remove-OrchQueueItem | Export-Csv c:\itemsToRemove.csv -Encoding utf8BOM
```

あるテナントのすべてのキューから、同じフォルダ構造を持つ別のテナントの対応するキューに、すべての"New"キューアイテムを移動します。削除に失敗したものはCSVにエクスポートされます。

### Example 4
```powershell
PS C:\> Copy-OrchQueueItem -Path Orch1:\Development ProcessingQueue Orch2:\Production
```

Orch1:\DevelopmentのProcessingQueueからOrch2:\ProductionのProcessingQueueに、すべての"New"キューアイテムをコピーし、テナント間キューアイテムコピーを示しています。

### Example 5
```powershell
PS Orch1:\Development> Copy-OrchQueueItem *Invoice*, *Report* Orch1:\Production -WhatIf
```

現在のフォルダからProductionフォルダに、InvoiceまたはReportが含まれる名前のキューから"New"キューアイテムをコピーする場合に何が起こるかを示します。

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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Destination
キューアイテムをコピーする宛先フォルダを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
アイテムをコピーするキューの名前を指定します。

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
ソースフォルダを指定します。指定しない場合、現在のフォルダがソースとして使用されます。

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

### -Recurse
すべてのサブフォルダからキューアイテムを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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

### -WhatIf
コマンドレットを実行した場合の動作を示します。
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
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

このコマンドレットは"New"ステータスのキューアイテムのみをコピーします。宛先キューは存在し、ソースキューと同じ名前でなければなりません。必要に応じて、Copy-OrchQueue を使用してキュー定義を最初にコピーしてください。

**重要**: このコマンドレットは、正常にコピーされたアイテムを出力し、Remove-OrchQueueItem にパイプして移動操作を完了できます。これにより、重複したトランザクション処理を防ぎます。Export-Csv を使用して、失敗した削除アイテムを後で再試行するために保存してください。エクスポートされたCSVは、Import-Csv を使用して再インポートし、Remove-OrchQueueItem にパイプして失敗したアイテムの削除を再試行できます。

**完全なキュー移行ワークフロー**:
1. キュー定義をコピー: `Copy-OrchQueue * \destination`
2. キューアイテムを移動: `Copy-OrchQueueItem * \destination | Remove-OrchQueueItem | Export-Csv failedRemovals.csv`

効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)

[Copy-OrchQueue](Copy-OrchQueue.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Export-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/export-csv)
