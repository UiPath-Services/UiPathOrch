---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Stop-OrchTestSetExecution

## SYNOPSIS
UiPath Orchestrator で実行中または保留中のテストセット実行を停止します。

## SYNTAX

```
Stop-OrchTestSetExecution [-Id] <Int64[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stop-OrchTestSetExecution コマンドレットは、UiPath Orchestrator で実行中または保留中のテストセット実行を停止します。このコマンドレットを使用すると、現在実行中または実行待ちのテストセット実行をキャンセルできます。これは、テスト自動化リソースを管理し、不要になったテストを停止するのに役立ちます。

これはフォルダーエンティティ操作です。Set-Location（cdコマンド）を使用して特定のフォルダーに移動するか、-Path パラメーターを使用して対象フォルダーを指定する必要があります。-Path パラメーターは対象フォルダーパスを指定します。指定されていない場合は、現在のフォルダーが対象になります。

プライマリ エンドポイント: [PLACEHOLDER - Test set execution stop API endpoint]

OAuth 必要なスコープ: [PLACEHOLDER - Test execution management scope]

必要な権限: [PLACEHOLDER - Test set execution stop permissions]

## EXAMPLES

### Example 1: 複数のテストセット実行を停止
```powershell
PS Orch1:\Shared> Stop-OrchTestSetExecution 274957, 274958, 274959 -WhatIf
```

実際にテストセット実行を停止せずに、停止操作が実行された場合に何が起こるかを表示します。

### Example 2: 確認プロンプトで停止
```powershell
PS C:\> Stop-OrchTestSetExecution -Path Orch1:\Shared 274957 -Confirm
```

実行前に追加の確認プロンプトでテストセット実行を停止します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。これは、進行中のテスト活動に影響を与える可能性があるため、テスト実行を停止する場合に特に重要です。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
停止するテストセット実行の ID を指定します。複数の ID を指定して、複数のテスト実行を同時に停止できます。

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
テストセット実行が配置されているフォルダーパスを指定します。パターンマッチングにワイルドカード文字（* と ?）をサポートします。特定のフォルダー内のテスト実行を対象にしたい場合は、このパラメーターを使用します。

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

### -WhatIf
コマンドレットが実行された場合の動作を表示します。コマンドレットは実行されません。このパラメーターを使用して、実際に操作を実行する前に、どのテストセット実行が停止されるかをプレビューします。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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

### System.Object
## NOTES

このコマンドレットはフォルダーエンティティ上で動作するため、特定のフォルダーに移動するか、-Path パラメーターを使用して目的の場所のテスト実行を対象にする必要がある場合があります。

テストセット実行を停止すると、それらのテストセット内で実行中のすべてのテストケースが即座に終了されます。この操作は元に戻すことができないため、実行を停止する際は注意してください。

テスト実行を停止する前に、Get-OrchTestSetExecution を使用して、現在実行中の実行とその状態を特定することを検討してください。

特に複数のテスト実行にマッチする可能性があるパターンを使用する場合は、-WhatIf パラメーターを使用して操作をプレビューしてください。

複数のテスト実行を停止する場合、操作は順次実行されます。大量の実行の処理には時間がかかる場合があります。

Test Manager 操作の場合は、適切な Test Manager ドライブ（Orch1Tm:、Orch2Tm: など）に接続しているか、-Path パラメーターを使用して正しいドライブを指定してください。

## RELATED LINKS

[Get-OrchTestSetExecution](Get-OrchTestSetExecution.md)
[Start-OrchTestSet](Start-OrchTestSet.md)
[Get-OrchTestSet](Get-OrchTestSet.md)
[Get-OrchTestCase](Get-OrchTestCase.md)
