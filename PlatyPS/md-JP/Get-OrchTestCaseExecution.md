---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestCaseExecution

## SYNOPSIS
UiPath Orchestratorからテストケース実行記録を取得します。

## SYNTAX

```
Get-OrchTestCaseExecution [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestCaseExecution コマンドレットは、UiPath Orchestratorからテストケース実行記録を取得します。テストケース実行は、実行ステータス、タイミング情報、入出力引数、および関連メタデータを含む、自動化テストケースの個別の実行を表します。

これはフォルダエンティティ操作です。Set-Location（cd コマンド）を使用して特定のフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

主要エンドポイント: [PLACEHOLDER - Test case execution API endpoint]

OAuth必須スコープ: [PLACEHOLDER - Test execution read scope]

必要な権限: [PLACEHOLDER - Test case execution view permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestCaseExecution
```

現在のフォルダからすべてのテストケース実行記録を取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchTestCaseExecution -Recurse
```

現在のフォルダとすべてのサブフォルダから再帰的にすべてのテストケース実行記録を取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestCaseExecution -Recurse TestCase*
```

位置パラメータを使用して、すべてのフォルダから"TestCase"で始まる名前のテストケース実行を再帰的に取得します。

### Example 4
```powershell
PS C:\> Get-OrchTestCaseExecution -Path Orch1:\Production
```

任意の場所からの実行を示して、Productionフォルダからテストケース実行記録を取得します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchTestCaseExecution | Select-Object -First 1 | ConvertTo-Json -Depth 2
```

最初のテストケース実行を取得し、詳細分析のためにJSON形式でその構造を表示します。

## PARAMETERS

### -Depth
-Recurseと組み合わせて使用する際の再帰の最大深度を指定します。フォルダ階層での検索の深さレベルを制御します。

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
取得するテストケース実行の名前パターンを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。EntryPointPathプロパティに対してマッチングします。

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
テストケース実行を検索するフォルダパスを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。現在の場所を変更せずに特定のフォルダをターゲットにしたい場合にこのパラメータを使用します。

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
すべてのサブフォルダでテストケース実行を再帰的に検索します。指定されると、コマンドレットはフォルダ階層全体を検索します。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.TestCaseExecution
## NOTES
- このコマンドレットはフォルダエンティティで動作し、フォルダナビゲーションまたはパス指定が必要です
- 最適なパフォーマンスのために、適切な自動補完を有効にするため、-Path、-Recurse、-Depthパラメータをコマンドレット名の直後に配置してください
- 一般的な実行ステータスには"Passed"、"Failed"、"Cancelled"、"Pending"などがあります
- InputArgumentsとOutputArgumentsには、JSON形式のテストデータが含まれます
- テスト実行パターンと結果を分析するには、フィルタリングとソート操作を使用してください
- RuntimeTypeプロパティは、テストを実行したロボットのタイプを示します（例："TestAutomation"）
- ExecutionOrderプロパティは、テストセット内でのテストケース実行の順序を示します

主要エンドポイント: GET /odata/TestCaseExecutions
OAuth必須スコープ: OR.TestSetExecutions または OR.TestSetExecutions.Read
必要な権限: TestSetExecutions.View

## RELATED LINKS

[Get-OrchTestCase]()

[Get-OrchTestSet]()

[Get-OrchTestSetExecution]()

[Start-OrchTestSet]()
