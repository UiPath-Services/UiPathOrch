---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmTestCase

## SYNOPSIS
Test Managerからテストケースを取得します。

## SYNTAX

```
Get-TmTestCase [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-TmTestCase コマンドレットは、UiPath Test Managerからテストケース情報を取得します。テストケースは、自動化されたテストワークフローの個々のテストシナリオを表し、検証ステップ、期待結果、テストデータを含みます。

このコマンドレットは、テストケースの詳細、ステータス、実行履歴、関連するテストセットなどの包括的な情報を提供し、テスト管理とレポート活動をサポートします。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /api/testcases

OAuth必須スコープ：[PLACEHOLDER - Test Manager test case scopes]

必須権限：[PLACEHOLDER - Test Manager test case permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse
```

現在のTest Managerインスタンスからすべてのテストケースを取得します。

### Example 2
```powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse *login*
```

ワイルドカードパターンマッチングを使用して、名前に"login"を含むテストケースを取得します。

### Example 3
```powershell
PS C:\> Get-TmTestCase -Path Orch1Tm:, Orch2Tm: -Recurse
```

複数のTest Managerインスタンスからテストケースを取得します。

### Example 4
```powershell
PS Orch1Tm:\> Get-TmTestCase -Recurse | ConvertTo-Json -Depth 2
```

すべてのテストケースを取得し、詳細な分析のために完全な構造をJSON形式で表示します。

## PARAMETERS

### -Name
取得するテストケースの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべてのテストケースが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All test cases
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象のTest Managerドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、複数のTest Managerインスタンスを指定するためのパイプライン入力を受け入れます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
Specifies that the operation should include the target folder and all its subfolders.

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### UiPath.PowerShell.Entities.TmProjectSettings
## NOTES
- このコマンドレットは、Test Managerドライブ（Orch1Tm:など）で動作します
- テストケースは、自動化されたテストワークフローの個々のテストシナリオを表します
- 検証ステップ、期待結果、テストデータを含む包括的な情報を提供します
- テスト管理とレポート活動をサポートします

主要エンドポイント：GET /api/testcases
OAuth必須スコープ：[PLACEHOLDER - Test Manager test case scopes]
必須権限：[PLACEHOLDER - Test Manager test case permissions]

## RELATED LINKS

[Get-TmTestSet](Get-TmTestSet.md)
[Get-TmServerInfo](Get-TmServerInfo.md)
[about_UiPathOrch](about_UiPathOrch.md)
