---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestDataQueueItem

## SYNOPSIS
テストデータキュー内のアイテムを取得します。

## SYNTAX

```
Get-OrchTestDataQueueItem [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestDataQueueItem コマンドレットは、テストデータキューからアイテムを取得します。テストデータキューアイテムには、テスト自動化シナリオで使用される構造化データが含まれます。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、最初にSet-Location（cd）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

パラメータが指定されていない場合、現在のフォルダ内のすべてのテストデータキューアイテムが返されます。

プライマリエンドポイント: GET /odata/TestDataQueueItems?$filter=(TestDataQueueId eq {testDataQueueId})

OAuth必須スコープ: OR.TestDataQueues または OR.TestDataQueues.Read

必要な権限: TestDataQueueItems.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem
```

現在のフォルダ内のすべてのテストデータキューアイテムを取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem CustomerData
```

現在のフォルダ内の「CustomerData」という名前のテストデータキューからすべてのアイテムを取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem *Data*
```

名前に「Data」を含むテストデータキューからすべてのアイテムを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchTestDataQueueItem -Recurse
```

現在のフォルダとそのすべてのサブフォルダからすべてのテストデータキューアイテムを取得します。

### Example 5
```powershell
PS C:\> Get-OrchTestDataQueueItem -Path Orch1:\Development, Orch1:\Production UserTestData
```

DevelopmentフォルダとProductionフォルダの両方から「UserTestData」キューのすべてのアイテムを取得します。

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchTestDataQueueItem | ConvertTo-Json -Depth 2
```

データ構造と内容の詳細分析のために、テストデータキューアイテムをJSON形式で表示します。

## PARAMETERS

### -Depth
ターゲットフォルダへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダは含まれません。

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
取得するアイテムを含むテストデータキューの名前を指定します。

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

### -Recurse
操作にターゲットフォルダとそのすべてのサブフォルダを含めることを指定します。

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
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestDataQueue
## NOTES

プライマリエンドポイント: GET /odata/TestDataQueues
OAuth必須スコープ: OR.TestDataQueues または OR.TestDataQueues.Read
必要な権限: TestDataQueues.View

## RELATED LINKS
