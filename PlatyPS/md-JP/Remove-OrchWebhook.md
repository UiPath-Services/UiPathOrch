---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchWebhook

## SYNOPSIS
Orchestratorからウェブフックを削除します。

## SYNTAX

```
Remove-OrchWebhook [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchWebhookコマンドレットは、Orchestrator環境からウェブフックを完全に削除します。ウェブフックは、Orchestratorで特定のイベントが発生したときに外部システムにHTTP通知を提供します。

ウェブフックが削除されると、URL、イベントタイプ、カスタムヘッダーを含むすべての構成が完全に削除されます。この操作は元に戻すことができないため、特に重要な統合に積極的に使用されているウェブフックを削除する前には、慎重に検討する必要があります。

このコマンドレットは、操作をプレビューする-WhatIfや削除前に確認を求める-Confirmなどの安全機能をサポートしています。重要な統合構成の誤った削除を避けるために、ウェブフックを削除する際にはこれらのパラメータを使用することを強く推奨します。

プライマリ エンドポイント: GET /odata/Webhooks, DELETE /odata/Webhooks({webhookId})

OAuth 必要なスコープ: OR.Webhooks

必要な権限: Webhooks.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-OrchWebhook TestWebhook
```

現在のOrchestrator環境から"TestWebhook"という名前のウェブフックを削除します。

### Example 2
```powershell
PS C:\> Remove-OrchWebhook TestWebhook1, TestWebhook2 -WhatIf
```

指定されたウェブフックが削除された場合に何が起こるかを実際に削除を実行せずに表示します。

### Example 3
```powershell
PS C:\> Remove-OrchWebhook Temp* -Confirm
```

名前が"Temp"で始まるすべてのウェブフックを削除し、各削除前に確認を求めます。

### Example 4
```powershell
PS C:\> Remove-OrchWebhook OldIntegration -Path Orch1:, Orch2:
```

複数のOrchestrator環境から"OldIntegration"という名前のウェブフックを削除します。

### Example 5
```powershell
PS C:\> Get-OrchWebhook | Where-Object {$_.Enabled -eq $false -and $_.Name -like "*test*"} | Remove-OrchWebhook -WhatIf
```

名前に"test"を含む無効なウェブフックを特定し、実際に削除を実行せずに削除対象を表示します。

### Example 6
```powershell
PS C:\> Remove-OrchWebhook UnusedWebhook -Confirm | Out-Null
```

確認プロンプトでウェブフックを削除し、出力を抑制します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。操作を元に戻すことができず、統合構成に影響するため、ウェブフックを削除する際には特に重要です。

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
削除するウェブフックの名前を指定します。パターンマッチング用のワイルドカード文字（*および?）をサポートします。複数の名前が指定された場合、一致するすべてのウェブフックが削除されます。

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
対象ドライブの名前を指定します。指定しない場合は、現在のドライブが対象となります。このパラメータを使用して、複数のOrchestrator環境から同時にウェブフックを削除します。

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
コマンドレットを実行した場合に何が起こるかを表示します。コマンドレットは実行されません。実際に操作を実行する前にどのウェブフックが削除されるかをプレビューするために、このパラメータを使用します。

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
このコマンドレットは共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

このコマンドレットはテナントエンティティに対して動作します。これは、特定のフォルダではなく、Orchestratorテナント全体のウェブフックに影響することを意味します。

ウェブフックを削除する前に、統合への影響をテストするために、まずDisable-OrchWebhookを使用してそれらを無効にすることを検討してください。これにより、削除が重要なビジネスプロセスを破綻させないことを確認できます。

将来の使用のためにウェブフック構成を保持する必要がある場合は、削除前にウェブフック設定を文書化するか、構成をエクスポートすることを検討してください。

複数のウェブフックに一致する可能性があるワイルドカードパターンを使用する場合は特に、実際の実行前に操作をプレビューするために-WhatIfパラメータを使用してください。

削除操作は元に戻すことができません。ウェブフックが削除されると、復元できないため、必要に応じてすべての構成で再作成する必要があります。

ウェブフックを削除すると、Orchestrator通知に依存する外部システムに影響を与える可能性があります。ウェブフックを削除する前に、依存システムが更新されているか、代替通知メカニズムがあることを確認してください。

## RELATED LINKS

[Get-OrchWebhook](Get-OrchWebhook.md)
[Enable-OrchWebhook](Enable-OrchWebhook.md)
[Disable-OrchWebhook](Disable-OrchWebhook.md)
[Copy-OrchWebhook](Copy-OrchWebhook.md)
