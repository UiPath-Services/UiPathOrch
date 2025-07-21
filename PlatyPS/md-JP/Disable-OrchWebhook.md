---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchWebhook

## SYNOPSIS
Orchestrator内のWebhookを無効にします。

## SYNTAX

```
Disable-OrchWebhook [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchWebhookコマンドレットは、Orchestrator環境で以前に有効にされたWebhookを無効にします。Webhookを無効にすると、外部システムへのHTTP通知の送信が停止され、Webhookの設定を削除せずに統合を効果的に一時停止します。

Webhookの無効化は、メンテナンス期間中に通知を一時的に停止する場合、統合の問題をトラブルシューティングする場合、または外部システムが利用できない場合に有用です。Webhookの設定は保持され、後でEnable-OrchWebhookコマンドレットを使用して再度有効にできます。

このコマンドレットは、操作をプレビューする-WhatIfや、Webhookを無効にする前に確認を求める-Confirmなどの安全機能をサポートしています。

主要エンドポイント: PATCH /odata/Webhooks({webhookId})

OAuth必要スコープ: OR.Webhooks

必要な権限: Webhooks.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> Disable-OrchWebhook IntegrationHook
```

"IntegrationHook"という名前のWebhookを無効にします。

### Example 2
```powershell
PS C:\> Disable-OrchWebhook Hook1, Hook2
```

名前をカンマ区切りのリストで指定して、複数のWebhookを無効にします。

### Example 3
```powershell
PS C:\> Disable-OrchWebhook Test* -WhatIf
```

"Test"で始まる名前のすべてのWebhookを無効にした場合の動作を、実際に操作を実行せずに表示します。

### Example 4
```powershell
PS C:\> Disable-OrchWebhook MaintenanceHook -Path Orch1:, Orch2:
```

複数のOrchestrator環境で"MaintenanceHook"という名前のWebhookを無効にします。

### Example 5
```powershell
PS C:\> Get-OrchWebhook | Where-Object Enabled -eq $true | Disable-OrchWebhook -Confirm
```

すべての有効なWebhookを検索し、各操作に対して確認プロンプトを表示してそれらを無効にします。

### Example 6
```powershell
PS C:\> Disable-OrchWebhook CriticalEventsHook | Select-Object Name, Enabled, Url
```

Webhookを無効にし、名前、有効状態、URLを含む更新されたステータスを表示します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。複数のWebhookを無効にする場合や、操作が意図的であることを確認したい場合に有用です。

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

### -Name
無効にするWebhookの名前を指定します。パターンマッチングのためのワイルドカード文字（*と?）をサポートします。複数の名前が指定された場合、一致するすべてのWebhookが無効にされます。

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
ターゲットドライブの名前を指定します。指定されない場合、現在のドライブが対象になります。このパラメータを使用して、複数のOrchestrator環境のWebhookを同時に無効にします。

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

### -WhatIf
コマンドレットが実行された場合の動作を表示します。コマンドレットは実行されません。実際に操作を実行する前に、どのWebhookが無効になるかをプレビューするために使用します。

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

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況更新に対してPowerShellがどのように応答するかを指定します。

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

このコマンドレットはテナントエンティティを操作します。これは、特定のフォルダではなく、Orchestratorテナント全体のWebhookに影響することを意味します。

無効にされると、Webhookは即座に通知の送信を停止します。Webhookが無効化されている間に発生するすべてのイベントは、ターゲットURLに送信されません。

Webhookを無効にする前後でWebhookの現在の状態を確認するには、Get-OrchWebhookを使用してください。

特に複数のWebhookに一致する可能性があるワイルドカードパターンを使用する場合は、-WhatIfを使用して操作をプレビューすることを検討してください。

Webhookの無効化は可逆操作です。必要に応じてEnable-OrchWebhookを使用してWebhookを再有効化してください。

将来同じWebhook設定を再び使用する予定がある場合は、削除よりもWebhookの一時的な無効化が推奨されます。

## RELATED LINKS

[Get-OrchWebhook](Get-OrchWebhook.md)
[Enable-OrchWebhook](Enable-OrchWebhook.md)
[Remove-OrchWebhook](Remove-OrchWebhook.md)
[Copy-OrchWebhook](Copy-OrchWebhook.md)
