---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-PmGroupMember

## SYNOPSIS
グループにメンバーを追加します。

## SYNTAX

```
Add-PmGroupMember [-GroupName] <String[]> [[-Type] <String[]>] [-UserName] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-PmGroupMember コマンドレットは、UiPath Platform Management 内のグループにユーザーを追加します。このコマンドレットは組織レベルでグループメンバーシップを管理し、さまざまなプラットフォーム機能やリソースへのアクセスを制御するグループにユーザーを割り当てることができます。

Platform Management のグループは、メンバーが継承できるアクセス権限とロールを定義します。ユーザーをグループに追加すると、組織全体でそれらのグループに関連付けられた権限と機能が付与されます。

メンバーを追加するグループを指定するには -GroupName パラメーターを使用し、追加するユーザーを指定するには -UserName パラメーターを使用します。-Type パラメーターを使用してユーザータイプでフィルタリングできます。-Path パラメーターを使用すると、複数のプラットフォームインスタンスで作業できます。

これはテナントエンティティコマンドレットです。-Path パラメーターは、複数の環境で作業する際に特定のプラットフォームインスタンスをターゲットにするためのドライブ名（例：Orch1:、Orch2:）を指定します。

主要エンドポイント: POST /api/Directory/BulkResolveByName/{tenantId}, GET /api/Group/{tenantId}/{groupId}, PUT /api/Group/{groupId}

OAuth 必要スコープ: PM.Group

必要なアクセス許可: 組織レベルでのグループ管理権限

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Add-PmGroupMember Administrators john.doe
```

現在のプラットフォームインスタンスで、ユーザー john.doe を Administrators グループに追加します。

### Example 2
```powershell
PS C:\> Add-PmGroupMember -Path Orch1:, Orch2: "Power Users" jane.smith
```

Orch1 と Orch2 の両方のプラットフォームインスタンスで、ユーザー jane.smith を "Power Users" グループに追加します。

### Example 3
```powershell
PS Orch1:\> Add-PmGroupMember Developers admin.user, lead.user -WhatIf
```

admin.user と lead.user を Developers グループに追加する際に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Add-PmGroupMember -GroupName "Business Users" -UserName *analyst* -Type User
```

ユーザー名に "analyst" を含むすべてのユーザーを "Business Users" グループに追加し、User タイプでフィルタリングします。

### Example 5
```powershell
PS Orch1:\> Get-PmUser | Where-Object {$_.Email -like "*@contoso.com"} | Add-PmGroupMember -GroupName Employees
```

contoso.com メールドメインを持つすべてのユーザーを取得し、パイプライン入力を使用して Employees グループに追加します。

### Example 6
```powershell
PS C:\> Add-PmGroupMember -Path Orch1: Support, "Help Desk" support.user -Confirm
```

確認プロンプト付きで support.user を Support と "Help Desk" の両方のグループに追加します。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認を求めます。

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
メンバーを追加するグループの名前を指定します。

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
対象とするプラットフォームインスタンスを指定します。

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

### -Type
メンバーを追加する際にフィルタリングするユーザータイプを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -UserName
グループに追加するユーザーのユーザー名を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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
これはテナントエンティティコマンドレットです。-Path パラメーターは、特定のプラットフォームインスタンスをターゲットにするためのドライブ名（例：Orch1:、Orch2:）を指定します。

このコマンドレットは Platform Management を通じて組織レベルで動作します。グループは、メンバーが継承する権限とアクセス権を定義します。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-PmGroup](Get-PmGroup.md)

[Remove-PmGroupMember](Remove-PmGroupMember.md)

[Get-PmUser](Get-PmUser.md)

[Get-PmGroupMember](Get-PmGroupMember.md)
