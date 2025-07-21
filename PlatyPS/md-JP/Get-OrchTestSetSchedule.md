---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestSetSchedule

## SYNOPSIS
テストスケジュールを取得します。

## SYNTAX

```
Get-OrchTestSetSchedule [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestSetSchedule コマンドレットは、UiPath Orchestratorからテストセットスケジュールを取得します。テストセットスケジュールは、タイミング、頻度、実行パラメータを含むテストセットの自動実行計画を定義します。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、最初にSet-Location（cd）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

テストセットスケジュールは、事前に決められた時間または間隔でテストセットを実行することで自動テストシナリオを可能にし、継続的インテグレーションと品質保証ワークフローをサポートします。

主要エンドポイント: Get /odata/TestSetSchedules

OAuth必須スコープ: OR.TestSetExecutions または OR.TestSetExecutions.Read

必要な権限: TestSetExecutions.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule
```

現在のフォルダ内のすべてのテストセットスケジュールを取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule NightlyRegression
```

位置パラメータを使用して、現在のフォルダから"NightlyRegression"という名前のテストセットスケジュールを取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule *Daily*
```

ワイルドカードパターンマッチングを使用して、名前に"Daily"を含むすべてのテストセットスケジュールを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchTestSetSchedule -Recurse
```

現在のフォルダとそのすべてのサブフォルダから再帰的にすべてのテストセットスケジュールを取得します。

### Example 5
```powershell
PS C:\> Get-OrchTestSetSchedule -Path Orch1:\Production
```

任意の場所からの実行を示して、Productionフォルダからテストセットスケジュールを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchTestSetSchedule -Path \Production, \Shared -Recurse
```

-Recurseと-Pathパラメータの優先順位を示して、複数の特定フォルダから再帰的にテストセットスケジュールを取得します。

### Example 7
```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule | ConvertTo-Json -Depth 2
```

すべてのテストセットスケジュールを取得し、スケジュールプロパティの詳細分析のためにJSON形式でその構造を表示します。

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

### -Name
取得するテストスケジュールの名前を指定します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule
## NOTES

主要エンドポイント: GET /odata/TestSetSchedules
OAuth必須スコープ: OR.TestSetSchedules または OR.TestSetSchedules.Read
必要な権限: TestSetSchedules.View

## RELATED LINKS
