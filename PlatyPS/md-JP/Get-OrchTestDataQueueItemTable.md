---
external help file: UiPathOrch-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestDataQueueItemTable

## SYNOPSIS
UiPath Orchestrator から、拡張テーブル形式でテストデータキューアイテムを取得します。

## SYNTAX

`
Get-OrchTestDataQueueItemTable [[-Path] <String>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-OrchTestDataQueueItemTable コマンドレットは、UiPath Orchestrator からテストデータキューアイテムを取得し、拡張テーブル形式で表示します。このコマンドレットは、テストデータの内部JSON形式を読み取り可能なテーブル構造に変換する関数として実装されています。

Get-OrchTestDataQueueItem がテーブルとして表示できない内部JSON形式でテストデータを返すのに対し、このコマンドレットは可変形式のテストデータを構造化されたテーブル形式に展開することで、その制限に対処します。これにより、テストデータの内容を表示、分析、レポート作成に理想的です。

このコマンドレットは、テストデータキューを含む特定のフォルダーに移動するか、-Path または -Recurse パラメーターを使用してターゲットフォルダーを指定する必要があるフォルダーエンティティ操作コマンドレットです。コマンドレットは、テストデータのスキーマに動的に適応し、すべての列と値をユーザーフレンドリーなテーブル形式で表示します。

このコマンドレットは、JSON コンテンツを適切なテーブル列に展開することで、任意のテストデータスキーマを処理・表示できるため、テストデータの列構造が変動するデータ駆動型テストのシナリオで特に有用です。

-Path と -Recurse パラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターの自動補完が正しく機能します。

主要エンドポイント: 関数実装 (内部的に Get-OrchTestDataQueueItem を使用し、GET /odata/TestDataQueueItems を呼び出す)

OAuth 必須スコープ: OR.TestData.Read

必要な権限: Folders.View, TestData.View (テストデータキューアイテムアクセス用)

## EXAMPLES

### Example 1
`powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable
`

現在のフォルダーからテストデータキューアイテムを取得し、拡張テーブル形式で表示します。

### Example 2
`powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable TestDataQueue
`

位置パラメーターを使用して、"TestDataQueue" という名前の指定されたキューからテストデータアイテムをテーブル形式で取得します。

### Example 3
`powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable *Test*
`

ワイルドカード一致を使用して、名前に "Test" を含むキューからテストデータアイテムを取得し、テーブル形式で表示します。

### Example 4
`powershell
PS Orch1:\> Get-OrchTestDataQueueItemTable -Recurse
`

現在のフォルダーとすべてのサブフォルダーから再帰的にテストデータキューアイテムを取得し、拡張テーブル形式で表示します。

### Example 5
`powershell
PS C:\> Get-OrchTestDataQueueItemTable -Path Orch1:\Production
`

任意の場所からの実行を示して、Production フォルダーからテストデータアイテムをテーブル形式で取得します。

### Example 6
`powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItemTable | ConvertTo-Json -Depth 2
`

テストデータをテーブル形式で取得し、詳細な分析のためにテーブルメタデータを含む完全な構造を JSON 形式で表示します。

## PARAMETERS

### -Path
テストデータキューを含むフォルダーパスを指定します。指定しない場合は、現在の場所が使用されます。このパラメーターはパイプライン入力を受け取り、複数のパスを指定するためのワイルドカードをサポートします。

`yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: Current location
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
`

### -Recurse
アイテムを取得する際に、サブフォルダーのテストデータキューアイテムを含めます。これは、複数のフォルダーでテストデータをスキャンできるフォルダーエンティティ操作パラメーターです。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新 (Write-Progress コマンドレットによって生成される進行状況バーなど) に対して PowerShell が応答する方法を指定します。有効な値は次のとおりです: SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### System.String

## OUTPUTS

### System.Object

## NOTES
- これは、テストデータフォルダーへのナビゲーションまたは -Path/-Recurse パラメーターの使用を必要とするフォルダーエンティティ操作コマンドレットです
- このコマンドレットは、内部JSON テストデータをテーブル形式に変換する関数として実装されています
- JSON 形式を返す Get-OrchTestDataQueueItem とは異なり、このコマンドレットはユーザーフレンドリーなテーブル出力を提供します
- テーブル形式は、テストデータスキーマに動的に適応し、利用可能なすべての列を表示します
- このコマンドレットは、JSON コンテンツを適切なテーブル列に展開することで、可変形式のテストデータを処理します
- テーブルフォーマットのメタデータを含む完全な構造を調べるには、ConvertTo-Json を使用してください
- IsConsumed プロパティは、テストデータアイテムがテスト実行で使用されたかどうかを示します
- このコマンドレットは、読み取り可能なテストデータの表示が必要なデータ駆動型テストシナリオで不可欠です

主要エンドポイント: GET /odata/TestDataQueues
OAuth 必須スコープ: OR.TestDataQueues または OR.TestDataQueues.Read
必要な権限: TestDataQueues.View

## RELATED LINKS

[Get-OrchTestDataQueueItem](Get-OrchTestDataQueueItem.md)
[Get-OrchTestDataQueue](Get-OrchTestDataQueue.md)
[about_UiPathOrch](about_UiPathOrch.md)
