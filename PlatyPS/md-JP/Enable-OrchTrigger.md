---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchTrigger

## SYNOPSIS
指定されたオートメーショントリガーを有効にして、スケジュールされた実行を再開します。

## SYNTAX

```
Enable-OrchTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchTriggerコマンドレットは、UiPath Orchestrator内の指定されたオートメーショントリガーを有効にし、オートメーションプロセスのスケジュールされた実行を再開します。トリガーは、時刻ベースのスケジュール、キューベースのトリガー、その他のオートメーション起動条件を含む、オートメーションプロセスがいつ、どのように実行されるかを定義します。

トリガーが有効になると、アクティブになり、設定されたスケジュールまたは条件に従って実行されます。このコマンドレットは、メンテナンス期間、トラブルシューティング後、または一時的に無効にされていたトリガーの実行を再開するために不可欠です。

このコマンドレットは、フォルダエンティティ操作として動作し、適切なフォルダコンテキストへの移動または-Pathパラメーターを使用したターゲットフォルダーの指定が必要です。サブフォルダー内のトリガーを含めるには-Recurseパラメーターを使用し、再帰レベルを制御するには-Depthを使用します。

トリガーの有効化は、オートメーション実行スケジュールに直接影響する重要な操作です。複数のトリガーを有効にする際は、操作をプレビューするために-WhatIf、確認プロンプトのために-Confirmを使用してください。

主要エンドポイント: POST /odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth必須スコープ: OR.Jobs

必要な権限: Schedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Enable-OrchTrigger DailyReportTrigger
```

現在のProductionフォルダー内のDailyReportTriggerを有効にします。

### Example 2
```powershell
PS C:\> Enable-OrchTrigger -Path Orch1:\Production -Name *Invoice* -WhatIf
```

Productionフォルダー内で"Invoice"を含む名前のすべてのトリガーを有効にした場合に何が起こるかを表示します。

### Example 3
```powershell
PS Orch1:\> Enable-OrchTrigger -Recurse MonthlyTrigger, WeeklyTrigger -Confirm
```

確認プロンプトとともに、すべてのフォルダーでMonthlyTriggerとWeeklyTriggerを有効にします。

### Example 4
```powershell
PS Orch1:\Development> Enable-OrchTrigger TestTrigger1, TestTrigger2
```

Developmentフォルダー内の複数のテストトリガーを有効にします。

### Example 5
```powershell
PS C:\> Enable-OrchTrigger -Path Orch1:\Production -Recurse -Depth 2 -Name *Schedule*
```

Productionフォルダーと最大2レベルのサブフォルダーで、"Schedule"を含む名前のすべてのトリガーを有効にします。

### Example 6
```powershell
PS Orch1:\> Get-OrchTrigger | Where-Object {$_.Enabled -eq $false} | Enable-OrchTrigger -WhatIf
```

パイプライン入力を使用して、現在無効になっているすべてのトリガーを有効にした場合に何が起こるかを表示します。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深さを指定します。深さ0は現在の場所のみを示します。より高い値はより多くのサブフォルダーレベルを含みます。

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
有効にするトリガーの名前を指定します。柔軟なトリガー選択のためのワイルドカードパターンをサポートします。

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
検索するターゲットフォルダーを指定します。指定されていない場合、現在のフォルダーコンテキストが使用されます。パス指定が必要なフォルダーエンティティ操作用。

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
操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。フォルダー階層全体にわたる包括的なトリガー管理に不可欠です。

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
コマンドレットを実行する前に確認を求めます。オートメーションスケジュールに影響する複数のトリガーを有効にする際に推奨されます。

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
実際にトリガーを有効にすることなく、コマンドレットが実行された場合に何が起こるかを表示します。操作範囲をプレビューするために推奨されます。

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
このコマンドレットは、スケジュールされた実行のためにオートメーショントリガーをアクティブ化するフォルダーエンティティ操作です。トリガーの有効化は、オートメーションスケジュールとプロセス実行に直接影響します。複数のトリガーを有効にする際の安全性のために、操作をプレビューするには-WhatIf、-Confirmを使用してください。この操作にはターゲットフォルダーでのSchedules.Edit権限が必要です。有効になると、トリガーは設定されたスケジュールと条件に従って実行されます。

## RELATED LINKS

[Disable-OrchTrigger](Disable-OrchTrigger.md)

[Get-OrchTrigger](Get-OrchTrigger.md)

[Set-OrchTrigger](Set-OrchTrigger.md)

[Add-OrchTrigger](Add-OrchTrigger.md)
