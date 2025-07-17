---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchFolderUser

## SYNOPSIS
フォルダユーザー割り当てを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchFolderUser [-UserName] <String[]> [-Destination] <String> [-Type <String[]>] [-Path <String>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchFolderUser コマンドレットは、UiPath Orchestrator テナント内またはテナント間で、ソースフォルダから宛先フォルダにユーザー割り当てをコピーします。このコマンドレットは、ユーザー対フォルダの関係とその関連するロールを複製し、宛先フォルダで同じユーザーが同等のアクセス権を持つことを保証します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートしています。これは、異なる環境間で一貫したユーザーアクセスパターンを維持したり、同一の権限を持つ並列フォルダ構造を設定したりするのに役立ちます。

-UserName パラメーターを使用して割り当てをコピーするユーザーを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。-Type パラメーターを使用してユーザータイプでフィルタリングできます。このコマンドレットは、ユーザー自体ではなく、ユーザーの割り当てとその役割をコピーします。

これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからユーザー割り当てをコピーし、宛先でフォルダ構造を維持します。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={key},includeInherited={includeInherited}), GET /api/DirectoryService/SearchForUsersAndGroups, POST /odata/Folders/UiPath.Server.Configuration.OData.AssignDirectoryUser

OAuth 必要スコープ: OR.Folders

必要な権限: Units.View, SubFolders.View, Units.Edit, SubFolders.Edit, Assets.Create, Assets.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchFolderUser john.doe \Production
```

現在のフォルダ（Development）から同じテナント内の Production フォルダに john.doe のユーザー割り当てをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchFolderUser -Path Orch1:\Development jane.smith Orch2:\Production
```

テナント間ユーザー割り当てコピーを実証して、Orch1:\Development から Orch2:\Production に jane.smith のユーザー割り当てをコピーします。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchFolderUser admin.*, lead.* \Production -WhatIf
```

現在のフォルダから Production フォルダに admin.* または lead.* パターンに一致するユーザー名の複数のユーザー割り当てをコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchFolderUser -Path Orch1:\Development -Type DirectoryUser *manager* Orch2:\Production
```

User タイプでフィルタリングして、Orch1:\Development から Orch2:\Production にユーザー名に manager を含むすべてのユーザー割り当てをコピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchFolderUser -Recurse *admin* Orch2:\ -WhatIf
```

すべてのサブフォルダから admin を含むすべてのユーザー割り当てを再帰的に Orch2:\ にコピーする場合に何が起こるかを表示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchFolderUser *developer* | Copy-OrchFolderUser -Destination Orch2:\Production
```

ユーザー名に developer を含むすべてのフォルダユーザー割り当てを取得し、パイプライン入力を使用して Orch2:\Production にコピーします。

## PARAMETERS

### -Destination
ユーザー割り当てをコピーする宛先フォルダを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソースフォルダを指定します。指定しない場合、現在のフォルダがソースとして使用されます。

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
割り当てをコピーするユーザーのユーザー名を指定します。

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

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。
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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Recurse
ユーザー割り当てをすべてのサブフォルダから再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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

### -Type
割り当てをコピーする際にフィルタリングするユーザータイプを指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.UserRoles
## NOTES
これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

このコマンドレットは、ユーザー自体ではなく、ユーザーの割り当てとその役割をコピーします。テナント間でコピーする場合、ユーザーはターゲットテナントに既に存在している必要があります。特定のユーザータイプでフィルタリングするには -Type パラメーターを使用してください。

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Add-OrchRoleToFolderUser](Add-OrchRoleToFolderUser.md)

[Remove-OrchRoleFromFolderUser](Remove-OrchRoleFromFolderUser.md)
