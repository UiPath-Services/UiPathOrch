---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchJobStats

## SYNOPSIS
UiPath Orchestratorからジョブ統計を取得します。

## SYNTAX

```
Get-OrchJobStats [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchJobStats` コマンドレットは、UiPath Orchestratorのジョブに関する統計情報を取得します。実行ステータス（Successful、Faulted、Stopped、Running、Pending、Stopping、Terminating、Suspended、Resumed）でグループ化されたジョブの数を返します。

ジョブ統計は、テナント全体のスコープで動作するテナントエンティティです。-Pathパラメーターを使用して、ドライブ名（例：Orch1:、Orch2:）でターゲットテナントを指定します。

このコマンドレットは、Orchestrator環境全体のジョブ実行ステータスの簡単な概要を提供し、監視およびレポート目的に有用です。

-Pathパラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値のオートコンプリートを使用できます。

主要エンドポイント: GET /api/Stats/GetJobsStats

OAuth必要スコープ: OR.Monitoring または OR.Monitoring.Read

必要な権限: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1:
PS Orch1:\> Get-OrchJobStats
```

現在のOrchestratorインスタンスのジョブ統計を取得し、ステータス別のジョブ数を表示します。

### Example 2
```powershell
PS Orch1:\> $stats = Get-OrchJobStats
PS Orch1:\> $stats | Where-Object {$_.count -gt 0}
```

ジョブ統計を取得し、ゼロ以外の数のジョブがあるステータスのみを表示するようにフィルタリングします。

### Example 3
```powershell
PS Orch1:\> Get-OrchJobStats | Sort-Object count -Descending | Select-Object Path, title, count
```

ジョブ統計を取得し、数の降順でソートして表示し、titleとcountプロパティのみを表示します。

### Example 4
```powershell
PS Orch1:\> $total = (Get-OrchJobStats | Measure-Object count -Sum).Sum
PS Orch1:\> Write-Host "Total jobs: $total"
```

すべてのステータスのジョブの総数を計算して表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchJobStats -Path Orch1:, Orch2:
```

複数のテナントからジョブ統計を取得します。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットとなります。このパラメーターはパイプライン入力を受け取り、複数のパスを指定するためのワイルドカードをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 現在の場所
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
Write-Progressコマンドレットによって生成される進行状況バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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

### UiPath.PowerShell.Entities.CountStats
## NOTES
- 統計には、Successful、Faulted、Stopped、Running、Pending、Stopping、Terminating、Suspended、Resumedのすべてのジョブステータスが含まれます
- hasPermissionsプロパティは、現在のユーザーが各ステータスのジョブ数を表示する権限を持っているかどうかを示します
- このコマンドレットは、実行時の現在のジョブ統計のスナップショットを提供します

主要エンドポイント: GET /api/Stats/GetJobsStats
OAuth必要スコープ: [プレースホルダー]
必要な権限: [プレースホルダー]

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)
[Get-OrchLicenseStats](Get-OrchLicenseStats.md)
[about_UiPathOrch](about_UiPathOrch.md)
