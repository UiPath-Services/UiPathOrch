---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchRole

## SYNOPSIS
Orchestratorからロールを削除します。

## SYNTAX

```
Remove-OrchRole -Name <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorテナントからロールを完全に削除します。ロールは、組織内のユーザーおよびグループの権限とアクセスレベルを定義します。

現在ユーザーまたはグループに割り当てられていないカスタムロールのみ削除できます。静的（組み込み）ロールは、Orchestrator機能に不可欠であるため削除できません。

このコマンドレットは、操作をプレビューする-WhatIfや削除前に確認を求める-Confirmなどの安全機能をサポートしています。

プライマリ エンドポイント: GET /odata/Roles, DELETE /odata/Roles({roleId})

OAuth 必要なスコープ: OR.Users

必要な権限: Roles.Delete

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Remove-OrchRole CustomRole1 -WhatIf
```

実際に削除を実行せずに、ロールを削除する際に何が起こるかを表示します。

### Example 2
```powershell
PS Orch1:\> Remove-OrchRole Test* -Confirm
```

名前が"Test"で始まるロールを削除し、各削除前に確認を求めます。

### Example 3
```powershell
PS Orch1:\> Remove-OrchRole TemporaryRole -Path Orch1:, Orch2:
```

複数のテナントからロールを削除します。

### Example 4
```powershell
PS Orch1:\> Get-OrchRole | Where-Object {$_.IsStatic -eq $false} | Remove-OrchRole -WhatIf
```

パイプライン入力を使用して、どのカスタム（非静的）ロールが削除されるかを表示します。

## PARAMETERS

### -Path
ドライブ名でターゲットテナントを指定します。複数のテナントにはカンマ区切りの値を使用します。指定しない場合は、現在のテナントをターゲットにします。

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
コマンドレット実行中の進行状況情報の表示方法を制御します。

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
コマンドレットを実行する前に確認を求めます。破壊的操作に推奨されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
実際に操作を実行せずに、コマンドレットを実行した場合に何が起こるかを表示します。安全確認のために推奨されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
削除するロールの名前を指定します。ワイルドカードと複数の値をサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
このコマンドレットは共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
ロールエンティティはテナントスコープで、テナント全体で動作します。

ロールを削除する前に、ユーザーまたはグループに割り当てられていないことを確認してください。Get-OrchRoleを使用して割り当てを確認し、Remove-OrchRoleFromUserまたはRemove-OrchRoleFromFolderUserを使用して割り当てを削除します。

静的ロール（IsStatic = $true）は、Orchestrator機能に不可欠であるため削除できません。

削除操作は元に戻すことができません。実行前に操作をプレビューするために-WhatIfを使用してください。

## RELATED LINKS

[Get-OrchRole](Get-OrchRole.md)

[Set-OrchRole](Set-OrchRole.md)

[Copy-OrchRole](Copy-OrchRole.md)

[Remove-OrchRoleFromUser](Remove-OrchRoleFromUser.md)
