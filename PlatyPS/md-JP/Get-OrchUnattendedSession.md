---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUnattendedSession

## SYNOPSIS
UiPath Orchestratorから無人ロボットセッションを取得します。

## SYNTAX

```
Get-OrchUnattendedSession [[-Status] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchUnattendedSession コマンドレットは、UiPath Orchestratorから無人ロボットセッションに関する情報を取得します。無人セッションは、人間の介入なしにプロセスを実行できるロボットランタイム環境を表します。各セッションは、ロボットマシン、ランタイムタイプ、ステータス、および構成に関する詳細を提供します。

これは、Orchestratorインスタンス全体のセッション情報を取得するテナントレベルの操作です。

主要エンドポイント: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes

OAuth必須スコープ: OR.Robots または OR.Robots.Read

必要な権限: Robots.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchUnattendedSession
```

現在のOrchestratorテナントからすべての無人ロボットセッションを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchUnattendedSession Available, Disconnected
```

接続が切断されているか利用可能な無人セッションを取得し、複数ステータスフィルタリングの例を示します。

### Example 3
```powershell
PS C:\> Get-OrchUnattendedSession -Path Orch1:, Orch2:
```

テナント間分析のために、指定された複数のOrchestratorテナントから無人セッションを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchUnattendedSession | ConvertTo-Json -Depth 2
```

無人セッションを取得し、ロボットセッションプロパティの詳細分析のためにJSON形式でその構造を表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchUnattendedSession | Where-Object {$_.RuntimeType -eq 'Headless'}
```

パイプラインフィルタリングを使用してヘッドレスランタイムの無人セッションのみを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchUnattendedSession | Select-Object Path, MachineName, RuntimeType, Status, ReportingTime
```

マルチテナント識別のためにPathを最初に表示して、選択されたプロパティで無人セッションを取得します。

## PARAMETERS

### -Path
無人セッションを検索するフォルダパスを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。現在の場所を変更せずに特定のフォルダをターゲットにしたい場合にこのパラメータを使用します。

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

### -Status
取得する無人セッションのステータスを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。一般的なステータス値には、"Connected"、"Disconnected"、"Busy"、および"Unknown"があります。

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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.MachineSessionRuntime
## NOTES

このコマンドレットはフォルダエンティティで動作します。つまり、特定のフォルダに移動するか、-Pathパラメータを使用して目的の場所をターゲットにする必要がある場合があります。

大きなフォルダ階層で -Recurse パラメータを使用する場合、操作が完了するまでにかなりの時間がかかる場合があります。必要に応じて -Depth を使用してトラバーサルスコープを制限することを検討してください。

無人セッションは、無人ロボット上で実行されているアクティブな自動化セッションを表します。この情報は以下に有用です：
- ロボットの利用率とパフォーマンスの監視
- ボトルネックとリソース競合の特定
- 容量計画とスケーリングの決定
- 自動化実行の問題のトラブルシューティング

セッションオブジェクトの完全な構造を探索するにはConvertTo-Jsonを使用してください。デフォルトの表示形式では表示されない詳細なランタイム情報が含まれています。

主要エンドポイント: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes
OAuth必須スコープ: OR.Robots または OR.Robots.Read
必要な権限: Robots.View

## RELATED LINKS

[Get-OrchRobot](Get-OrchRobot.md)
[Get-OrchMachine](Get-OrchMachine.md)
[Get-OrchJob](Get-OrchJob.md)
[Get-OrchLicenseRuntime](Get-OrchLicenseRuntime.md)
