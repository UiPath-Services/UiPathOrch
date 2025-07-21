---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTestDataQueue

## SYNOPSIS
テストデータキューを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchTestDataQueue [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchTestDataQueue コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースフォルダから宛先フォルダにテストデータキューをコピーします。このコマンドレットは、構成、スキーマ定義、構造メタデータを含むテストデータキューの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。テストデータキューには、自動テストシナリオで使用される構造化されたテストデータが含まれており、このコマンドレットは、テスト環境のデプロイと異なる開発段階での一貫したテストデータの維持に不可欠です。

-Name パラメーターを使用してコピーするテストデータキューを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のテストデータキューを効率的にコピーするためのワイルドカードパターンをサポートしています。テストデータアイテムはキュー構造とともにコピーされず、キュー定義とスキーマのみがコピーされることに注意してください。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからテストデータキューをコピーし、宛先でフォルダ構造を維持できます。

主要エンドポイント: GET /odata/TestDataQueues, POST /odata/TestDataQueues

OAuth 必要なスコープ: OR.TestDataQueues

必要な権限: TestDataQueues.View, TestDataQueues.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTestDataQueue CustomerTestData Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内のProductionフォルダにCustomerTestDataキューをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchTestDataQueue -Path Orch1:\Development UserAccountData Orch2:\Production
```

Orch1:\DevelopmentからOrch2:\ProductionにUserAccountDataテストデータキューをコピーし、テナント間テストデータキューコピーを示しています。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTestDataQueue *Test*, *Sample* Orch1:\Production -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダからProductionフォルダにTestまたはSampleが含まれる名前の複数のテストデータキューをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchTestDataQueue -Path Orch1:\Development *Data* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\DevelopmentからOrch2:\ProductionにDataが含まれるすべてのテストデータキューをテナント間コピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchTestDataQueue -Recurse *TestData* Orch2:\Finance -WhatIf
```

すべてのサブフォルダからTestDataが含まれるテストデータキューを再帰的にOrch2:\Financeにコピーする場合に何が起こるかを示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestDataQueue *QA* | Copy-OrchTestDataQueue -Destination Orch2:\Production
```

QAが含まれる名前のすべてのテストデータキューを取得し、パイプライン入力を使用してOrch2:\Productionにコピーします。

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
テストデータキューをコピーする宛先フォルダを指定します。

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
コピーするテストデータキューの名前を指定します。

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
すべてのサブフォルダからテストデータキューを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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

### UiPath.PowerShell.Entities.TestDataQueue
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

テストデータキューには、構造化されたテストデータ定義とスキーマが含まれています。環境間でコピーする場合、キューの構造とスキーマはコピーされますが、テストデータアイテム自体はコピーされません。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchTestDataQueue](Get-OrchTestDataQueue.md)

[Remove-OrchTestDataQueue](Remove-OrchTestDataQueue.md)

[Set-OrchTestDataQueue](Set-OrchTestDataQueue.md)

[New-OrchTestDataQueue](New-OrchTestDataQueue.md)
