---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestDataQueue

## SYNOPSIS
UiPath Orchestratorからテストデータキューを取得します。

## SYNTAX

```
Get-OrchTestDataQueue [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestDataQueue コマンドレットは、UiPath Orchestratorからテストデータキューを取得します。テストデータキューは、自動化テスト用の構造化されたテストデータ管理を提供し、自動化検証と品質保証プロセスで使用されるテストデータセットの組織的な保存と取得を可能にします。

テストデータキューには、テスト自動化ワークフローで消費できる構造化データセットが含まれており、データ駆動テストシナリオをサポートします。これらのキューは、テストデータをテストロジックから分離することで、保守可能でスケーラブルなテスト自動化プラクティスを促進します。

このコマンドレットはフォルダエンティティ操作として動作し、適切なフォルダコンテキストへの移動または-Pathパラメータを使用したターゲットフォルダの指定が必要です。サブフォルダからテストデータキューを含めるには-Recurseパラメータを使用し、再帰レベルを制御するには-Depthを使用します。

主要エンドポイント: GET /odata/TestDataQueues

OAuth必須スコープ: [PLACEHOLDER]

必要な権限: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue
```

現在のフォルダからすべてのテストデータキューを取得します。

### Example 2
```powershell
PS C:\> Get-OrchTestDataQueue -Path Orch1:\Production *UserData*
```

Productionフォルダから"UserData"を含む名前のテストデータキューを取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue -Recurse
```

現在のフォルダとすべてのサブフォルダから再帰的にすべてのテストデータキューを取得します。

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue | ConvertTo-Json -Depth 2
```

詳細なテストデータキューのプロパティをJSON形式で表示します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueue TestUserQueue, LoginTestData
```

名前によって特定のテストデータキューを取得します。

## PARAMETERS

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

### -Name
取得するテストデータキューの名前を指定します。柔軟なキュー選択のためのワイルドカードパターンをサポートします。

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

### -Recurse
検索操作にターゲットフォルダとそのすべてのサブフォルダを含めます。包括的なテストデータキュー検出に不可欠です。

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestDataQueue
## NOTES
このコマンドレットは、テストデータキュー構成にアクセスするためのフォルダエンティティ操作です。テストデータキューは、自動化検証用の構造化データセットを提供することで、データ駆動テストをサポートします。キューは、テストデータをテストロジックから分離して組織化し、保守可能なテスト自動化プラクティスを可能にします。個別のテストデータエントリにアクセスするにはGet-OrchTestDataQueueItemと組み合わせて使用してください。この操作にはターゲットフォルダでのTestDataQueues.View権限が必要です。

主要エンドポイント: GET /odata/TestDataQueueDefinitions
OAuth必須スコープ: OR.TestDataQueues または OR.TestDataQueues.Read
必要な権限: TestDataQueues.View

## RELATED LINKS

[Get-OrchTestDataQueueItem](Get-OrchTestDataQueueItem.md)

[Add-OrchTestDataQueue](Add-OrchTestDataQueue.md)

[Set-OrchTestDataQueue](Set-OrchTestDataQueue.md)

[Remove-OrchTestDataQueue](Remove-OrchTestDataQueue.md)
