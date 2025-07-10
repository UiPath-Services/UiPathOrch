---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchUser

## SYNOPSIS
テナント間でユーザーをコピーします。

## SYNTAX

```
Copy-OrchUser [-UserName] <String[]> [-FullName <String[]>] [-Type <String[]>] [-Destination] <String[]>
 [-Path <String>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchUser コマンドレットは、UiPath Orchestrator のソーステナントから宛先テナントにユーザーをコピーします。このコマンドレットは、構成やメタデータを含むユーザーアカウントのコピーを作成し、複数のテナント環境間でのユーザー管理を可能にします。

このコマンドレットは、複数の宛先テナントへの同時ユーザーコピーをサポートします。ユーザーは、UserName または FullName パラメーターのいずれかで識別でき、対象のユーザー管理操作のために Type でフィルタリングできます。

-UserName パラメーターを使用してコピーするユーザーを指定し、-Destination パラメーターを使用してターゲットテナントを指定します。-Type パラメーターを使用してユーザータイプでフィルタリングし、-Path パラメーターを使用して複数のソーステナントを操作できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:, Orch2:）を指定し、-Destination はユーザーをコピーするターゲットテナントドライブを指定します。

プライマリエンドポイント: GET /odata/Users, GET /api/DirectoryService/SearchForUsersAndGroups, POST /odata/Users

OAuth 必要なスコープ: OR.Users

必要な権限: Users.View, Users.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchUser john.doe Orch2:
```

現在のテナント（Orch1）からOrch2テナントにユーザーjohn.doeをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchUser -Path Orch1: jane.smith Orch2:, Orch3:
```

Orch1からOrch2とOrch3の両方のテナントにユーザーjane.smithをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-OrchUser admin.user, developer.user Orch2: -WhatIf
```

現在のテナントからOrch2にadmin.userとdeveloper.userをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchUser -Path Orch1: *admin* Orch2: -Type User
```

Orch1からOrch2にadminが含まれるすべてのユーザーをコピーし、Userタイプでフィルタリングします。

### Example 5
```powershell
PS Orch1:\> Get-OrchUser *developer* | Copy-OrchUser -Destination Orch2:, Orch3:
```

developerが含まれるユーザー名のすべてのユーザーを取得し、パイプライン入力を使用してOrch2とOrch3の両方のテナントにコピーします。

### Example 6
```powershell
PS C:\> Copy-OrchUser -Path Orch1: -FullName "John Smith" Orch2: -Confirm
```

フルネームが「John Smith」のユーザーをOrch1からOrch2に確認プロンプトでコピーします。

## PARAMETERS

### -Destination
ユーザーをコピーする宛先テナントドライブを指定します。

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

### -FullName
コピーするユーザーのフルネームを指定します。UserNameパラメーターの代替です。

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

### -Path
ソーステナントドライブを指定します。指定しない場合、現在のテナントがソースとして使用されます。

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

### -UserName
コピーするユーザーのユーザー名を指定します。

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

### -Type
ユーザーをコピーする際にフィルタリングするユーザータイプを指定します。

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

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先のテナントのドライブ名（例：Orch1:, Orch2:）を指定します。

ユーザーは、UserName または FullName のいずれかで識別できます。-Type パラメーターを使用して特定のユーザータイプでフィルタリングします。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchUser](Get-OrchUser.md)

[Add-OrchUser](Add-OrchUser.md)

[Remove-OrchUser](Remove-OrchUser.md)

[Set-OrchUser](Set-OrchUser.md)
