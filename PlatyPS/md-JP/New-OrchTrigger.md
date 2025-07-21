---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchTrigger

## SYNOPSIS
プロセス実行用の自動化トリガーを作成します。

## SYNTAX

```
New-OrchTrigger [-Name] <String[]> [-ReleaseName] <String> [-Enabled <String>] [-Priority <String>]
 [-StartStrategy <Int32>] [-StopStrategy <String>] [-StopProcessExpression <String>]
 [-KillProcessExpression <String>] [-AlertPendingExpression <String>] [-AlertRunningExpression <String>]
 [-ConsecutiveJobFailuresThreshold <Int32>] [-JobFailuresGracePeriodInHours <Int32>] [-RuntimeType <String>]
 [-InputArguments <String>] [-ResumeOnSameContext <String>] [-RunAsMe <String>] [-IsConnected <String>]
 [-CalendarName <String>] [-ActivateOnJobComplete <String>] [-ItemsActivationThreshold <Int32>]
 [-ItemsPerJobActivationTarget <Int32>] [-MaxJobsForActivation <Int32>] [-StartProcessCronDetails <String>]
 [-StartProcessCron <String>] [-QueueDefinitionName <String>] [-TimeZone <String>]
 [-StopProcessDate <DateTime>] [-ExecutorRobots <String[]>] [-MachineRobots <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
New-OrchTrigger コマンドレットは、さまざまな条件に基づいてプロセス実行を自動的に開始する自動化トリガーを作成します。トリガーは、時間ベース（スケジュール済み）、キューベース（キューアイテムによってアクティブ化）、またはイベントベース（特定の条件によってトリガー）にできます。

**これはフォルダエンティティコマンドレットです。** このコマンドレットを使用するには、最初にSet-Location（cd）を使用してターゲットフォルダに移動するか、-Pathパラメータを使用してターゲットフォルダを指定する必要があります。フォルダコンテキストなしでこのコマンドレットを実行しようとすると、"最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定してください。"というエラーが表示されます。

トリガーは、Cron式を使用した時間ベースのスケジューリング、アイテムがキューに追加されたときのキューベースのアクティベーション、カレンダー統合、ロボット割り当て、障害処理ポリシーなどの高度な構成を含む、さまざまな実行戦略をサポートします。

主要エンドポイント: POST /odata/ProcessSchedules
OAuth必須スコープ: OR.Execution または OR.Execution.Write
必要な権限: Execution.Create

## EXAMPLES

### Example 1
```powershell
New-OrchTrigger DailyTrigger InvoiceProcessing
```

位置パラメータを使用して"InvoiceProcessing"プロセス用の"DailyTrigger"という名前の基本トリガーを作成します。

### Example 2
```powershell
New-OrchTrigger BusinessHoursTrigger DataProcessor -StartProcessCron "0 9 * * 1-5" -TimeZone "UTC" -Enabled "True"
```

Cronスケジューリングを使用して平日の午前9時にDataProcessorプロセスを実行するトリガーを作成します。

### Example 3
```powershell
New-OrchTrigger QueueTrigger EmailHandler -QueueDefinitionName "EmailQueue" -ItemsActivationThreshold 5 -MaxJobsForActivation 3
```

EmailQueueに5個以上のアイテムがある場合にEmailHandlerプロセスを開始し、最大3つの並行ジョブを許可するキューベースのトリガーを作成します。

### Example 4
```powershell
New-OrchTrigger CriticalTrigger EmergencyResponse -Priority "High" -RuntimeType "Unattended" -RunAsMe "True" -MachineRobots "Robot1", "Robot2"
```

特定のロボット割り当てとRunAsMe実行コンテキストを持つ高優先度トリガーを作成します。

### Example 5
```powershell
New-OrchTrigger -Path Orch1:\Production MonthlyReport ReportGenerator -CalendarName "BusinessCalendar" -StopProcessDate (Get-Date).AddMonths(1)
```

カレンダー統合と自動停止日を持つProductionフォルダでトリガーを作成します。

### Example 6
```powershell
New-OrchTrigger FailsafeTrigger BackupProcess -ConsecutiveJobFailuresThreshold 3 -JobFailuresGracePeriodInHours 2 -AlertPendingExpression "duration > 30" -WhatIf
```

障害処理とアラート構成を持つトリガーを作成する際に何が起こるかを表示します。

## PARAMETERS

### -ActivateOnJobComplete
{{ Fill ActivateOnJobComplete Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AlertPendingExpression
{{ Fill AlertPendingExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AlertRunningExpression
{{ Fill AlertRunningExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CalendarName
{{ Fill CalendarName Description }}

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

### -ConsecutiveJobFailuresThreshold
{{ Fill ConsecutiveJobFailuresThreshold Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Enabled
作成時にトリガーが有効（"True"）か無効（"False"）かを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -InputArguments
{{ Fill InputArguments Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -IsConnected
{{ Fill IsConnected Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ItemsActivationThreshold
トリガーをアクティブ化するために必要なキューアイテムの最小数を指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ItemsPerJobActivationTarget
{{ Fill ItemsPerJobActivationTarget Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -JobFailuresGracePeriodInHours
{{ Fill JobFailuresGracePeriodInHours Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -KillProcessExpression
{{ Fill KillProcessExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MachineRobots
{{ Fill MachineRobots Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MaxJobsForActivation
このトリガーによって開始できる並行ジョブの最大数を指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
作成するトリガーの名前を指定します。トリガー名はフォルダ内で一意である必要があります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
トリガーが作成されるターゲットフォルダのパスを指定します。現在の場所を変更せずに特定のフォルダでトリガーを作成するには、このパラメータを使用します。

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

### -Priority
トリガーされた実行のジョブ優先度を指定します。有効な値には、Low、Normal、High、Criticalがあります。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -QueueDefinitionName
キューベースのトリガー用のキュー名を指定します。このキューにアイテムが追加されるとトリガーがアクティブになります。

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

### -ReleaseName
このトリガーが実行するプロセス（リリース）の名前を指定します。プロセスは同じフォルダに存在する必要があります。

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

### -ResumeOnSameContext
{{ Fill ResumeOnSameContext Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RunAsMe
{{ Fill RunAsMe Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RuntimeType
ロボットランタイムタイプを指定します。有効な値には、Unattended、NonProduction、TestAutomationがあります。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartProcessCron
時間ベースのスケジューリング用のCron式を指定します。標準のCron形式を使用します（例：平日の午前9時の場合は"0 9 * * 1-5"）。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartProcessCronDetails
{{ Fill StartProcessCronDetails Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartStrategy
{{ Fill StartStrategy Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StopProcessDate
{{ Fill StopProcessDate Description }}

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StopProcessExpression
{{ Fill StopProcessExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StopStrategy
{{ Fill StopStrategy Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TimeZone
{{ Fill TimeZone Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。コマンドレットは実行されません。

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

### -ExecutorRobots
{{ Fill ExecutorRobots Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
### System.Nullable`1[[System.DateTime, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule
## NOTES
- トリガー名はフォルダ内で一意である必要があります
- Cron式は標準形式を使用します：秒 分 時 日 月 曜日 年
- キューベースのトリガーには既存のキューが必要です
- 実際の作成前に-WhatIfを使用して操作をプレビューすることを検討してください
- タイムゾーンは有効なTimeZone識別子として指定する必要があります
- ロボット割り当ては既存のロボットを参照する必要があります
- カレンダー統合には既存のカレンダーオブジェクトが必要です

## RELATED LINKS

[Get-OrchTrigger](Get-OrchTrigger.md)
[Update-OrchTrigger](Update-OrchTrigger.md)
[Remove-OrchTrigger](Remove-OrchTrigger.md)
[Enable-OrchTrigger](Enable-OrchTrigger.md)
[Disable-OrchTrigger](Disable-OrchTrigger.md)
