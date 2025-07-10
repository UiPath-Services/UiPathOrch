---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmTestSet

## SYNOPSIS
Test Managerのプロジェクトからテストセットを取得します。

## SYNTAX

`
Get-TmTestSet [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-TmTestSetコマンドレットは、Test Managerプロジェクトからテストセットを取得します。テストセットは、グループとして一緒に実行できるテストケースのコレクションで、組織化されたテスト実行とレポート機能を提供します。

このコマンドレットはフォルダーエンティティに対して動作し、特定のTest Managerプロジェクトへのナビゲーションまたは-Path、-Recurse、-Depthパラメーターの使用が必要です。設定にTest Managerスコープが含まれている場合に自動的に利用可能になるUiPathOrchTmプロバイダーと連携します。

テストセットは、Orchestratorプロセス、外部テスト管理システム、または手動テスト作成など、さまざまなソースに由来できます。このコマンドレットは、テストセット構成、実行順序の強制、カバレッジ設定、および関連するテストケースに関する情報を提供します。

プライマリ エンドポイント: GET /testmanager_/api/v2/{projectId}/testsets

OAuth 必要なスコープ: TM.TestSets or TM.TestSets.Read

必要な権限: TestSet.Read

## EXAMPLES

### Example 1
`powershell
PS Orch1Tm:\> Get-TmTestSet
`

現在のTest Managerプロジェクトからすべてのテストセットを取得します。

### Example 2
`powershell
PS Orch1Tm:\> Get-TmTestSet RegressionTests
`

現在のプロジェクトから"RegressionTests"という名前のテストセットを取得します。

### Example 3
`powershell
PS Orch1Tm:\> Get-TmTestSet Smoke*
`

ワイルドカードマッチングを使用して、名前が"Smoke"で始まるすべてのテストセットを取得します。

### Example 4
`powershell
PS Orch1Tm:\> Get-TmTestSet -Recurse
`

すべてのTest Managerプロジェクトからテストセットを再帰的に取得します。

### Example 5
`powershell
PS Orch1Tm:\> Get-TmTestSet -Path Orch1Tm:\MyProject
`

現在の場所を変更せずに、指定されたTest Managerプロジェクトからすべてのテストセットを取得します。

### Example 6
`powershell
PS Orch1Tm:\> Get-TmTestSet -Recurse | Where-Object {$_.source -eq "Orchestrator"}
`

すべてのプロジェクトからOrchestratorプロセスに由来するすべてのテストセットを取得します。

### Example 7
`powershell
PS Orch1Tm:\> Get-TmTestSet -Recurse | ConvertTo-Json -Depth 3
`

すべてのテストセットを取得し、詳細な分析のためにJSON形式で完全な構造を表示します。

### Example 8
`powershell
PS Orch1Tm:\> Get-TmTestSet | Where-Object {$_.enforceExecutionOrder -eq $true} | Select-Object name, numberOfTestCases
`

実行順序を強制するテストセットを見つけ、その名前とテストケース数を表示します。

## PARAMETERS

### -Name
取得するテストセットの名前を指定します。パターンマッチング用のワイルドカード文字（*と?）をサポートします。複数の名前が指定された場合、一致するすべてのテストセットが返されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
テストセットを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（*と?）をサポートします。現在の場所を変更せずに特定のTest Managerプロジェクトをターゲットにしたい場合に、このパラメーターを使用します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Recurse
サブフォルダーからテストセットを再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から始まってTest Managerプロジェクト階層全体を走査します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に対するPowerShellの応答方法を指定します。

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
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメーターをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TmTestSet
## NOTES

このコマンドレットはフォルダーエンティティに対して動作します。つまり、特定のTest Managerプロジェクトにナビゲートするか、-Pathパラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

Test Manager操作では、適切なTest Managerドライブ（Orch1Tm:、Orch2Tm:など）に接続しているか、-Pathパラメーターを使用して正しいドライブを指定してください。

大規模なTest Manager環境で-Recurseパラメーターを使用すると、操作の完了に相当な時間がかかる場合があります。必要に応じて-Pathを使用してスコープを制限することを検討してください。

テストセットには以下のような重要な実行構成が含まれます：
- enforceExecutionOrder: テストケースを特定の順序で実行する必要があるかどうか
- enableCoverage: コードカバレッジ分析が有効かどうか
- source: テストセットの起源（Orchestrator、外部システムなど）

テストセットオブジェクトには、デフォルトの表示形式では表示されない詳細なメタデータが含まれているため、ConvertTo-Jsonを使用してテストセットオブジェクトの完全な構造を探索してください。

フォルダーエンティティコマンドレットでは、パラメーターの配置が重要です。適切な自動補完機能のために、-Path、-Recurseパラメーターをコマンドレット名の直後に配置してください。

## RELATED LINKS

[Get-TmTestCase](Get-TmTestCase.md)
[Start-OrchTestSet](Start-OrchTestSet.md)
[Get-OrchTestSetExecution](Get-OrchTestSetExecution.md)
[Stop-OrchTestSetExecution](Stop-OrchTestSetExecution.md)
[Remove-TmTestSet](Remove-TmTestSet.md)
