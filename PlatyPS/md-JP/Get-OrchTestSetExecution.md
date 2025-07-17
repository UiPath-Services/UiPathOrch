---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestSetExecution

## SYNOPSIS
テストセット実行を取得します。

## SYNTAX

```
Get-OrchTestSetExecution [[-Name] <String[]>] [-Status <String[]>] [-Last <String>]
 [-StartTimeAfter <DateTime>] [-StartTimeBefore <DateTime>] [-TriggerType <String[]>] [-Skip <UInt64>]
 [-First <UInt64>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestSetExecution コマンドレットは、UiPath Orchestratorからテストセット実行記録を取得します。テストセット実行は、ステータス、実行時間、結果を含むテストセット実行の履歴記録を表します。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、最初にSet-Location（cd）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

ステータス、時間範囲、トリガータイプ、その他の条件で実行をフィルタリングできます。このコマンドレットは、大きな結果セット用に-Skipと-Firstパラメータを通じてページネーションをサポートします。

主要エンドポイント: GET /odata/TestSetExecutions?$expand=TestSet

OAuth必須スコープ: OR.TestSetExecutions または OR.TestSetExecutions.Read

必要な権限: TestSetExecutions.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestSetExecution
```

現在のフォルダ内のすべてのテストセット実行を取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestSetExecution RegressionTests
```

位置パラメータを使用して、"RegressionTests"という名前のテストセットのすべての実行を取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestSetExecution -Status Failed, Stopped
```

分析とトラブルシューティングのために、FailedまたはStoppedステータスのすべてのテストセット実行を取得します。

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchTestSetExecution -Last Week
```

便利な時間フィルターを使用して、過去7日間のテストセット実行を取得します。

### Example 5
```powershell
PS C:\> Get-OrchTestSetExecution -Path Orch1:\Production -Recurse
```

任意の場所からの実行を示して、Productionフォルダとすべてのサブフォルダからテストセット実行を取得します。

## PARAMETERS

### -Depth
ターゲットフォルダへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダは含まれません。

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

### -First
指定された数のオブジェクトのみを取得します。
取得するオブジェクトの数を入力してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Last
最近の実行の時間期間を指定します。有効な値：Hour、Day、Week、Month、3Months、6Months、Year、3Years。

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

### -Name
取得するテストセット実行の名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットフォルダを指定します。指定されていない場合は、現在のフォルダがターゲットになります。

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
操作にターゲットフォルダとそのすべてのサブフォルダを含めることを指定します。

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

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。
スキップするオブジェクトの数を入力してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartTimeAfter
取得するテストセット実行のStartTimeの開始日時を指定します。

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

### -StartTimeBefore
取得するテストセット実行のStartTimeの終了日時を指定します。

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

### -Status
取得するテストセット実行のステータスを指定します。一般的な値には、Running、Successful、Failed、Stopped、Pendingがあります。

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

### -TriggerType
取得するテストセット実行のTriggerTypeを指定します。一般的な値には、Manual、Schedule、APIがあります。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestSetExecution
## NOTES

主要エンドポイント: GET /odata/TestSetExecutions
OAuth必須スコープ: OR.TestSetExecutions または OR.TestSetExecutions.Read
必要な権限: TestSetExecutions.View

## RELATED LINKS
