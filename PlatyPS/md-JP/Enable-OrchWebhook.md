---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchWebhook

## SYNOPSIS
ウェブフックを有効にします。

## SYNTAX

```
Enable-OrchWebhook [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchWebhookコマンドレットは、Orchestrator環境で以前に無効にされたウェブフックを有効にします。ウェブフックを使用すると、特定のイベントが発生したときにOrchestratorが外部システムにHTTP通知を送信でき、外部アプリケーションやサービスとのリアルタイム統合が可能になります。

ウェブフックが有効になると、監視するように設定されたイベントの通知を送信し始めます。これには、ジョブの完了、プロセスの展開、ロボットの状態変更、およびウェブフックの設定に基づくその他のOrchestratorアクティビティなどのイベントが含まれます。

このコマンドレットは、操作をプレビューする-WhatIfやウェブフックを有効にする前に確認を求める-Confirmなどの安全機能をサポートしています。

主要エンドポイント: PATCH /odata/Webhooks({webhookId})

OAuth必須スコープ: OR.Webhooks

必要な権限: Webhooks.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> Enable-OrchWebhook JobCompletionHook
```

"JobCompletionHook"という名前のウェブフックを有効にします。

### Example 2
```powershell
PS C:\> Enable-OrchWebhook IntegrationHook1, IntegrationHook2
```

コンマ区切りリストで名前を指定して、複数のウェブフックを有効にします。

### Example 3
```powershell
PS C:\> Enable-OrchWebhook Test* -WhatIf
```

名前が"Test"で始まるすべてのウェブフックを有効にした場合に何が起こるかを、実際に操作を実行せずに表示します。

### Example 4
```powershell
PS C:\> Enable-OrchWebhook ProductionHook -Path Orch1:, Orch2:
```

複数のOrchestrator環境で"ProductionHook"という名前のウェブフックを有効にします。

### Example 5
```powershell
PS C:\> Get-OrchWebhook | Where-Object Enabled -eq $false | Enable-OrchWebhook -Confirm
```

すべての無効なウェブフックを見つけ、各操作に対して確認プロンプトとともにそれらを有効にします。

### Example 6
```powershell
PS C:\> Enable-OrchWebhook CriticalEventsHook | Select-Object Name, Enabled, Url
```

ウェブフックを有効にし、名前、有効状態、URLを含む更新された状態を表示します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。複数のウェブフックを有効にする場合や、操作が意図的であることを確認したい場合に役立ちます。

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
有効にするウェブフックの名前を指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。複数の名前が指定された場合、一致するすべてのウェブフックが有効になります。

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
ターゲットドライブの名前を指定します。指定されていない場合、現在のドライブがターゲットになります。複数のOrchestrator環境で同時にウェブフックを有効にするには、このパラメーターを使用します。

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
コマンドレットが実行された場合に何が起こるかを表示します。コマンドレットは実行されません。実際に操作を実行する前に、どのウェブフックが有効になるかをプレビューするには、このパラメーターを使用します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Webhook
## NOTES

このコマンドレットはテナントエンティティを操作します。つまり、特定のフォルダーではなく、Orchestratorテナント全体のウェブフックに影響します。

有効になると、ウェブフックは設定されたイベントタイプとフィルターに基づいて、即座に通知の送信を開始します。ウェブフックを有効にする前に、ターゲットURLがアクセス可能で、ウェブフック通知を受信する準備ができていることを確認してください。

ウェブフックを有効にする前後の現在の状態を確認するには、Get-OrchWebhookを使用してください。

特に複数のウェブフックに一致する可能性があるワイルドカードパターンを使用する場合は、操作をプレビューするために-WhatIfの使用を検討してください。

ウェブフックのターゲットURLが無効またはアクセス不可能な場合、有効にすると通知試行の失敗や潜在的なパフォーマンスへの影響が生じる可能性があります。

## RELATED LINKS

[Get-OrchWebhook](Get-OrchWebhook.md)
[Disable-OrchWebhook](Disable-OrchWebhook.md)
[Remove-OrchWebhook](Remove-OrchWebhook.md)
[Copy-OrchWebhook](Copy-OrchWebhook.md)
