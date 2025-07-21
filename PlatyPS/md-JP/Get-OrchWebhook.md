---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchWebhook

## SYNOPSIS
UiPath Orchestratorからウェブフックを取得します。

## SYNTAX

```
Get-OrchWebhook [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchWebhook コマンドレットは、UiPath Orchestratorからウェブフック情報を取得します。ウェブフックは、ジョブの完了、プロセスのデプロイメント、キューアイテムの更新などの特定のイベントが発生したときに、Orchestratorが外部システムにリアルタイム通知を送信できるHTTPコールバックです。

このコマンドレットは、URL、イベント購読、セキュリティ設定、運用ステータスなど、設定されたウェブフックの詳細情報を返します。ワイルドカードを使用したウェブフック名によるフィルタリングをサポートします。

ウェブフックは、Orchestratorからのリアルタイムイベント通知を提供することで、外部監視システム、通知プラットフォーム、カスタムアプリケーションとの統合を可能にします。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /odata/Webhooks

OAuth必須スコープ：OR.Webhooks または OR.Webhooks.Read

必須権限：Webhooks.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchWebhook
```

現在のOrchestratorインスタンスで設定されたすべてのウェブフックを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchWebhook my*
```

ワイルドカードパターンマッチングを使用して、名前が"my"で始まるウェブフックを取得します。

### Example 3
```powershell
PS C:\> Get-OrchWebhook -Path Orch1:, Orch2: integration*
```

複数のテナントから名前が"integration"で始まるウェブフックを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchWebhook mywebhook | ConvertTo-Json
```

特定のウェブフックを取得し、Eventsの配列などのネストされたプロパティを含む完全な構造を表示します。

## PARAMETERS

### -Name
取得するウェブフックの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべてのウェブフックが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All webhooks
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象ドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、複数のOrchestratorインスタンスを指定するためのパイプライン入力を受け入れます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新（Write-Progressコマンドレットによって生成される進行状況バーなど）にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### UiPath.PowerShell.Entities.Webhook
## NOTES
- ウェブフックは、Orchestratorから外部システムへのリアルタイムイベント通知を提供します
- Eventsプロパティには、ウェブフックをトリガーするイベントを指定するWebhookEventオブジェクトの配列が含まれます
- 一般的なイベントタイプには次のものがあります：job.created、job.faulted、job.completed、process.deleted、queue.created、queue.deleted、その他多数
- SubscribeToAllEventsプロパティは、ウェブフックがすべての利用可能なイベントタイプを受信するかどうかを示します
- AllowInsecureSslプロパティは、ウェブフックが無効または自己署名SSL証明書を受け入れるかどうかを制御します
- Secretプロパティ（設定されている場合）は、ウェブフック署名検証に使用されますが、セキュリティのため表示されません
- Keyプロパティには、ウェブフックの一意識別子が含まれます
- 無効化されたウェブフック（Enabled = false）は、イベントが発生しても通知を送信しません

主要エンドポイント：GET /odata/Webhooks
OAuth必須スコープ：OR.Webhooks または OR.Webhooks.Read
必須権限：Webhooks.View

## RELATED LINKS

[Enable-OrchWebhook](Enable-OrchWebhook.md)
[Disable-OrchWebhook](Disable-OrchWebhook.md)
[Copy-OrchWebhook](Copy-OrchWebhook.md)
[Remove-OrchWebhook](Remove-OrchWebhook.md)
[about_UiPathOrch](about_UiPathOrch.md)
