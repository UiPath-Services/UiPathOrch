---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-PmGroup

## SYNOPSIS
Platform Managementでグループを作成します。

## SYNTAX

```
New-PmGroup [-GroupName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
New-PmGroupコマンドレットは、UiPath Platform Managementでグループを作成します。グループは、ユーザーを整理し、複数のテナントにわたって組織レベルで権限を管理するために使用されます。

**これは組織エンティティコマンドレットです。** Platform Managementレベルで動作し、組織全体のグループを管理します。必要に応じて-Pathパラメータを使用してターゲットテナントを指定します。

Platform Managementを通じて作成されたグループは、テナント間で共有キャッシュに関するUiPathOrch 0.9.13.0リリースノートで言及されているように、同じ組織内の複数のテナントにまたがる一元化されたユーザーおよび権限管理機能を提供します。

プライマリ エンドポイント: POST /api/Group
OAuth 必要なスコープ: OR.Users または OR.Users.Write
必要な権限: Users.Create

## EXAMPLES

### Example 1
```powershell
New-PmGroup Administrators
```

位置パラメータを使用して"Administrators"という名前の新しいグループを作成します。

### Example 2
```powershell
New-PmGroup Developers, Testers, DevOps -WhatIf
```

位置パラメータを使用して複数のグループを作成する場合の結果を表示します。

### Example 3
```powershell
New-PmGroup -Path Orch1: "Finance Team" -Confirm
```

指定されたテナントコンテキストで確認プロンプト付きでグループを作成します。

## PARAMETERS

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

### -GroupName
作成するグループの名前を指定します。グループ名は組織内で一意である必要があります。

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

### System.String[]
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS

[Get-PmGroup](Get-PmGroup.md)
[Remove-PmGroup](Remove-PmGroup.md)
[Copy-PmGroup](Copy-PmGroup.md)
