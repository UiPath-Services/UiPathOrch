---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchWebhook

## SYNOPSIS
UiPath Orchestrator から Webhook を取得します。

## SYNTAX

`
Get-OrchWebhook [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-OrchWebhook コマンドレットは、UiPath Orchestrator から Webhook 情報を取得します。Webhook は HTTP コールバックで、ジョブ完了、プロセスデプロイメント、キューアイテム更新などの特定のイベントが発生したときに、Orchestrator が外部システムにリアルタイム通知を送信できるようにします。

このコマンドレットは、構成された Webhook に関する詳細情報を、URL、イベントサブスクリプション、セキュリティ設定、および運用ステータスを含めて返します。ワイルドカードを使用した Webhook 名でのフィルタリングをサポートします。

Webhook は、Orchestrator からのリアルタイムイベント通知を提供することで、外部監視システム、通知プラットフォーム、およびカスタムアプリケーションとの統合を可能にします。

-Name と -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /odata/Webhooks

OAuth 必須スコープ: OR.Webhooks または OR.Webhooks.Read

必要な権限: Webhooks.View

## EXAMPLES

### Example 1
`powershell
PS Orch1:\> Get-OrchWebhook
`

現在の Orchestrator インスタンスで構成されているすべての Webhook を取得します。

### Example 2
`powershell
PS Orch1:\> Get-OrchWebhook my*
`

ワイルドカードパターンマッチングを使用して、名前が "my" で始まる Webhook を取得します。

### Example 3
`powershell
PS C:\> Get-OrchWebhook -Path Orch1:, Orch2: integration*
`

複数のテナントから名前が "integration" で始まる Webhook を取得します。

### Example 4
`powershell
PS Orch1:\> Get-OrchWebhook mywebhook | ConvertTo-Json
`

特定の Webhook を取得し、Events 配列などのネストされたプロパティを含む完全な構造を表示します。

## PARAMETERS

### -Name
取得する Webhook の名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space] または [Tab] を押すことで自動補完を使用できます。指定しない場合、すべての Webhook が返されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All webhooks
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
ターゲットドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットになります。このパラメーターは、複数の Orchestrator インスタンスを指定するためのパイプライン入力を受け取ります。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新 (Write-Progress コマンドレットによって生成される進行状況バーなど) に対して PowerShell が応答する方法を指定します。有効な値は次のとおりです: SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None

## OUTPUTS

### UiPath.PowerShell.Entities.Webhook

## NOTES
- Webhook は、Orchestrator から外部システムへのリアルタイムイベント通知を提供します
- Events プロパティには、Webhook をトリガーするイベントを指定する WebhookEvent オブジェクトの配列が含まれています
- 一般的なイベントタイプには次があります: job.created、job.faulted、job.completed、process.deleted、queue.created、queue.deleted、およびその他多数
- SubscribeToAllEvents プロパティは、Webhook が利用可能なすべてのイベントタイプを受信するかどうかを示します
- AllowInsecureSsl プロパティは、Webhook が無効または自己署名 SSL 証明書を受け入れるかどうかを制御します
- Secret プロパティ（構成されている場合）は Webhook 署名検証に使用されますが、セキュリティのため表示されません
- Key プロパティには、Webhook の一意識別子が含まれています
- 無効な Webhook（Enabled = false）は、イベントが発生しても通知を送信しません

主要エンドポイント: GET /odata/Webhooks
OAuth 必須スコープ: OR.Webhooks または OR.Webhooks.Read
必要な権限: Webhooks.View

## RELATED LINKS

[Enable-OrchWebhook](Enable-OrchWebhook.md)
[Disable-OrchWebhook](Disable-OrchWebhook.md)
[Copy-OrchWebhook](Copy-OrchWebhook.md)
[Remove-OrchWebhook](Remove-OrchWebhook.md)
[about_UiPathOrch](about_UiPathOrch.md)
