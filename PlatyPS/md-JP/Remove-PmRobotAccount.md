---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-PmRobotAccount

## SYNOPSIS
UiPath Platform Managementからロボットアカウントを削除します。

## SYNTAX

```
Remove-PmRobotAccount [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-PmRobotAccountコマンドレットは、UiPath Platform Managementからロボットアカウントを削除します。ロボットアカウントは、無人自動化シナリオに使用されるサービスアカウントです。このコマンドレットは、指定されたロボットアカウントとその関連する構成を完全に削除します。

-Pathパラメータは対象ドライブを指定します。指定しない場合は、現在のドライブが対象となります。

このコマンドレットはPlatform Management APIにアクセスし、すべてのUiPath Orchestratorドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

プライマリ エンドポイント: DELETE /api/RobotAccount/{tenantId}/{robotAccountId}

OAuth 必要なスコープ: PM.RobotAccount

必要な権限: [PLACEHOLDER - Platform Managementロボットアカウント削除権限]

## EXAMPLES

### Example 1: 複数のロボットアカウントを削除
```powershell
PS Orch1:\> Remove-PmRobotAccount RobotAccount1, RobotAccount2, RobotAccount3 -WhatIf
```

単一の操作で複数のロボットアカウントを削除します。

### Example 2: 確認プロンプトで削除
```powershell
PS Orch1:\> Remove-PmRobotAccount Robot* -Confirm
```

実行前に追加の確認プロンプトでロボットアカウントを削除します。

### Example 3: 特定のドライブからロボットアカウントを削除
```powershell
PS C:\> Remove-PmRobotAccount -Path Orch1:, Orch2: RobotAccount1
```

複数の指定されたOrchestratorドライブからロボットアカウントを削除します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。これにより、ロボットアカウントを削除する前の追加の安全チェックが提供されます。

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
削除するロボットアカウントの名前を指定します。複数の名前を配列として指定できます。このパラメータは必須で、Platform Managementに表示される通りのロボットアカウント名を受け入れます。

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

### -Path
対象ドライブの名前を指定します。指定しない場合は、現在のドライブが対象となります。Platform Management APIはすべてのOrchestratorドライブで動作します。

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
実際に削除操作を実行せずに、コマンドレットを実行した場合に何が起こるかを表示します。これは、変更を行う前にどのロボットアカウントが影響を受けるかを確認するのに便利です。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
## OUTPUTS

### System.Object
## NOTES
- このコマンドレットはロボットアカウントを完全に削除し、元に戻すことはできません
- 実行前に削除操作をプレビューするために-WhatIfパラメータを使用してください
- ロボットアカウントは無人自動化シナリオに使用されます
- ロボットアカウントを削除すると、それに依存する自動化プロセスに影響します
- Platform Managementコマンドレット（"Pm"で始まる）は組織横断的なロボットアカウント管理を提供します
- 自動化ワークフローの中断を避けるため、削除前に常にロボットアカウントの依存関係を確認してください

## RELATED LINKS

[Get-PmRobotAccount]()

[New-PmRobotAccount]()

[Set-PmRobotAccount]()

[Copy-PmRobotAccount]()
