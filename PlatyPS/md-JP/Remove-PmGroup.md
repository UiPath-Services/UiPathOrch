---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-PmGroup

## SYNOPSIS
UiPath Platform Managementからグループを削除します。

## SYNTAX

```
Remove-PmGroup [-GroupName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-PmGroupコマンドレットは、UiPath Platform Managementからグループを削除します。グループは、ロールと権限を集合的に割り当てることができるユーザーのコレクションです。このコマンドレットは、指定されたグループとその関連する構成を完全に削除します。

-Pathパラメータは対象ドライブを指定します。指定しない場合は、現在のドライブが対象となります。

このコマンドレットはPlatform Management APIにアクセスし、すべてのUiPath Orchestratorドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

プライマリ エンドポイント: DELETE /api/Group/{tenantId}/{groupId}

OAuth 必要なスコープ: PM.Group

必要な権限: [PLACEHOLDER - Platform Managementグループ削除権限]

## EXAMPLES

### Example 1: 複数のグループを削除
```powershell
PS Orch1:\> Remove-PmGroup TestGroup1, TestGroup2, CustomGroup
```

単一の操作で複数のグループを削除します。

### Example 2: ワイルドカードを使用してグループを削除
```powershell
PS Orch1:\> Remove-PmGroup Test* -WhatIf
```

"Test"で始まるすべてのグループが削除された場合に何が起こるかを表示します。

### Example 3: 確認プロンプトで削除
```powershell
PS Orch1:\> Remove-PmGroup -GroupName CustomGroup -Confirm
```

実行前に追加の確認プロンプトでグループを削除します。

### Example 4: 特定のドライブからグループを削除
```powershell
PS C:\> Remove-PmGroup -Path Orch1:, Orch2: TestGroup
```

複数の指定されたOrchestratorドライブからグループを削除します。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。これにより、グループを削除する前の追加の安全チェックが提供されます。

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

### -GroupName
削除するグループの名前を指定します。パターンマッチング用のワイルドカード文字（*および?）をサポートします。複数のグループ名を配列として指定できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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
実際に削除操作を実行せずに、コマンドレットを実行した場合に何が起こるかを表示します。これは、変更を行う前にどのグループが影響を受けるかを確認するのに便利です。

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
- このコマンドレットはグループを完全に削除し、元に戻すことはできません
- 実行前に削除操作をプレビューするために-WhatIfパラメータを使用してください
- グループを削除すると、それに関連するユーザー、ロール、権限に影響します
- -GroupNameパラメータは一括操作用のワイルドカード文字をサポートします
- Platform Managementコマンドレット（"Pm"で始まる）は組織横断的なグループ管理を提供します
- 削除前に常にグループの依存関係とメンバー割り当てを確認してください
- 組み込みシステムグループ（"Everyone"など）は削除から保護されている場合があります

## RELATED LINKS

[Get-PmGroup]()

[New-PmGroup]()

[Copy-PmGroup]()

[Add-PmGroupMember]()

[Remove-PmGroupMember]()
