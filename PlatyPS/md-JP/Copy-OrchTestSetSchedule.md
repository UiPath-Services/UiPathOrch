---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTestSetSchedule

## SYNOPSIS
テストセットスケジュールを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchTestSetSchedule [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchTestSetSchedule コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースフォルダから宛先フォルダにテストセットスケジュールをコピーします。このコマンドレットは、タイミング構成、実行パラメーター、メタデータを含むテストセットスケジュールの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。テストセットスケジュールは、テストセットの自動実行タイミングを定義しており、このコマンドレットは、異なる環境間でテスト自動化スケジュールをデプロイするために不可欠です。

-Name パラメーターを使用してコピーするテストセットスケジュールを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のスケジュールを効率的にコピーするためのワイルドカードパターンをサポートしています。コピーされたスケジュールは、関連するテストセットが宛先フォルダで異なる名前を持つ場合、調整が必要になる可能性があることに注意してください。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからテストセットスケジュールをコピーし、宛先でフォルダ構造を維持できます。

主要エンドポイント: GET /odata/TestSetSchedules, POST /odata/TestSetSchedules

OAuth 必要なスコープ: OR.TestSetSchedules

必要な権限: TestSetSchedules.View, TestSetSchedules.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTestSetSchedule NightlyRegressionSchedule Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内のProductionフォルダにNightlyRegressionScheduleをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchTestSetSchedule -Path Orch1:\Development WeeklyTestSchedule Orch2:\Production
```

Orch1:\DevelopmentからOrch2:\ProductionにWeeklyTestScheduleをコピーし、テナント間テストセットスケジュールコピーを示しています。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTestSetSchedule *Daily*, *Weekly* Orch1:\Production -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダからProductionフォルダにDailyまたはWeeklyが含まれる名前の複数のテストセットスケジュールをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchTestSetSchedule -Path Orch1:\Development *Automated* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\DevelopmentからOrch2:\ProductionにAutomatedが含まれるすべてのテストセットスケジュールをテナント間コピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchTestSetSchedule -Recurse *Nightly* Orch2:\Finance -WhatIf
```

すべてのサブフォルダからNightlyが含まれるテストセットスケジュールを再帰的にOrch2:\Financeにコピーする場合に何が起こるかを示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSetSchedule *Regression* | Copy-OrchTestSetSchedule -Destination Orch2:\Production
```

Regressionが含まれる名前のすべてのテストセットスケジュールを取得し、パイプライン入力を使用してOrch2:\Productionにコピーします。

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
テストセットスケジュールをコピーする宛先フォルダを指定します。

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
コピーするテストセットスケジュールの名前を指定します。

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
すべてのサブフォルダからテストセットスケジュールを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

テストセットスケジュールは、テストセットの自動実行タイミングを定義します。環境間でコピーする場合は、関連するテストセットが宛先フォルダに存在することを確認し、必要に応じてスケジュール構成を調整してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchTestSetSchedule](Get-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](Remove-OrchTestSetSchedule.md)

[Set-OrchTestSetSchedule](Set-OrchTestSetSchedule.md)

[Copy-OrchTestSet](Copy-OrchTestSet.md)
