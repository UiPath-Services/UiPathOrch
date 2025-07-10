---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchTestSetSchedule

## SYNOPSIS
指定したフォルダー内のテストセットスケジュールを有効にします。

## SYNTAX

```
Enable-OrchTestSetSchedule [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchTestSetSchedule コマンドレットは、UiPath Orchestrator内の指定したフォルダーで以前に無効化されたテストセットスケジュールを有効にします。このコマンドレットは、スケジュールされたテスト実行を再アクティブ化し、設定されたスケジュールに従って自動化されたテストセットを実行できるようにします。

テストセットスケジュールは、指定した間隔や時間でテストセットの実行を自動化します。スケジュールを有効にすると、メンテナンスやトラブルシューティングのために無効化された後に、スケジュールされたテスト実行機能が復元されます。

-Nameパラメーターを使用して、有効にするテストセットスケジュールを指定します。このコマンドレットは、複数のスケジュールを効率的に有効にするためのワイルドカードパターンをサポートしています。-Pathパラメーターでは特定のフォルダーをターゲットにでき、-Recurseはすべてのサブフォルダーの処理を有効にします。

これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定します。-Recurseパラメーターは、すべてのサブフォルダーからテストセットスケジュールを有効にします。

主要エンドポイント: POST /odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled

必要なOAuthスコープ: OR.TestSetSchedules または OR.TestSetSchedules.Write

必要な権限: TestSetSchedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Enable-OrchTestSetSchedule RegressionTestSchedule
```

現在のフォルダー（Development）でRegressionTestScheduleを位置パラメーターを使用して有効にします。

### Example 2
```powershell
PS C:\> Enable-OrchTestSetSchedule -Path Orch1:\Development SmokeTestSchedule
```

Orch1:\DevelopmentフォルダーでSmokeTestScheduleを有効にします。

### Example 3
```powershell
PS Orch1:\Development> Enable-OrchTestSetSchedule *Daily*, *Weekly* -WhatIf
```

現在のフォルダーでDailyやWeeklyを含む名前の複数のテストセットスケジュールを有効にした場合の動作を、安全のために-WhatIfを使用して表示します。

### Example 4
```powershell
PS C:\> Enable-OrchTestSetSchedule -Path Orch1:\Development *Automated* -Confirm
```

Developmentフォルダーで名前にAutomatedを含むすべてのテストセットスケジュールを確認プロンプトとともに有効にします。

### Example 5
```powershell
PS Orch1:\> Enable-OrchTestSetSchedule -Recurse *Nightly*
```

すべてのサブフォルダーから名前にNightlyを含むすべてのテストセットスケジュールを再帰的に有効にします。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSetSchedule -Enabled $false | Enable-OrchTestSetSchedule -WhatIf
```

すべての無効化されたテストセットスケジュールを取得し、パイプライン入力を使用してそれらを有効にした場合の動作を表示します。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認を求めます。

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

### -Depth
-Recurseパラメーターを使用するときに含めるサブフォルダーレベルの最大数を指定します。

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
有効にするテストセットスケジュールの名前を指定します。

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
対象のフォルダーを指定します。指定しない場合、現在のフォルダーが対象になります。

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
すべてのサブフォルダーからテストセットスケジュールを再帰的に有効にすることを指定します。

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

### -WhatIf
コマンドレットが実行された場合の動作を表示します。
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

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定します。

テストセットスケジュールは、指定した間隔でテスト実行を自動化します。スケジュールを有効にすると、無効化された後にスケジュールされたテスト実行機能が復元されます。スケジュール操作を一時的に停止するには、Disable-OrchTestSetScheduleを使用します。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには-WhatIfを使用します。

## RELATED LINKS

[Disable-OrchTestSetSchedule](Disable-OrchTestSetSchedule.md)

[Get-OrchTestSetSchedule](Get-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](Remove-OrchTestSetSchedule.md)

[Set-OrchTestSetSchedule](Set-OrchTestSetSchedule.md)
