---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestCase

## SYNOPSIS
Orchestratorからテストケース定義を取得します。

## SYNTAX

```
Get-OrchTestCase [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestCase コマンドレットは、Orchestrator環境からテストケース定義を取得します。テストケースは、自動化パッケージ内でテストケースとして指定されたワークフローファイル（.xaml）であり、自動テストシナリオのテストセットの一部として実行できます。

このコマンドレットはフォルダエンティティで動作し、異なるフォルダ階層間でテストケースを見つけるための再帰的トラバーサルをサポートします。テストケースは特定のパッケージとバージョンに関連付けられ、バージョン管理されたテストと品質保証プロセスを可能にします。

テストケース定義には、関連付けられたパッケージ、バージョン情報、作成と変更の詳細、フィード関連付けに関するメタデータが含まれます。これらのテストケースはテストセットに編成され、自動化ワークフローを検証するために実行できます。

主要エンドポイント: GET /odata/TestCaseDefinitions

OAuth必須スコープ: OR.TestSets または OR.TestSets.Read

必要な権限: TestSets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestCase
```

現在のフォルダからすべてのテストケース定義を取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestCase *Test*.xaml
```

ワイルドカードマッチングを使用して、名前に"Test"を含むすべてのテストケース定義を取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchTestCase -Recurse
```

現在のフォルダとすべてのサブフォルダから再帰的にすべてのテストケース定義を取得します。

### Example 4
```powershell
PS C:\> Get-OrchTestCase -Path Orch1:\Production -Recurse | ConvertTo-Json -Depth 2
```

Productionフォルダからテストケース定義を取得し、JSON形式で詳細な構造を表示します。

## PARAMETERS

### -Depth
再帰操作の最大深度レベルを指定します。-Recurseが指定されたときに再帰検索がどこまで深く進むべきかを制限するためにこのパラメータを使用します。

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
取得するテストケース定義の名前を指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。複数の名前が指定された場合、一致するすべてのテストケース定義が返されます。

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
テストケース定義を検索するフォルダパスを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。現在の場所を変更せずに特定のフォルダをターゲットにしたい場合にこのパラメータを使用します。

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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### -Recurse
サブフォルダからテストケース定義を再帰的に取得します。指定されると、コマンドレットは指定されたパスまたは現在の場所から始まるフォルダ階層全体をトラバーサルします。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestCaseDefinition
## NOTES

このコマンドレットはフォルダエンティティで動作します。つまり、特定のフォルダに移動するか、-Pathパラメータを使用して目的の場所をターゲットにする必要がある場合があります。

大きなフォルダ階層で-Recurseパラメータを使用する場合、操作が完了するまでにかなりの時間がかかる場合があります。必要に応じて-Depthを使用してトラバーサルスコープを制限することを検討してください。

テストケース定義は自動化パッケージに関連付けられ、重要なバージョン情報を含みます：
- PackageIdentifier: テストケースを含む自動化パッケージ
- AppVersion: テストケースのアプリケーションバージョン
- CreatedVersion: テストケースが作成されたときのパッケージバージョン
- LatestVersion: 関連付けられたパッケージの最新バージョン
- FeedId: パッケージフィード関連付け

フォルダエンティティコマンドレットでは、パラメータの配置が重要です。適切な自動補完機能のために、-Path、-Recurse、-Depthパラメータをコマンドレット名の直後に配置してください。

テストケース定義オブジェクトの完全な構造を探索するにはConvertTo-Jsonを使用してください。バージョン追跡やパッケージ関連付けを含む詳細なメタデータが含まれています。

テストケース定義は自動テストワークフローの基盤として機能し、包括的な品質保証プロセスのためにテストセットに編成できます。

主要エンドポイント: GET /odata/TestCaseDefinitions
OAuth必須スコープ: OR.TestSets または OR.TestSets.Read
必要な権限: TestSets.View

## RELATED LINKS

[Get-OrchTestSet](Get-OrchTestSet.md)
[Get-OrchTestSetExecution](Get-OrchTestSetExecution.md)
[Remove-OrchTestCase](Remove-OrchTestCase.md)
[Start-OrchTestSet](Start-OrchTestSet.md)
[Get-TmTestCase](Get-TmTestCase.md)
