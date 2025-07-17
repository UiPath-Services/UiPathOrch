---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchQueueItem

## SYNOPSIS
CSVファイルからキューアイテムを指定されたキューにインポートします。

## SYNTAX

```
Import-OrchQueueItem [-Name] <String[]> -ImportCsv <String[]> [[-CsvEncoding] <Encoding>]
 [[-CommitType] <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Import-OrchQueueItemコマンドレットは、CSVファイルから指定されたフォルダ内のUiPath Orchestratorキューにキューアイテムをインポートします。このコマンドレットを使用すると、外部データソースからワークアイテムを一括読み込みでき、データの移行、テスト環境のセットアップ、外部システムからのワークアイテムの読み込みに不可欠です。

キューアイテムは、ロボットが順次処理する作業単位を表します。CSVファイルからキューアイテムをインポートすることで、効率的な一括データ読み込み、他のシステムからの移行、またはオートメーションシナリオ用のテストデータの準備が可能になります。

-Nameパラメータを使用して、インポートされたアイテムを受信するキューを指定します。-ImportCsvパラメータは、キューアイテムデータを含むCSVファイルを指定します。このコマンドレットは、さまざまなインポートシナリオに対応するため、さまざまなCSVエンコーディングとコミット戦略をサポートしています。

このコマンドレットはフォルダ上で動作します。-Pathパラメータを使用して、キューが配置されているターゲットフォルダを指定します。CSVファイルには、適切な列ヘッダーを持つ適切にフォーマットされたキューアイテムデータが含まれている必要があります。

プライマリ エンドポイント: GET /odata/QueueDefinitions, POST /odata/Queues/UiPathODataSvc.BulkAddQueueItems

OAuth 必要なスコープ: OR.Queues または OR.Queues.Write

必要な権限: Transactions.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Import-OrchQueueItem WorkQueue -ImportCsv "C:\Data\workitems.csv"
```

workitems.csvファイルから現在のフォルダ（Development）のWorkQueueにキューアイテムをインポートします。

### Example 2
```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Production -Name ProcessingQueue -ImportCsv "C:\Exports\items.csv" -CsvEncoding UTF8
```

UTF8エンコーディングでitems.csvからProductionフォルダのProcessingQueueにキューアイテムをインポートします。

### Example 3
```powershell
PS Orch1:\Development> Import-OrchQueueItem DataQueue, BulkQueue -ImportCsv "C:\Data\bulk_items.csv" -WhatIf
```

bulk_items.csvから現在のフォルダのDataQueueとBulkQueueにキューアイテムをインポートする場合の結果を表示します。

### Example 4
```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Development -Name ImportQueue -ImportCsv "C:\Migration\*.csv" -CommitType Immediate
```

C:\Migrationディレクトリ内のすべてのCSVファイルから、即座コミット戦略を使用してImportQueueにキューアイテムをインポートします。

### Example 5
```powershell
PS Orch1:\Production> Import-OrchQueueItem TestQueue -ImportCsv "C:\TestData\scenario1.csv", "C:\TestData\scenario2.csv" -Confirm
```

複数のCSVファイルから確認プロンプト付きでTestQueueにキューアイテムをインポートします。

### Example 6
```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Development -Name *ProcessQueue -ImportCsv "C:\Data\items.csv" -CsvEncoding Unicode
```

Unicodeエンコーディングでitems.csvから、名前がProcessQueueで終わるすべてのキューにキューアイテムをインポートします。

## PARAMETERS

### -CommitType
インポート操作のコミット戦略を指定します。有効な値には、Immediate、Batch、またはTransactionが含まれます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CsvEncoding
CSVファイルのエンコーディングを指定します。指定されていない場合は、UTF8エンコーディングが使用されます。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
アイテムをインポートするキューの名前を指定します。

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
キューを含むターゲットフォルダを指定します。指定されていない場合は、現在のフォルダがターゲットになります。

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

### -ImportCsv
インポートするキューアイテムデータを含むCSVファイルを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.FailedQueueItem
## NOTES
CSVファイルには、適切な列ヘッダーを持つ適切にフォーマットされたキューアイテムデータが含まれている必要があります。列は、キューアイテムのプロパティと、ターゲットキューが期待する特定のデータと一致する必要があります。インポート中のデータ破損を避けるため、CSVファイルが適切にエンコードされていることを確認してください。

大きなCSVファイルの場合、インポート操作は時間がかかる場合があります。データ量とトランザクション要件に基づいて適切なコミット戦略を使用してください。大きなデータインポートを実行する前に、-WhatIfでテストインポートを行ってください。

## RELATED LINKS

[Get-OrchQueue](Get-OrchQueue.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Add-OrchQueueItem](Add-OrchQueueItem.md)

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)
