---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmTestCase

## SYNOPSIS
Test Managerのプロジェクトからテストケースを取得します。

## SYNTAX

`
Get-TmTestCase [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-TmTestCaseコマンドレットは、Test Managerプロジェクトからテストケースを取得します。テストケースは、手動または自動化のいずれかの個別のテストシナリオで、特定のテスト手順、期待される結果、および自動化の関連付けを定義します。

このコマンドレットはフォルダーエンティティに対して動作し、特定のTest Managerプロジェクトへのナビゲーションまたは-Path、-Recurseパラメーターの使用が必要です。設定にTest Managerスコープが含まれている場合に自動的に利用可能になるUiPathOrchTmプロバイダーと連携します。

テストケースは自動化プロジェクトと関連付けることができ、テスト実行パラメーター、前提条件、およびバージョン履歴に関する情報を含みます。これらは、テストセット内でのテスト実行の基盤として機能し、詳細なテストドキュメントとトレーサビリティを提供します。

プライマリ エンドポイント: GET /testmanager_/api/v2/{projectId}/testcases

OAuth 必要なスコープ: TM.TestCases or TM.TestCases.Read

必要な権限: TestCase.Read

## EXAMPLES

### Example 1
`powershell
PS Orch1Tm:\> Get-TmTestCase
`

現在のTest Managerプロジェクトからすべてのテストケースを取得します。

### Example 2
`powershell
PS Orch1Tm:\> Get-TmTestCase LoginTest
`

現在のプロジェクトから"LoginTest"という名前のテストケースを取得します。

### Example 3
`powershell
PS Orch1Tm:\> Get-TmTestCase *API*
`

ワイルドカードマッチングを使用して、名前に"API"を含むすべてのテストケースを取得します。

### Example 4
`powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse
`

すべてのTest Managerプロジェクトからテストケースを再帰的に取得します。

### Example 5
`powershell
PS Orch1Tm:\> Get-TmTestCase -Path Orch1Tm:\MyProject
`

現在の場所を変更せずに、指定されたTest Managerプロジェクトからすべてのテストケースを取得します。

### Example 6
`powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse | Where-Object {$_.automationTestCaseName -ne $null}
`

すべてのプロジェクトから自動化されたテストケース（関連する自動化を持つもの）を取得します。

### Example 7
`powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse | ConvertTo-Json -Depth 3
`

すべてのテストケースを取得し、詳細な分析のためにJSON形式で完全な構造を表示します。

### Example 8
`powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse | Select-Object name, version, automationProjectName, updated | Format-Table
`

名前、バージョン、関連する自動化プロジェクト、最終更新時刻を示すキー情報を含むフォーマットされたテーブルでテストケースを表示します。

### Example 9
`powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse | Group-Object automationProjectName | Select-Object Name, Count
`

関連する自動化プロジェクトごとにテストケースをグループ化し、各プロジェクトの数を表示します。

## PARAMETERS

### -Name
取得するテストケースの名前を指定します。パターンマッチング用のワイルドカード文字（*と?）をサポートします。複数の名前が指定された場合、一致するすべてのテストケースが返されます。

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
テストケースを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（*と?）をサポートします。現在の場所を変更せずに特定のTest Managerプロジェクトをターゲットにしたい場合に、このパラメーターを使用します。

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
サブフォルダーからテストケースを再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から始まってTest Managerプロジェクト階層全体を走査します。

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

### UiPath.PowerShell.Entities.TmProjectSettings
## NOTES

このコマンドレットはフォルダーエンティティに対して動作します。つまり、特定のTest Managerプロジェクトにナビゲートするか、-Pathパラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

Test Manager操作では、適切なTest Managerドライブ（Orch1Tm:、Orch2Tm:など）に接続しているか、-Pathパラメーターを使用して正しいドライブを指定してください。

大規模なTest Manager環境で-Recurseパラメーターを使用すると、操作の完了に相当な時間がかかる場合があります。必要に応じて-Pathを使用してスコープを制限することを検討してください。

テストケースには以下の重要な情報が含まれます：
- automationTestCaseName: 関連する自動化ワークフローファイル
- automationProjectName: テストを含む自動化プロジェクト
- version: 変更追跡のためのテストケースバージョン
- preCondition: テスト実行の前提条件
- inputParams: テスト実行に必要なパラメーター

テストケースオブジェクトには、デフォルトの表示形式では表示されない詳細なメタデータが含まれているため、ConvertTo-Jsonを使用してテストケースオブジェクトの完全な構造を探索してください。

フォルダーエンティティコマンドレットでは、パラメーターの配置が重要です。適切な自動補完機能のために、-Path、-Recurseパラメーターをコマンドレット名の直後に配置してください。

テストケースは、手動（自動化の関連付けなし）または自動化（UiPathワークフローにリンク）のいずれかです。automationTestCaseNameの存在に基づいてこれらのタイプを区別するためにフィルタリングを使用してください。

## RELATED LINKS

[Get-TmTestSet](Get-TmTestSet.md)
[Remove-TmTestCase](Remove-TmTestCase.md)
[Get-OrchTestCaseExecution](Get-OrchTestCaseExecution.md)
[Start-OrchTestSet](Start-OrchTestSet.md)
[Get-TmRequirement](Get-TmRequirement.md)
