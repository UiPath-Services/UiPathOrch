---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchQueueItem

## SYNOPSIS
キューアイテムを削除します。

## SYNTAX

```
Remove-OrchQueueItem [-Name] <String> [-Id] <Int64[]> [[-RowVersion] <String>] [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchQueueItemコマンドレットは、UiPath Orchestrator内のキューから特定のキューアイテムをIDで削除します。このコマンドレットは、独立して動作するように設計されており、キューアイテム移行ワークフローの一部としても機能します。

キュー名と削除したいアイテムのIDを指定します。RowVersionも提供する場合、削除プロセスが高速化されます。RowVersionが指定されていない場合、指定されたIDのキャッシュから取得されます。キャッシュにRowVersionが含まれていない場合、コマンドレットは自動的にGetItemById APIを呼び出して取得します。

このコマンドレットは、削除に失敗したアイテムを出力します。これらの失敗したアイテムは、Export-Csvを使用してCSVファイルにエクスポートし、後でImport-Csvで再インポートして削除を再試行できます。これにより、大規模なキューアイテム管理操作に対する回復力のあるエラー処理が提供されます。

Copy-OrchQueueItemの出力をRemove-OrchQueueItemにパイプすることで、正常にコピーされたアイテムを簡単に削除できます。これにより、重複したトランザクション処理を防ぐ完全な移動操作が作成されます。

プライマリ エンドポイント: GET /odata/QueueItems({queueItemId}), POST /odata/QueueItems/UiPathODataSvc.DeleteBulk

OAuth 必要なスコープ: OR.Queues

必要な権限: Queues.View および Transactions.Delete

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Remove-OrchQueueItem MyQueue 12345, 12346, 12347
```

現在のフォルダのMyQueueからID 12345、12346、12347のキューアイテムを削除します。

### Example 2
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem
```

MyQueueから宛先にキューアイテムをコピーし、正常にコピーされたアイテムをソースキューから削除して、移動操作を完了します。

### Example 3
```powershell
PS Orch1:\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem | Export-Csv c:\failedRemovals.csv -Encoding utf8BOM
```

キューアイテムを移動し、削除に失敗したアイテムを後で再試行するためにCSVファイルにエクスポートします。

### Example 4
```powershell
PS C:\> Import-Csv c:\failedRemovals.csv | Remove-OrchQueueItem
```

以前に削除に失敗したアイテムをCSVから再インポートし、削除を再試行します。

### Example 5
```powershell
PS Orch1:\Production> Remove-OrchQueueItem ProcessingQueue 98765 "ABC123-RowVersion" -WhatIf
```

ProcessingQueueからID 98765とRowVersion "ABC123-RowVersion"の特定のキューアイテムを削除する際に何が起こるかを表示します。

### Example 6
```powershell
PS C:\> Remove-OrchQueueItem -Path Orch1:\Development TestQueue 11111, 22222 -Confirm
```

確認プロンプトでDevelopmentフォルダのTestQueueからID 11111と22222のキューアイテムを削除します。

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
削除するキューアイテムのIDを指定します。

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
削除するアイテムを含むキューの名前を指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
キューを含むソースフォルダを指定します。指定しない場合は、現在のフォルダが使用されます。

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

### -RowVersion
高速削除のためのキューアイテムのRowVersionを指定します。提供されない場合は、自動的に取得されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### System.String
### System.Int64[]
## OUTPUTS

### System.Object
## NOTES
このコマンドレットは、キュー名とアイテムIDを指定して独立して使用することも、移動操作のためにCopy-OrchQueueItemとのパイプラインの一部として使用することもできます。

**パフォーマンス**: RowVersionを提供すると、バージョン情報を取得するための追加のAPI呼び出しを回避することで削除が高速化されます。

**エラー処理**: 失敗した削除は出力として返され、後で再試行するためにCSVにエクスポートできます。Import-Csvを使用して失敗したアイテムを再読み込みし、Remove-OrchQueueItemにパイプで戻します。

**移動操作パターン**:
1. `Copy-OrchQueueItem SourceQueue Destination | Remove-OrchQueueItem | Export-Csv failedRemovals.csv`
2. `Import-Csv failedRemovals.csv | Remove-OrchQueueItem` (失敗の再試行)

特に一括操作の場合は、実行前に削除をプレビューするために-WhatIfを使用してください。

## RELATED LINKS

[Copy-OrchQueueItem](Copy-OrchQueueItem.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Add-OrchQueueItem](Add-OrchQueueItem.md)

[Export-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/export-csv)

[Import-Csv](https://docs.microsoft.com/powershell/module/microsoft.powershell.utility/import-csv)
