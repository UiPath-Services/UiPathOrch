---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmTestSet

## SYNOPSIS
Test Managerからテストセットを取得します。

## SYNTAX

```
Get-TmTestSet [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-TmTestSet コマンドレットは、UiPath Test Managerからテストセット情報を取得します。テストセットは、関連するテストケースの論理的なグループであり、特定のテストシナリオや機能領域の包括的なテストカバレッジを提供します。

このコマンドレットは、テストセットの詳細、含まれるテストケース、実行ステータス、スケジューリング情報などの包括的な情報を提供し、テスト管理とレポート活動をサポートします。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /api/testsets

OAuth必須スコープ：[PLACEHOLDER - Test Manager test set scopes]

必須権限：[PLACEHOLDER - Test Manager test set permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1Tm:\> Get-TmTestSet -Recurse
```

現在のTest Managerインスタンスからすべてのテストセットを取得します。

### Example 2
```powershell
PS Orch1Tm:\> Get-TmTestSet -Path MyProject *regression*
```

ワイルドカードパターンマッチングを使用して、名前に"regression"を含むテストセットを取得します。

### Example 3
```powershell
PS C:\> Get-TmTestSet -Path Orch1Tm:, Orch2Tm: -Recurse
```

複数のTest Managerインスタンスからテストセットを取得します。

### Example 4
```powershell
PS Orch1Tm:\> Get-TmTestSet -Recurse | ConvertTo-Json -Depth 2
```

すべてのテストセットを取得し、詳細な分析のために完全な構造をJSON形式で表示します。

## PARAMETERS

### -Name
取得するテストセットの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべてのテストセットが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All test sets
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

### UiPath.PowerShell.Entities.TmTestSet
## NOTES
- このコマンドレットは、Test Managerドライブ（Orch1Tm:など）で動作します
- テストセットは、関連するテストケースの論理的なグループです
- 特定のテストシナリオや機能領域の包括的なテストカバレッジを提供します
- テスト管理とレポート活動をサポートします

主要エンドポイント：GET /api/testsets
OAuth必須スコープ：[PLACEHOLDER - Test Manager test set scopes]
必須権限：[PLACEHOLDER - Test Manager test set permissions]

## RELATED LINKS

[Get-TmTestCase](Get-TmTestCase.md)
[Get-TmServerInfo](Get-TmServerInfo.md)
[about_UiPathOrch](about_UiPathOrch.md)
