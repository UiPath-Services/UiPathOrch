---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchQueueItem

## SYNOPSIS
包括的なフィルタリング機能を備えたUiPath Orchestratorキューからキューアイテムを取得します。

## SYNTAX

```
Get-OrchQueueItem [[-Name] <String[]>] [-Status <String[]>] [-Revision <String[]>] [-Priority <String[]>]
 [-Exception <String[]>] [-Robot <String[]>] [-Reviewer <String[]>] [-DueDateAfter <DateTime>]
 [-DueDateBefore <DateTime>] [-DeferDateAfter <DateTime>] [-DeferDateBefore <DateTime>]
 [-StartProcessingAfter <DateTime>] [-StartProcessingBefore <DateTime>] [-EndProcessingAfter <DateTime>]
 [-EndProcessingBefore <DateTime>] [-Skip <Int32>] [-First <Int32>] [-OrderBy <String>] [-OrderAscending]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchQueueItem コマンドレットは、包括的なフィルタリング機能を備えたUiPath Orchestratorキューからキューアイテムを取得します。キューアイテムは自動化プロセスの作業単位を表し、処理されるデータと実行ステータス、優先度、処理履歴の追跡を含みます。

各キューアイテムには、ステータス（New、InProgress、Successful、Failedなど）、優先度（High、Normal、Low）、処理タイムスタンプ、ロボット割り当て、特定のコンテンツデータ、例外の詳細、レビュー情報を含む広範囲な情報が含まれます。アイテムには一意の識別子（Id、Key、UniqueKey）と処理メタデータも含まれます。

このコマンドレットはフォルダエンティティ操作として動作し、適切なフォルダコンテキストへの移動または-Pathパラメータを使用したターゲットフォルダの指定が必要です。**重要**: このコマンドレットは過度なデータ取得を防ぐために少なくとも一つのフィルタパラメータが必要です。フィルタなしの場合、警告とともにキャッシュされた内容を出力します。

主要エンドポイント: GET /odata/QueueItems

OAuth必須スコープ: OR.Queues または OR.Queues.Read

必要な権限: Queues.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchQueueItem -Status New -First 5
```

現在のフォルダからNewステータスの最初の5つのキューアイテムを取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchQueueItem ProcessingQueue -Status Failed -First 10
```

ProcessingQueueから失敗した最初の10個のキューアイテムを取得します。

### Example 3
```powershell
PS C:\> Get-OrchQueueItem -Path Orch1:\Production -Recurse -Status New, InProgress -Priority High
```

ProductionフォルダからNewまたはInProgressステータスの高優先度キューアイテムを取得します。

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchQueueItem -StartProcessingAfter (Get-Date).AddHours(-2) | ConvertTo-Json -Depth 3
```

過去2時間以内に処理を開始したキューアイテムを取得し、SpecificContentとProcessingExceptionを含む詳細なJSON情報を表示します。

### Example 5
```powershell
PS C:\> Get-OrchQueueItem -Path Orch1:\Production -Robot Robot01 -Status Successful -OrderBy EndProcessing -OrderAscending
```

ProductionフォルダからRobot01によって処理された成功したキューアイテムを取得し、終了処理時間の昇順（古いものから順）で並べ替えます。

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchQueueItem ReviewQueue -Reviewer john.doe -Skip 20 -First 10
```

ReviewQueueからレビュー担当者john.doeに割り当てられたキューアイテムを取得し、最初の20個をスキップして次の10個を取得します。

## PARAMETERS

### -Name
アイテムを取得するキューの名前を指定します。柔軟なキュー選択のためのワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
検索するターゲットフォルダを指定します。指定されていない場合は、現在のフォルダコンテキストが使用されます。パス指定が必要なフォルダエンティティ操作用です。

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

### -Skip
結果セットの最初からスキップするキューアイテムの数を指定します。ページネーションに便利です。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
結果セットの最初から返すキューアイテムの最大数を指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Depth
ターゲットフォルダへの再帰の深度を指定します。深度0は現在の場所のみを示します。高い値はより多くのサブフォルダレベルを含みます。

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

### -Recurse
検索操作にターゲットフォルダとそのすべてのサブフォルダを含めます。包括的なキューアイテム検出に不可欠です。

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

### -Exception
キューアイテムをフィルタリングする例外タイプを指定します。特定のエラータイプを持つアイテムを見つけるために使用します。

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

### -Priority
キューアイテムをフィルタリングする優先度レベルを指定します。一般的な値にはHigh、Normal、Lowがあります。

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

### -Reviewer
キューアイテムをフィルタリングするレビュー担当者のユーザー名を指定します。特定のレビュー担当者に割り当てられたアイテムを見つけるために使用します。

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

### -Revision
キューアイテムをフィルタリングするリビジョン番号を指定します。

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

### -Robot
キューアイテムをフィルタリングするロボット名を指定します。特定のロボットによって処理されたアイテムを見つけるために使用します。

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

### -Status
キューアイテムをフィルタリングするステータス値を指定します。一般的な値にはNew、InProgress、Successful、Failed、Abandoned、Retried、Deletedがあります。

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

### -OrderAscending
結果を昇順（true）または降順（false）で並べ替えるかを指定します。

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

### -OrderBy
結果を並べ替えるフィールドを指定します（例：StartProcessing、EndProcessing、Priority、Status）。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -DeferDateAfter
キューアイテムをフィルタリングする最も早い延期日を指定します。この時刻以降の延期日を持つアイテムのみが返されます。

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

### -DeferDateBefore
キューアイテムをフィルタリングする最も遅い延期日を指定します。この時刻以前の延期日を持つアイテムのみが返されます。

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

### -DueDateAfter
キューアイテムをフィルタリングする最も早い期限日を指定します。この時刻以降の期限日を持つアイテムのみが返されます。

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

### -DueDateBefore
キューアイテムをフィルタリングする最も遅い期限日を指定します。この時刻以前の期限日を持つアイテムのみが返されます。

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

### -EndProcessingAfter
キューアイテムをフィルタリングする最も早い処理終了時刻を指定します。この時刻以降に処理を終了したアイテムのみが返されます。

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

### -EndProcessingBefore
キューアイテムをフィルタリングする最も遅い処理終了時刻を指定します。この時刻以前に処理を終了したアイテムのみが返されます。

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

### -StartProcessingAfter
キューアイテムをフィルタリングする最も早い処理開始時刻を指定します。この時刻以降に処理を開始したアイテムのみが返されます。

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

### -StartProcessingBefore
キューアイテムをフィルタリングする最も遅い処理開始時刻を指定します。この時刻以前に処理を開始したアイテムのみが返されます。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.QueueItem
## NOTES
このコマンドレットは、過度なデータ取得を防ぐために少なくとも一つのフィルタパラメータが必要なフォルダエンティティ操作です。フィルタパラメータが指定されていない場合、コマンドレットは警告とともにキャッシュされた内容を出力します。一般的なフィルタパターンには、キュー名（Name）、ステータス値（Status）、時間範囲（StartProcessingAfter/Before）、ロボット割り当て（Robot）があります。大きな結果セットを管理するにはページネーションパラメータ（Skip、First）を使用してください。この操作にはターゲットフォルダでのQueues.View権限が必要です。

主要エンドポイント: GET /odata/QueueItems
OAuth必須スコープ: OR.Queues または OR.Queues.Read
必要な権限: Transactions.View

## RELATED LINKS

[Add-OrchQueueItem](Add-OrchQueueItem.md)

[Set-OrchQueueItem](Set-OrchQueueItem.md)

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)

[Get-OrchQueue](Get-OrchQueue.md)
