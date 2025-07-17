---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchTestSetSchedule

## SYNOPSIS
指定されたフォルダのテストセットスケジュールを無効にします。

## SYNTAX

```
Disable-OrchTestSetSchedule [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchTestSetScheduleコマンドレットは、UiPath Orchestrator内の指定されたフォルダのテストセットスケジュールを無効にします。このコマンドレットを使用すると、スケジュール設定を削除せずにスケジュールされたテスト実行を一時的に停止できるため、メンテナンスシナリオやテスト自動化ワークフローのトラブルシューティング時に役立ちます。

テストセットスケジュールは、指定された間隔や時間でテストセットの実行を自動化します。これを無効にすると、スケジュールされたテスト実行が開始されなくなりますが、将来の再有効化のためにスケジュール設定は保持されます。

-Nameパラメータを使用して、無効にするテストセットスケジュールを指定します。このコマンドレットは、複数のスケジュールを効率的に無効にするためのワイルドカードパターンをサポートします。-Pathパラメータを使用すると特定のフォルダを対象にでき、-Recurseを使用するとすべてのサブフォルダが処理されます。

これはフォルダエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定します。-Recurseパラメータを使用すると、すべてのサブフォルダからテストセットスケジュールを無効にできます。

主要エンドポイント: POST /odata/TestSetSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth必要スコープ: OR.TestSetSchedules または OR.TestSetSchedules.Write

必要な権限: TestSetSchedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Disable-OrchTestSetSchedule RegressionTestSchedule
```

位置パラメータを使用して、現在のフォルダ（Development）のRegressionTestScheduleを無効にします。

### Example 2
```powershell
PS C:\> Disable-OrchTestSetSchedule -Path Orch1:\Development SmokeTestSchedule
```

Orch1:\DevelopmentフォルダのSmokeTestScheduleを無効にします。

### Example 3
```powershell
PS Orch1:\Development> Disable-OrchTestSetSchedule *Daily*, *Weekly* -WhatIf
```

安全のために-WhatIfを使用して、現在のフォルダでDailyまたはWeeklyが含まれる名前の複数のテストセットスケジュールを無効にする際の動作を表示します。

### Example 4
```powershell
PS C:\> Disable-OrchTestSetSchedule -Path Orch1:\Development *Automated* -Confirm
```

確認プロンプトを表示して、DevelopmentフォルダでAutomatedが含まれるすべてのテストセットスケジュールを無効にします。

### Example 5
```powershell
PS Orch1:\> Disable-OrchTestSetSchedule -Recurse *Nightly*
```

すべてのサブフォルダから再帰的に、Nightlyが含まれるすべてのテストセットスケジュールを無効にします。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTestSetSchedule *Integration* | Disable-OrchTestSetSchedule -WhatIf
```

Integrationが含まれるすべてのテストセットスケジュールを取得し、パイプライン入力を使用してそれらを無効にする際の動作を表示します。

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

### -Depth
-Recurseパラメータを使用する際に含めるサブフォルダレベルの最大数を指定します。

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
無効にするテストセットスケジュールの名前を指定します。

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
対象フォルダを指定します。指定されない場合、現在のフォルダが対象になります。

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
すべてのサブフォルダから再帰的にテストセットスケジュールを無効にすることを指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定します。

テストセットスケジュールは、指定された間隔でテスト実行を自動化します。これを無効にすると、設定を保持しながらスケジュールされたテスト実行が一時的に停止されます。無効にされたスケジュールを再有効化するには、Enable-OrchTestSetScheduleを使用します。効率的な一括操作にはワイルドカードを使用し、実際の実行前にテストするには-WhatIfを使用してください。

## RELATED LINKS

[Enable-OrchTestSetSchedule](Enable-OrchTestSetSchedule.md)

[Get-OrchTestSetSchedule](Get-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](Remove-OrchTestSetSchedule.md)

[Set-OrchTestSetSchedule](Set-OrchTestSetSchedule.md)
