---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTestSet

## SYNOPSIS
テストセットを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchTestSet [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchTestSet コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースフォルダから宛先フォルダにテストセットをコピーします。このコマンドレットは、テストケース構成、実行パラメーター、メタデータを含むテストセットの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。テストセットには、自動テストワークフローのテストケースのコレクションが含まれており、このコマンドレットは、異なる環境間でテスト自動化をデプロイするために不可欠です。

-Name パラメーターを使用してコピーするテストセットを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のテストセットを効率的にコピーするためのワイルドカードパターンをサポートしています。コピーされたテストセットは、関連するテストケースが宛先フォルダで異なる名前を持つ場合、調整が必要になる可能性があることに注意してください。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからテストセットをコピーし、宛先でフォルダ構造を維持できます。

主要エンドポイント: GET /odata/TestSets, POST /odata/TestSets

OAuth 必要なスコープ: OR.TestSets

必要な権限: TestSets.View, TestSets.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTestSet RegressionTests Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内のProductionフォルダにRegressionTestsテストセットをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchTestSet -Path Orch1:\Development SmokeTests Orch2:\Production
```

Orch1:\DevelopmentからOrch2:\ProductionにSmokeTestsテストセットをコピーし、テナント間テストセットコピーを示しています。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTestSet *API*, *UI* Orch1:\Production -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダからProductionフォルダにAPIまたはUIが含まれる名前の複数のテストセットをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchTestSet -Path Orch1:\Development *Integration* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\DevelopmentからOrch2:\ProductionにIntegrationが含まれるすべてのテストセットをテナント間コピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchTestSet -Recurse *Automated* Orch2:\Finance -WhatIf
```

すべてのサブフォルダからAutomatedが含まれるテストセットを再帰的にOrch2:\Financeにコピーする場合に何が起こるかを示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSet *Regression* | Copy-OrchTestSet -Destination Orch2:\Production
```

Regressionが含まれる名前のすべてのテストセットを取得し、パイプライン入力を使用してOrch2:\Productionにコピーします。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。

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

### -Destination
テストセットをコピーする宛先フォルダを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
コピーするテストセットの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソースフォルダを指定します。指定しない場合、現在のフォルダがソースとして使用されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行した場合の動作を示します。
コマンドレットは実行されません。

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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

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
すべてのサブフォルダからテストセットを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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

### UiPath.PowerShell.Entities.TestSet
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

テストセットには、自動テストのテストケースのコレクションが含まれています。環境間でコピーする場合は、関連するテストケースが宛先フォルダに存在することを確認し、必要に応じてテストセット構成を調整してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchTestSet](Get-OrchTestSet.md)

[Remove-OrchTestSet](Remove-OrchTestSet.md)

[Set-OrchTestSet](Set-OrchTestSet.md)

[Start-OrchTestSet](Start-OrchTestSet.md)
