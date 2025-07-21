---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchRole

## SYNOPSIS
テナント間でロールをコピーします。

## SYNTAX

```
Copy-OrchRole -Name <String[]> [-Destination] <String[]> [-Path <String>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
現在のテナントから1つ以上の宛先テナントにロールをコピーします。このコマンドレットは、複数のUiPath Orchestrator環境間でロール構成のレプリケーションを可能にします。

カスタム（非静的）ロールのみをコピーできます。静的ロールは組み込みであり、すべてのテナントにすでに存在するため、複製できません。

このコマンドレットは、コピー操作中にロール権限、説明、その他の構成詳細を保持します。

主要エンドポイント: GET /odata/Roles, POST /odata/Roles

OAuth 必要なスコープ: OR.Users

必要な権限: Roles.View, Roles.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchRole CustomRole1 Orch2: -WhatIf
```

実際にコピーせずに、ロールをコピーする場合に何が起こるかを示します。

### Example 2
```powershell
PS Orch1:\> Copy-OrchRole CustomRole1 Orch2:
```

現在のテナントからOrch2にCustomRole1をコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-OrchRole TestRole Orch2:, Orch3: -Confirm
```

確認プロンプトを使用して、TestRoleを複数のテナントにコピーします。

### Example 4
```powershell
PS Orch1:\> Copy-OrchRole Custom* Orch2:
```

"Custom"で始まるすべてのロールをOrch2にコピーします。

### Example 5
```powershell
PS Orch1:\> Get-OrchRole | Where-Object {$_.IsStatic -eq $false} | Copy-OrchRole -Destination Orch2: -WhatIf
```

パイプライン入力を使用して、コピーされるカスタム（非静的）ロールを示します。

## PARAMETERS

### -Destination
ドライブ名で宛先テナントを指定します。複数の宛先にはカンマ区切りの値を使用します。

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
ドライブ名でソーステナントを指定します。指定しない場合、現在のテナントを使用します。

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
ロールをコピーする前に確認を求めます。複数のロールをコピーする場合に推奨されます。

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
実際にロールをコピーせずに、コマンドレットを実行した場合の動作を示します。安全性検証に推奨されます。

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
コピーするロールの名前を指定します。ワイルドカードと複数の値をサポートします。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Role
## NOTES
ロールエンティティはテナントスコープであり、このコマンドレットはテナント間レプリケーションを可能にします。

カスタムロール（IsStatic = $false）のみをコピーできます。静的ロールは組み込みであり、すべてのテナントにすでに存在します。

コピー操作は、権限、説明、メタデータを含むすべてのロール構成を保持します。

複数のロールにマッチする可能性のあるワイルドカードを使用する場合は、実行前に -WhatIf を使用してコピー操作をプレビューしてください。

ソースと宛先の両方のテナントで適切な権限を持っていることを確認してください。

## RELATED LINKS

[Get-OrchRole](Get-OrchRole.md)

[Set-OrchRole](Set-OrchRole.md)

[Remove-OrchRole](Remove-OrchRole.md)

[New-OrchRole](New-OrchRole.md)
