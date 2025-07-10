---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachine

## SYNOPSIS
UiPath Orchestratorからマシンを取得します。

## SYNTAX

```
Get-OrchMachine [[-Name] <String[]>] [-Path <String[]>] [-ExpandRobotUser] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorテナントからマシン情報を取得します。マシンは、ロボットがオートメーションプロセスを実行するコンピューターリソースを表し、物理マシンとロボットプロビジョニングに使用されるマシンテンプレートの両方を含みます。

このコマンドレットは、マシンタイプ（Standard、Template）、スコープ（Default、PersonalWorkspace、Serverless、AgentService）、ライセンススロット割り当て、ロボットユーザー割り当て、および設定詳細を含む包括的なマシン情報を返します。

マシンは、テナント全体のスコープで動作するテナントエンティティです。ドライブ名でターゲットテナントを指定するには、-Pathパラメーターを使用します。

プライマリエンドポイント: GET /odata/Machines?$expand=UpdateInfo

OAuth必須スコープ: OR.Machines または OR.Machines.Read

必要な権限: Machines.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchMachine
```

現在のテナントからすべてのマシンを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchMachine *Template*
```

名前に"Template"を含むマシンを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchMachine -Path Orch1:, Orch2:
```

複数のテナントからマシンを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchMachine | Where-Object {$_.Type -eq "Standard"}
```

標準（非テンプレート）マシンを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchMachine | Where-Object {$_.UnattendedSlots -gt 0}
```

無人ロボットスロットが割り当てられているマシンを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchMachine -ExpandRobotUser
```

拡張されたロボットユーザー詳細を含むマシンを取得します。

### Example 7
```powershell
PS Orch1:\> Get-OrchMachine -ExportCsv C:\Reports\Machines.csv
```

UTF-8 BOMエンコーディングを使用してすべてのマシンをCSVにエクスポートします。

## PARAMETERS

### -ExpandRobotUser
各マシンのロボットユーザー詳細を展開し、どのユーザーがマシン上でロボットを実行するように割り当てられているかを表示します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
取得するマシンの名前を指定します。ワイルドカードと複数の値をサポートします。

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
ドライブ名でターゲットテナントを指定します。複数のテナントにはコンマ区切りの値を使用します。指定されていない場合、現在のテナントをターゲットにします。

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

### -ProgressAction
コマンドレット実行中に進行状況情報がどのように表示されるかを制御します。

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

### -CsvEncoding
CSVエクスポートのエンコーディングを指定します。デフォルトはExcel互換性のためのBOM付きUTF-8です。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
BOM付きUTF-8エンコーディングを使用して結果をCSVファイルにエクスポートします。内部IDを人間が読める名前に自動変換します。エクスポートされたCSVは、Import-Csvで使用し、New-OrchMachineにパイプして一括操作に使用できます。

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

### CommonParameters
このコマンドレットは共通パラメーターをサポートします: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedMachine
### UiPath.PowerShell.Entities.RobotUser
## NOTES
マシンエンティティはテナントスコープであり、テナント全体で動作します。

マシンタイプには、Standard（物理マシン）とTemplate（プロビジョニング用のマシンテンプレート）があります。スコープには、Default、PersonalWorkspace、Serverless、AgentServiceがあります。

特定のマシンでロボットを実行するように割り当てられているユーザーの詳細情報が必要な場合は、-ExpandRobotUserを使用してください。

-ExportCsvパラメーターは、内部IDの代わりに人間が読める名前を持つインポート準備完了のCSVファイルを作成します。

プライマリエンドポイント: GET /odata/Machines
OAuth必須スコープ: OR.Machines または OR.Machines.Read
必要な権限: Machines.View

## RELATED LINKS

[New-OrchMachine](New-OrchMachine.md)

[Update-OrchMachine](Update-OrchMachine.md)

[Remove-OrchMachine](Remove-OrchMachine.md)

[Copy-OrchMachine](Copy-OrchMachine.md)
