---
external help file: UiPathOrch-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestDataQueueItemTable

## SYNOPSIS
UiPath Orchestratorからテストデータキューアイテムを拡張テーブル形式で取得します。

## SYNTAX

```
Get-OrchTestDataQueueItemTable [[-Path] <String>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestDataQueueItemTable コマンドレットは、UiPath Orchestratorからテストデータキューアイテムを取得し、拡張テーブル形式で表示します。このコマンドレットは、テストデータの内部JSON形式を読みやすいテーブル構造に変換する関数として実装されています。

Get-OrchTestDataQueueItem がテーブルとして表示できない内部JSON形式でテストデータを返すのに対し、このコマンドレットは、変数形式のテストデータを構造化されたテーブル表示に展開することで、この制限に特化して対処します。これにより、テストデータの内容を表示、分析、報告するのに最適です。

これは、テストデータキューを含む特定のフォルダに移動するか、-Pathまたは-Recurseパラメーターを使用して対象フォルダを指定する必要があるフォルダエンティティ操作コマンドレットです。このコマンドレットは、テストデータのスキーマに動的に適応し、すべての列と値をユーザーフレンドリーなテーブル形式で表示します。

このコマンドレットは、JSON内容を適切なテーブル列に展開することで、任意のテストデータスキーマを処理および表示できるため、テストデータの列構造が変化するデータ駆動型テストシナリオに特に有用です。

-Pathおよび-Recurseパラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターの自動補完が正しく機能します。

主要エンドポイント：関数実装（内部的にGet-OrchTestDataQueueItemを使用し、GET /odata/TestDataQueueItemsを呼び出す）

OAuth必須スコープ：OR.TestData.Read

必須権限：Folders.View、TestData.View（テストデータキューアイテムアクセス用）

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable
```

現在のフォルダからテストデータキューアイテムを取得し、拡張テーブル形式で表示します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable TestDataQueue
```

位置パラメーターを使用して、"TestDataQueue"という名前の指定されたキューからテストデータアイテムをテーブル形式で取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable *Test*
```

ワイルドカードマッチングを使用して、名前に"Test"を含むキューからテストデータアイテムを取得し、テーブル形式で表示します。

### Example 4
```powershell
PS Orch1:\> Get-OrchTestDataQueueItemTable -Recurse
```

現在のフォルダとすべてのサブフォルダから再帰的にテストデータキューアイテムを取得し、拡張テーブル形式で表示します。

### Example 5
```powershell
PS C:\> Get-OrchTestDataQueueItemTable -Path Orch1:\Production
```

任意の場所からの実行を示し、Productionフォルダからテストデータアイテムをテーブル形式で取得します。

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable | ConvertTo-Json -Depth 2
```

テストデータをテーブル形式で取得し、詳細な分析のためにテーブルメタデータを含む完全な構造をJSON形式で表示します。

## PARAMETERS

### -Path
テストデータキューを含むフォルダパスを指定します。指定しない場合、現在の場所が使用されます。このパラメーターは、パイプライン入力を受け入れ、複数のパスを指定するためのワイルドカードをサポートします。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: Current location
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Recurse
アイテムを取得する際に、サブフォルダのテストデータキューアイテムも含めます。これは、テストデータの複数のフォルダをスキャンできるフォルダエンティティ操作パラメーターです。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新（Write-Progressコマンドレットによって生成される進行状況バーなど）にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### System.String
## OUTPUTS

### System.Object
## NOTES
- これは、テストデータフォルダへのナビゲーションまたは-Path/-Recurseパラメーターの使用が必要なフォルダエンティティ操作コマンドレットです
- このコマンドレットは、内部JSONテストデータをテーブル形式に変換する関数として実装されています
- JSON形式を返すGet-OrchTestDataQueueItemとは異なり、このコマンドレットはユーザーフレンドリーなテーブル出力を提供します
- テーブル形式は、テストデータスキーマに動的に適応し、利用可能なすべての列を表示します
- このコマンドレットは、JSON内容を適切なテーブル列に展開することで、変数形式のテストデータを処理します
- テーブル形式のメタデータを含む完全な構造を探索するには、ConvertTo-Jsonを使用してください
- IsConsumedプロパティは、テストデータアイテムがテスト実行で使用されたかどうかを示します
- このコマンドレットは、読みやすいテストデータ表示が必要なデータ駆動型テストシナリオに不可欠です

主要エンドポイント：GET /odata/TestDataQueues
OAuth必須スコープ：OR.TestDataQueues または OR.TestDataQueues.Read
必須権限：TestDataQueues.View

## RELATED LINKS

[Get-OrchTestDataQueueItem](Get-OrchTestDataQueueItem.md)
[Get-OrchTestDataQueue](Get-OrchTestDataQueue.md)
[about_UiPathOrch](about_UiPathOrch.md)
