---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchFolderUser

## SYNOPSIS
ユーザーをフォルダーに割り当てます。

## SYNTAX

```
Add-OrchFolderUser -Type <String> [-UserName] <String[]> [[-Roles] <String[]>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-OrchFolderUser コマンドレットは、UiPath Orchestrator テナント内の特定のフォルダーに既存のユーザーを割り当てます。このコマンドレットはフォルダーエンティティで動作し、適切なフォルダーナビゲーションまたは -Path、-Recurse、または -Depth パラメーターを使用した明示的なフォルダー指定が必要です。

ユーザーをフォルダーに割り当てる前に、テナント内にユーザーが既に存在している必要があります。ユーザーの存在を確認するには Get-OrchUser を使用してください。このコマンドレットは、フォルダーレベルで特定のロールと権限を持つ複数のユーザーを複数のフォルダーに割り当てることをサポートしています。

フォルダーユーザーの割り当ては、どのユーザーが特定のフォルダーにアクセスできるかを制御し、それらのフォルダー内での権限を定義します。-Type パラメーターを使用してディレクトリエンティティのタイプ（DirectoryUser、DirectoryRobot、DirectoryGroup、または DirectoryExternalApplication）を指定し、-Roles パラメーターを使用してフォルダーレベルのロールを定義します。

これはフォルダーエンティティコマンドレットです。"Use Set-Location cmdlet (cd command) to navigate to the target folder first"というエラーが表示された場合は、対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。

主要エンドポイント: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignDomainUser

OAuth 必要スコープ: OR.Folders または OR.Folders.Write

必要なアクセス許可: (Units.Edit または SubFolders.Edit - 任意のフォルダーにドメインユーザーを割り当てる、またはユーザーが提供されたすべてのフォルダーで SubFolders.Edit 権限を持っている場合のみ)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Add-OrchFolderUser DirectoryUser john.doe@company.com "Folder Administrator"
```

ディレクトリユーザーを現在のフォルダーに Folder Administrator ロールで割り当てます。

### Example 2
```powershell
PS Orch1:\> Add-OrchFolderUser -Path .\Development DirectoryUser jane.smith@company.com "Automation Developer"
```

明示的なパスを使用して、ディレクトリユーザーを Development フォルダーに Automation Developer ロールで割り当てます。

### Example 3
```powershell
PS Orch1:\> Add-OrchFolderUser -Recurse DirectoryUser admin.user@company.com *Administrator*
```

ワイルドカードを使用して、すべての Administrator ロールで管理者ユーザーをすべてのフォルダーに再帰的に割り当てます。

### Example 4
```powershell
PS Orch1:\Production> Add-OrchFolderUser DirectoryUser developer1@company.com, developer2@company.com "Automation Developer", "Automation User"
```

複数のディレクトリユーザーを現在のフォルダーに複数のロールで割り当てます。

### Example 5
```powershell
PS Orch1:\> Get-OrchUser *developer* | Add-OrchFolderUser -Path .\Development DirectoryUser "Automation Developer"
```

"developer" を含むユーザーをパイプし、Development フォルダーに developer ロールで割り当てます。

### Example 6
```powershell
PS Orch1:\> Add-OrchFolderUser -Path .\QA, .\Staging -Depth 1 DirectoryGroup "QA Team" "Automation User" -WhatIf
```

安全のため -WhatIf を使用して、深度制限付きで QA と Staging フォルダーにディレクトリグループを割り当てる際の動作を表示します。

## PARAMETERS

### -Path
対象フォルダーを指定します。指定されていない場合は、現在のフォルダーが対象になります。

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

### -Roles
ユーザーに追加するロールを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: FolderRoles

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -UserName
割り当てるユーザーのユーザー名を指定します。

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

### -Depth
対象フォルダーへの再帰の深度を指定します。深度 0 は現在の場所のみを示し、サブフォルダーは含まれません。

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
操作が対象フォルダーとそのすべてのサブフォルダーを含むことを指定します。

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
割り当てるユーザーのタイプを指定します。有効な値は次のとおりです:
- DirectoryUser: Active Directory の個別ユーザー
- DirectoryRobot: Active Directory のロボットアカウント
- DirectoryGroup: Active Directory のグループ
- DirectoryExternalApplication: Active Directory の外部アプリケーション

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。Set-Location（cd コマンド）を使用してフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定する必要があります。

ユーザーをフォルダーに割り当てる前に、テナント内にユーザーが既に存在している必要があります。ユーザーの存在を確認するには Get-OrchUser を使用し、割り当てが正常に行われたことを確認するには Get-OrchFolderUser を使用してください。

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Remove-OrchFolderUser](Remove-OrchFolderUser.md)

[Get-OrchUser](Get-OrchUser.md)

[Get-OrchRole](Get-OrchRole.md)
