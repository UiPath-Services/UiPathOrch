---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchTrigger

## SYNOPSIS
指定された自動化トリガーを無効にして、スケジュールされた実行を防止します。

## SYNTAX

```
Disable-OrchTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchTriggerコマンドレットは、UiPath Orchestrator内の指定された自動化トリガーを無効にし、設定されたスケジュールや条件に従って実行されないようにします。トリガーは自動化プロセスがいつ、どのように実行されるべきかを定義し、無効にすることで自動化実行が一時的に停止されます。

トリガーを無効にすることは、メンテナンス期間、トラブルシューティング、テストシナリオ、または特定の自動化プロセスを一時的に停止する必要がある場合に便利です。トリガー設定は保持され、Enable-OrchTriggerを使用して簡単に再有効化できます。

このコマンドレットはフォルダエンティティ操作として動作し、適切なフォルダコンテキストに移動するか、-Pathパラメータを使用してターゲットフォルダを指定する必要があります。サブフォルダ内のトリガーを含めるには-Recurseパラメータを使用し、再帰レベルを制御するには-Depthを使用します。

トリガーの無効化は自動化実行スケジュールに直接影響します。操作をプレビューするには-WhatIfを使用し、ビジネスプロセスに影響を与える可能性のある複数のトリガーを無効にする場合は-Confirmを使用して確認プロンプトを表示します。

主要エンドポイント: POST /odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth必要スコープ: OR.Jobs

必要な権限: Schedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Disable-OrchTrigger DailyReportTrigger -WhatIf
```

現在のProductionフォルダでDailyReportTriggerを無効にする場合の動作を表示します。

### Example 2
```powershell
PS C:\> Disable-OrchTrigger -Path Orch1:\Production -Name *Maintenance* -Confirm
```

確認プロンプトを表示して、Productionフォルダで"Maintenance"が含まれる名前のすべてのトリガーを無効にします。

### Example 3
```powershell
PS Orch1:\> Disable-OrchTrigger -Recurse TestTrigger1, TestTrigger2
```

すべてのフォルダでTestTrigger1とTestTrigger2を無効にします。

### Example 4
```powershell
PS Orch1:\Development> Disable-OrchTrigger *Debug* -WhatIf
```

Developmentフォルダで"Debug"が含まれる名前のすべてのトリガーを無効にする場合の動作を表示します。

### Example 5
```powershell
PS C:\> Disable-OrchTrigger -Path Orch1:\Production -Recurse -Depth 1 -Name WeekendTrigger
```

Productionフォルダとその直下のサブフォルダでWeekendTriggerを無効にします。

### Example 6
```powershell
PS Orch1:\> Get-OrchTrigger | Where-Object {$_.NextExecution -lt (Get-Date).AddDays(1)} | Disable-OrchTrigger -Confirm
```

翌日以内に実行予定のすべてのトリガーを確認プロンプトで無効にします。

## PARAMETERS

### -Depth
ターゲットフォルダへの再帰の深さを指定します。深さが0の場合は現在の場所のみを示します。より高い値はより多くのサブフォルダレベルを含みます。

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
無効にするトリガーの名前を指定します。柔軟なトリガー選択のためのワイルドカードパターンをサポートします。

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
検索するターゲットフォルダを指定します。指定されない場合、現在のフォルダコンテキストが使用されます。パス指定が必要なフォルダエンティティ操作用です。

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

### -Recurse
ターゲットフォルダとそのすべてのサブフォルダを操作に含めます。フォルダ階層全体での包括的なトリガー管理に不可欠です。

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

### -Confirm
コマンドレットを実行する前に確認を求めます。ビジネスプロセスに影響するトリガーを無効にする場合に強く推奨されます。

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

### -WhatIf
実際にトリガーを無効にせずに、コマンドレットが実行された場合の動作を表示します。操作範囲をプレビューするために強く推奨されます。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
このコマンドレットは、トリガー設定を削除せずに自動化トリガーの実行を停止するフォルダエンティティ操作です。トリガーを無効にすることは自動化スケジュールに直接影響し、ビジネスプロセスに影響を与える可能性があります。操作をプレビューするには-WhatIfを使用し、複数のトリガーを無効にする場合は安全のため-Confirmを使用してください。この操作にはターゲットフォルダでのSchedules.Edit権限が必要です。無効化されたトリガーは、すべての設定を保持したままEnable-OrchTriggerを使用して再有効化できます。

## RELATED LINKS

[Enable-OrchTrigger](Enable-OrchTrigger.md)

[Get-OrchTrigger](Get-OrchTrigger.md)

[Set-OrchTrigger](Set-OrchTrigger.md)

[Remove-OrchTrigger](Remove-OrchTrigger.md)
