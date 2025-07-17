---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchWebhook

## SYNOPSIS
テナント間でwebhookをコピーします。

## SYNTAX

```
Copy-OrchWebhook [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchWebhook コマンドレットは、UiPath Orchestrator のソーステナントから宛先テナントにwebhookをコピーします。このコマンドレットは、エンドポイントURL、認証設定、イベントトリガー構成を含むwebhook構成のコピーを作成し、複数のテナント環境間でのwebhook管理を可能にします。

このコマンドレットは、複数の宛先テナントへの同時webhookコピーをサポートします。Webhookは Name パラメーターで識別でき、複数のwebhookを効率的にコピーするためのワイルドカードパターンをサポートします。

-Name パラメーターを使用してコピーするwebhookを指定し、-Destination パラメーターを使用してターゲットテナントを指定します。-Path パラメーターを使用して、特定のテナントコンテキスト内から操作していない場合に複数のソーステナントを操作できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:, Orch2:）を指定し、-Destination はwebhookをコピーするターゲットテナントドライブを指定します。

主要エンドポイント: GET /odata/Webhooks, POST /odata/Webhooks

OAuth 必要なスコープ: OR.Webhooks

必要な権限: Webhooks.View, Webhooks.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchWebhook ProcessCompletionHook Orch2:
```

現在のテナント（Orch1）からOrch2テナントにProcessCompletionHook webhookをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: AlertWebhook Orch2:, Orch3:
```

Orch1からOrch2とOrch3の両方のテナントにAlertWebhookをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-OrchWebhook NotificationHook, StatusHook Orch2: -WhatIf
```

現在のテナントからOrch2にNotificationHookとStatusHookをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: *Alert* Orch2:
```

ワイルドカードパターンを使用して、Orch1からOrch2にAlertが含まれる名前のすべてのwebhookをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-OrchWebhook *Notification* | Copy-OrchWebhook -Destination Orch2:, Orch3:
```

Notificationが含まれる名前のすべてのwebhookを取得し、パイプライン入力を使用してOrch2とOrch3の両方のテナントにコピーします。

### Example 6
```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: SlackIntegration Orch2: -Confirm
```

確認プロンプトでOrch1からOrch2にSlackIntegration webhookをコピーします。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。

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

### -Destination
webhookをコピーする宛先テナントドライブを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
コピーするwebhookの名前を指定します。

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
ソーステナントドライブを指定します。指定しない場合、現在のテナントがソースとして使用されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行した場合の動作を示します。
コマンドレットは実行されません。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Webhook
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先のテナントのドライブ名（例：Orch1:, Orch2:）を指定します。

Webhookには、エンドポイントURL、認証設定、イベントトリガー構成が含まれています。環境間でコピーする場合は、webhookエンドポイントが宛先テナントからアクセス可能であることを確認し、必要に応じて環境固有のURLを更新してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchWebhook](Get-OrchWebhook.md)

[Add-OrchWebhook](Add-OrchWebhook.md)

[Remove-OrchWebhook](Remove-OrchWebhook.md)

[Set-OrchWebhook](Set-OrchWebhook.md)
