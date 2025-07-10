---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTrigger

## SYNOPSIS
トリガーを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchTrigger [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchTrigger コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースフォルダから宛先フォルダにトリガーをコピーします。このコマンドレットは、スケジュール構成、プロセス関連付け、実行パラメーターを含むトリガーの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。トリガーは、自動化されたプロセス実行スケジュールと条件を定義しており、このコマンドレットは、異なる環境間で自動化スケジュールをデプロイするために不可欠です。

-Name パラメーターを使用してコピーするトリガーを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のトリガーを効率的にコピーするためのワイルドカードパターンをサポートしています。コピーされたトリガーは、関連するプロセスが宛先フォルダで異なる名前を持つ場合、調整が必要になる可能性があることに注意してください。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからトリガーをコピーし、宛先でフォルダ構造を維持できます。

プライマリエンドポイント: GET /odata/ProcessSchedules({key}), GET /odata/Releases, POST /odata/ProcessSchedules

OAuth 必要なスコープ: OR.Jobs OR.Execution

必要な権限: Schedules.View, Schedules.Create, Processes.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTrigger DailyReportTrigger Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内のProductionフォルダにDailyReportTriggerをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchTrigger -Path Orch1:\Development ScheduledBackup Orch2:\Production
```

Orch1:\DevelopmentからOrch2:\ProductionにScheduledBackupトリガーをコピーし、テナント間トリガーコピーを示しています。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTrigger *Daily*, *Weekly* Orch1:\Production -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダからProductionフォルダにDailyまたはWeeklyが含まれる名前の複数のトリガーをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchTrigger -Path Orch1:\Development *Scheduled* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\DevelopmentからOrch2:\ProductionにScheduledが含まれるすべてのトリガーをテナント間コピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchTrigger -Recurse *Automated* Orch2:\Finance -WhatIf
```

すべてのサブフォルダからAutomatedが含まれるトリガーを再帰的にOrch2:\Financeにコピーする場合に何が起こるかを示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTrigger *Batch* | Copy-OrchTrigger -Destination Orch2:\Production
```

Batchが含まれる名前のすべてのトリガーを取得し、パイプライン入力を使用してOrch2:\Productionにコピーします。

## PARAMETERS

### -Destination
トリガーをコピーする宛先フォルダを指定します。

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
コピーするトリガーの名前を指定します。

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
すべてのサブフォルダからトリガーを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

トリガーには、スケジュール構成とプロセス関連付けが含まれています。環境間でコピーする場合は、関連するプロセスが宛先フォルダに存在することを確認し、必要に応じてトリガー構成を調整してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchTrigger](Get-OrchTrigger.md)

[Remove-OrchTrigger](Remove-OrchTrigger.md)

[Set-OrchTrigger](Set-OrchTrigger.md)

[Start-OrchTrigger](Start-OrchTrigger.md)
