---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmUser

## SYNOPSIS
組織のユーザーを組織間でコピーします。

## SYNTAX

```
Copy-PmUser [-Email] <String[]> [-Destination] <String[]> [-Path <String>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-PmUser コマンドレットは、UiPath Platform Management を使用して、ソース組織から宛先組織にPlatform Managementユーザーをコピーします。このコマンドレットは、権限、設定、グループ関連付けを含むユーザー構成のコピーを作成し、複数のUiPath組織間でのユーザー管理を可能にします。

このコマンドレットは、複数の宛先組織に同時にユーザーをコピーすることをサポートします。ユーザーは Email パラメーター（UserName エイリアスも使用可能）で識別でき、コマンドレットは複数のユーザーを効率的にコピーするためのワイルドカードパターンをサポートしています。

ユーザーが所属するグループが宛先組織に存在しない場合、コピー操作中に自動的に作成され、完全なユーザー構成の転送が保証されます。

-Email パラメーターを使用してコピーするユーザーを指定し、-Destination パラメーターを使用してターゲット組織を指定します。-Path パラメーターを使用すると、特定の組織コンテキスト内で操作していない場合に、複数のソース組織で作業できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:、Orch2:）を指定し、-Destination はユーザーをコピーする宛先組織ドライブを指定します。

主要エンドポイント: POST /api/User/BulkCreate

OAuth 必要なスコープ: PM.User

必要な権限: 

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmUser john.doe@company.com Orch2:
```

現在の組織（Orch1）からOrch2組織にユーザーjohn.doe@company.comをコピーします。

### Example 2
```powershell
PS C:\> Copy-PmUser -Path Orch1: admin@company.com Orch2:, Orch3:
```

Orch1からOrch2とOrch3の両方の組織にユーザーadmin@company.comをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-PmUser analyst@company.com, viewer@company.com Orch2: -WhatIf
```

現在の組織からOrch2にanalyst@company.comとviewer@company.comをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-PmUser -Path Orch1: *admin* Orch2:
```

ワイルドカードパターンを使用して、Orch1からOrch2にadminを含むメールアドレスのすべてのユーザーをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-PmUser *manager* | Copy-PmUser -Destination Orch2:, Orch3:
```

managerを含むメールアドレスのすべてのユーザーを取得し、パイプライン入力を使用してOrch2とOrch3の両方の組織にコピーします。

### Example 6
```powershell
PS C:\> Copy-PmUser -Path Orch1: developer@company.com Orch2: -Confirm
```

確認プロンプトを表示して、Orch1からOrch2にユーザーdeveloper@company.comをコピーします。

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

### -Destination
ユーザーをコピーする宛先組織ドライブを指定します。

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

### -Email
コピーするユーザーのメールアドレスを指定します。このパラメーターには互換性のためのUserNameエイリアスがあります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: UserName

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソース組織ドライブを指定します。指定しない場合、現在の組織がソースとして使用されます。

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
コマンドレットを実行した場合の動作を示します。
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
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.PmUser
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースおよび宛先組織のドライブ名（例：Orch1:、Orch2:）を指定します。

ユーザーには、権限、設定、グループ関連付けが含まれます。関連するグループが宛先組織に存在しない場合、自動的に作成されます。環境間でコピーする際は、ユーザー構成が宛先環境に適していることを確認してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-PmUser](Get-PmUser.md)

[New-PmUser](New-PmUser.md)

[Remove-PmUser](Remove-PmUser.md)

[Set-PmUser](Set-PmUser.md)
