---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-PmUser

## SYNOPSIS
Platform Managementでユーザーを作成します。

## SYNTAX

```
New-PmUser [-Email] <String> [-Name <String>] [-SurName <String>] [-DisplayName <String>] [-Type <String>]
 [-BypassBasicAuthRestriction <String>] [-InvitationAccepted <String>] [-GroupName <String[]>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
New-PmUserコマンドレットは、UiPath Platform Managementでユーザーを作成します。Platform Managementレベルで作成されたユーザーは、組織全体のアクセス権を持ち、複数のテナントにわたって一元管理できます。

**これは組織エンティティコマンドレットです。** Platform Managementレベルで動作し、組織全体のユーザーを管理します。必要に応じて-Pathパラメータを使用してターゲットテナントを指定します。

Platform Managementを通じて作成されたユーザーは、UiPathOrch 0.9.13.0リリースノートで言及されているように、同じ組織内のテナント間で共有キャッシュの恩恵を受け、パフォーマンスの向上と一貫した動作を提供します。

プライマリ エンドポイント: POST /api/platformmanagement/users
OAuth 必要なスコープ: OR.Users または OR.Users.Write
必要な権限: Users.Create

## EXAMPLES

### Example 1
```powershell
New-PmUser user@company.com
```

位置パラメータを使用して指定されたメールアドレスで新しいユーザーを作成します。

### Example 2
```powershell
New-PmUser admin@company.com -Name "admin" -DisplayName "System Administrator" -SurName "Administrator" -Type "User"
```

名前とユーザータイプを含む完全なプロファイル情報でユーザーを作成します。

### Example 3
```powershell
New-PmUser developer@company.com -GroupName "Developers", "Automation Express" -InvitationAccepted "True"
```

ユーザーを作成し、招待が事前承認された状態で複数のグループに割り当てます。

### Example 4
```powershell
New-PmUser -Path Orch1: finance@company.com -DisplayName "Finance Manager" -BypassBasicAuthRestriction "True" -WhatIf
```

認証バイパスが有効な状態で特定のテナントにユーザーを作成する場合の結果を表示します。

## PARAMETERS

### -BypassBasicAuthRestriction
{{ Fill BypassBasicAuthRestriction Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
コマンドレットを実行する前に確認を求めます。

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

### -DisplayName
{{ Fill DisplayName Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Email
作成するユーザーのメールアドレスを指定します。これはプライマリ識別子として機能し、一意である必要があります。

```yaml
Type: String
Parameter Sets: (All)
Aliases: UserName

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -GroupName
作成時にユーザーを割り当てるグループを指定します。

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

### -InvitationAccepted
{{ Fill InvitationAccepted Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
ユーザーアカウントのユーザー名を指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットテナントへのパスを指定します。テナントコンテキストを指定するには、Orch1:、Orch2:などのドライブ名を使用します。

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

### -SurName
{{ Fill SurName Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Type
{{ Fill Type Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合の結果を表示します。コマンドレットは実行されません。

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

### System.String
### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmUser
## NOTES

## RELATED LINKS

[Get-PmUser](Get-PmUser.md)
[Update-PmUser](Update-PmUser.md)
[Remove-PmUser](Remove-PmUser.md)
[Copy-PmUser](Copy-PmUser.md)
