---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchExecutionSetting

## SYNOPSIS
UiPath Orchestratorで設定された実行設定を取得します。

## SYNTAX

```
Get-OrchExecutionSetting [[-Scope] <String[]>] [[-DisplayName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchExecutionSetting` コマンドレットは、UiPath Orchestrator内で設定された実行設定を取得します。実行設定は、ログレベル、コンソールアクセス、解像度設定、開発機能など、自動化実行動作のさまざまな側面を制御します。

設定はスコープ（GlobalとRobot）によって整理され、DisplayName、ValueType（Boolean、Integer、MultipleChoice）、DefaultValue、および列挙型のPossibleValuesなどのプロパティが含まれます。これらの設定は、自動化プロセスがOrchestrator環境でどのように実行され、動作するかに直接影響します。

このコマンドレットはテナントレベルのエンティティ操作として動作し、指定されたOrchestrator環境から設定を取得します。出力はスコープ別にグループ化され、現在の値と制約とともに利用可能なすべての実行設定オプションが表示されます。

主要エンドポイント: GET /odata/Settings

OAuth必要スコープ: OR.Settings または OR.Settings.Read

必要な権限: Settings.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchExecutionSetting
```

スコープ（GlobalとRobot）別にグループ化されたすべての実行設定を取得し、DisplayName、ValueType、DefaultValue、およびPossibleValuesを表示します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchExecutionSetting | ConvertTo-Json -Depth 3
```

Key、Scope、PathScope、およびすべての設定詳細を含む、詳細な実行設定プロパティをJSON形式で表示します。

### Example 3
```powershell
PS C:\> Get-OrchExecutionSetting -Path Orch1: -DisplayName "*Logging*"
```

Orch1テナントで、キーに"Logging"が含まれるすべての実行設定を取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchExecutionSetting | Where-Object {$_.ValueType -eq "Boolean"}
```

すべてのBoolean型の実行設定を取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchExecutionSetting | Where-Object {$_.PossibleValues.Count -gt 0} | Select-Object DisplayName, PossibleValues
```

列挙値（MultipleChoice型）を持つ設定とその可能なオプションを表示します。

### Example 6
```powershell
PS Orch1:\> Get-OrchExecutionSetting | Group-Object Scope
```

実行設定をスコープ（Global対Robot）でグループ化します。

## PARAMETERS

### -DisplayName
取得する実行設定の表示名を指定します。柔軟な設定選択のためにワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットテナントドライブを指定します。指定しない場合は、現在のテナントがターゲットとなります。テナントレベル操作用です。

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

### -Scope
取得する実行設定のスコープを指定します。有効な値は"Global"と"Robot"です。指定しない場合は、すべてのスコープの設定が返されます。

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

### -ProgressAction
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。デフォルト値はContinueです。

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

### UiPath.PowerShell.Entities.ExecutionSettingDefinition
## NOTES
このコマンドレットは、実行設定構成にアクセスするためのテナントレベルのエンティティ操作です。設定は、ログ、コンソールアクセス、解像度設定を含む自動化実行動作を制御します。出力はスコープ別にグループ化され、GlobalおよびRobotレベルの設定が表示されます。特定の設定をキーまたは値の型でフィルタリングするには、フィルタリングを使用してください。この操作にはSettings.View権限が必要です。

主要エンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetExecutionSettingsConfiguration
OAuth必要スコープ: OR.Settings または OR.Settings.Read
必要な権限: Settings.View

## RELATED LINKS

[Set-OrchExecutionSetting](Set-OrchExecutionSetting.md)

[Reset-OrchExecutionSetting](Reset-OrchExecutionSetting.md)

[Get-OrchSetting](Get-OrchSetting.md)

[Export-OrchExecutionSetting](Export-OrchExecutionSetting.md)
