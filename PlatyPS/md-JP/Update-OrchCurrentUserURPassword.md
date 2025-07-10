---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchCurrentUserURPassword

## SYNOPSIS
UiPath Orchestratorで現在のユーザーのパスワードを更新します。

## SYNTAX

```
Update-OrchCurrentUserURPassword [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Update-OrchCurrentUserURPassword コマンドレットは、UiPath Orchestratorで現在のユーザーのパスワードを更新します。パラメータなしで実行すると、コマンドレットは新しいパスワードと確認を対話的に入力するようユーザーに求めます。このコマンドレットは、Orchestrator認証パスワードを変更する必要があるユーザーに便利です。

このコマンドレットには、新しいパスワードが正しく入力されることを確認するための確認プロンプトが組み込まれています。

これは、現在認証されているユーザーの資格情報のみに影響するユーザー固有の操作です。

プライマリエンドポイント: [PLACEHOLDER - User password update endpoint]

OAuth必須スコープ: [PLACEHOLDER - User profile modification scope]

必要な権限: [PLACEHOLDER - User profile edit permissions]

## EXAMPLES

### Example 1: 現在のユーザーのパスワードを対話的に更新
```powershell
PS Orch1:\> Update-OrchCurrentUserURPassword
```

新しいパスワードと確認の入力をユーザーに求め、Orchestratorで現在のユーザーのパスワードを更新します。

### Example 2: パスワード更新操作をプレビュー
```powershell
PS Orch1:\> Update-OrchCurrentUserURPassword -WhatIf
```

実際に更新を実行せずに、パスワード更新操作を実行した場合に何が起こるかを表示します。

### Example 3: 複数のドライブでパスワードを更新
```powershell
PS C:\> Update-OrchCurrentUserURPassword -Path Orch1:, Orch2:
```

指定された複数のOrchestratorドライブで現在のユーザーのパスワードを更新します。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定されていない場合は、現在のドライブがターゲットになります。このパラメータにより、複数のOrchestratorインスタンス間でパスワードを更新できます。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### -Confirm
コマンドレットを実行する前に確認を求めます。これにより、パスワード入力プロンプトを超えた追加の確認ステップが追加されます。

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

### -WhatIf
実際にパスワード更新操作を実行せずに、コマンドレットを実行した場合に何が起こるかを表示します。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES
- このコマンドレットは、パラメータなしで実行されるとパスワード入力を対話的に要求します
- パスワードプロンプトには以下が含まれます：
  - Password: （新しいパスワード用）
  - Confirmation: （新しいパスワードの確認用）
- セキュリティのため、両方のパスワード入力はアスタリスク（*）でマスクされます
- このコマンドレットは、実行時に必須のパスワードと確認パラメータが必要です
- 変更を加えずに操作をプレビューするには-WhatIfを使用してください
- この操作は、現在認証されているユーザーのパスワードのみに影響します
- パスワードを変更する際は、Orchestratorインスタンスへの適切なネットワーク接続があることを確認してください

## RELATED LINKS

[Get-OrchCurrentUser]()

[Update-OrchUser]()

[Add-OrchUser]()
