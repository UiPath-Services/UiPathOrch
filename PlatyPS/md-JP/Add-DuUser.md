---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-DuUser

## SYNOPSIS
Document Understanding プロジェクトにユーザーを追加します。

## SYNTAX

```
Add-DuUser [-Type] <String[]> [-Name] <String[]> [-Roles] <String[]> [-Path <String[]>] [-Recurse]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-DuUser コマンドレットは、UiPath Orchestrator フォルダー内の Document Understanding プロジェクトにユーザーを追加します。このコマンドレットは、Document Understanding 機能のユーザーアクセスとロール割り当てを管理し、ユーザーがドキュメント処理、検証、機械学習トレーニングワークフローに参加できるようにします。

Document Understanding プロジェクトでは、ドキュメント注釈、モデルトレーニング、検証などの異なるアクティビティに対して特定のユーザーロールが必要です。適切なロールでユーザーを追加することで、協調的なドキュメント処理が可能になり、人間のフィードバックを通じて機械学習モデルの精度が向上します。

-Type パラメーターでユーザータイプを指定し、-Name でユーザー名を指定し、-Roles で特定の Document Understanding ロールを割り当てます。-Path パラメーターで特定のフォルダーをターゲットにし、-Recurse でフォルダー階層全体の Document Understanding プロジェクトにユーザーを追加できます。

これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定してください。-Recurse パラメーターは、すべてのサブフォルダーの Document Understanding プロジェクトにユーザーを追加できるようにします。

プライマリエンドポイント: PATCH /{partitionGlobalId}/pap_/api/userroleassignments

OAuth 必須スコープ: [PLACEHOLDER - Document Understanding ユーザー管理スコープの確認が必要]

必要なアクセス許可: Document Understanding プロジェクト管理のアクセス許可

## EXAMPLES

### Example 1
```powershell
PS Orch1:\DocumentProcessing> Add-DuUser User john.doe Validator
```

現在のフォルダー（DocumentProcessing）の Document Understanding プロジェクトに、Validator ロールでユーザー john.doe を追加します。

### Example 2
```powershell
PS C:\> Add-DuUser -Path Orch1:\InvoiceProcessing User jane.smith "Data Entry", Reviewer
```

InvoiceProcessing フォルダーの Document Understanding プロジェクトに、"Data Entry" と Reviewer ロールでユーザー jane.smith を追加します。

### Example 3
```powershell
PS Orch1:\DocumentProcessing> Add-DuUser User admin.user, lead.user Administrator -WhatIf
```

現在のフォルダーの Document Understanding プロジェクトに Administrator ロールで admin.user と lead.user を追加する場合の動作を表示します。

### Example 4
```powershell
PS C:\> Add-DuUser -Path Orch1:\Reports User *analyst* Validator -Recurse
```

Reports フォルダーとそのすべてのサブフォルダーの Document Understanding プロジェクトに、ユーザー名に "analyst" を含むすべてのユーザーを Validator ロールで追加します。

### Example 5
```powershell
PS Orch1:\> Add-DuUser -Recurse User document.reviewer "Data Entry" -WhatIf
```

すべてのフォルダーを再帰的に検索して Document Understanding プロジェクトに "Data Entry" ロールで document.reviewer を追加する場合の動作を表示します。

### Example 6
```powershell
PS Orch1:\DocumentProcessing> Get-OrchUser | Where-Object {$_.Email -like "*@contoso.com"} | Add-DuUser -Type User -Roles Validator
```

contoso.com ドメインのメールアドレスを持つすべてのユーザーを取得し、パイプライン入力を使用して Validator ロールで Document Understanding プロジェクトに追加します。

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

### -Name
Document Understanding プロジェクトに追加するユーザーの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Document Understanding プロジェクトを含むターゲットフォルダーを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
すべてのサブフォルダーの Document Understanding プロジェクトに再帰的にユーザーを追加することを指定します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Roles
ユーザーに割り当てる Document Understanding ロールを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Type
Document Understanding アクセスのユーザータイプを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合の動作を表示します。
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
{{ ProgressAction の説明を入力 }}

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

### System.String[]
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定してください。

このコマンドレットは Document Understanding プロジェクトアクセスを管理します。一般的なロールには、Validator、Reviewer、Data Entry、Administrator があります。ユーザーはドキュメント注釈、検証、機械学習トレーニングワークフローに参加するために適切なロールが必要です。実際の実行前にテストするには -WhatIf を使用してください。

## RELATED LINKS

[Get-DuUser](Get-DuUser.md)

[Remove-DuUser](Remove-DuUser.md)

[Get-OrchUser](Get-OrchUser.md)
