---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Open-OrchJob

## SYNOPSIS
デフォルトのWebブラウザでジョブの詳細を開きます。

## SYNTAX

```
Open-OrchJob [[-Id] <Int64[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Open-OrchJob`コマンドレットは、指定されたジョブIDのジョブ詳細ページをデフォルトのWebブラウザで開きます。これにより、Orchestrator Webインターフェースで詳細なジョブ情報、実行ログ、その他のジョブ関連データを直接表示する便利な方法を提供します。

このコマンドレットは、PowerShellからWebインターフェースに素早く移動してジョブの詳細を調査したり、実行ログを表示したり、コマンドラインでは利用できない機能にアクセスしたりするのに便利です。

複数のジョブIDを指定して、複数のジョブページを同時に開くことができます。[Ctrl+Space]または[Tab]を押すことで、ジョブIDのオートコンプリートを使用できます。

プライマリ エンドポイント: ジョブ詳細ページへのブラウザナビゲーション（直接のAPIエンドポイントなし）

OAuth 必要なスコープ: 該当なし（ブラウザナビゲーション）

必要な権限: Jobs.View（ジョブ詳細へのブラウザアクセス用）

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1:\MyWorkspace
PS Orch1:\MyWorkspace> Open-OrchJob -Id 108984122
```

ジョブID 108984122のジョブ詳細ページをデフォルトのWebブラウザで開きます。

### Example 2
```powershell
PS Orch1:\MyWorkspace> $job = Get-OrchJob -First 1
PS Orch1:\MyWorkspace> Open-OrchJob -Id $job.Id
```

最初のジョブを取得し、その詳細ページをブラウザで開きます。

### Example 3
```powershell
PS Orch1:\> Open-OrchJob -Id 108984122,108983813
```

複数のジョブのジョブ詳細ページを別々のブラウザタブまたはウィンドウで開きます。

### Example 4
```powershell
PS Orch1:\> Get-OrchJob -State "Faulted" -First 5 | Open-OrchJob
```

最初の5つの失敗したジョブを取得し、調査のためにそれらの詳細ページを開きます。

### Example 5
```powershell
PS Orch1:\MyWorkspace> Get-OrchJob -Last "1h" | Where-Object State -eq "Faulted" | Open-OrchJob
```

過去1時間で失敗したすべてのジョブのジョブ詳細ページを開きます。

## PARAMETERS

### -Id
ブラウザで開くジョブIDを指定します。複数のページを開くために複数のジョブIDを指定できます。このパラメータの補完リストには、UiPathOrchによって以前に取得されメモリにキャッシュされたジョブIDが含まれます。

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットフォルダパスを指定します。指定されていない場合は、現在のフォルダがターゲットになります。このパラメータはパイプライン入力を受け入れ、複数のパスを指定するためのワイルドカードをサポートしています。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
Write-Progressコマンドレットによって生成される進行状況バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況更新にPowerShellがどのように応答するかを指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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

### System.Object
## NOTES
- これは、PowerShellからOrchestrator Webインターフェースに移動するためのユーティリティコマンドレットです
- コマンドレットは、アクティブなWebブラウザとOrchestratorインスタンスへのインターネット接続が必要です
- 複数のジョブIDは、ブラウザ設定に応じて複数のブラウザタブまたはウィンドウを開きます
- ユーザーは、Webインターフェースでジョブの詳細を表示する適切な権限を持っている必要があります

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)
[Start-OrchJob](Start-OrchJob.md)
[Stop-OrchJob](Stop-OrchJob.md)
[about_UiPathOrch](about_UiPathOrch.md)
