---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchRoleToFolderUser

## SYNOPSIS
フォルダーユーザーにロールを割り当てます。

## SYNTAX

```
Add-OrchRoleToFolderUser [[-UserName] <String[]>] [-FullName <String[]>] [-Roles] <String[]> [-Type <String[]>]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Add-OrchRoleToFolderUser コマンドレットは、UiPath Orchestrator テナント内の特定のフォルダー内のユーザーにロールを割り当てます。このコマンドレットは、ユーザーにそれらのフォルダー内でのアクセス権と機能を決定する特定のロールを付与することにより、フォルダーレベルの権限を管理します。

このコマンドレットは、ユーザーの識別に UserName または FullName のいずれかを使用してユーザーにロールを割り当てることをサポートします。複数のユーザーに複数のロールを同時に割り当て、特定のフォルダーをターゲットにしたり、フォルダー階層に再帰的に変更を適用したりできます。

割り当てるロールを指定するには -Roles パラメーターを使用し、対象ユーザーを識別するには -UserName または -FullName を使用します。-Path パラメーターは対象フォルダーを指定し、-Recurse パラメーターはすべてのサブフォルダーにロール割り当てを適用します。

これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダーでユーザーにロールを割り当て、フォルダー構造全体で一貫した権限を維持できます。

プライマリエンドポイント: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignUsers

OAuth 必要スコープ: OR.Folders

必要なアクセス許可: (Units.Edit または SubFolders.Edit - 任意のフォルダーにユーザーを割り当てる、またはユーザーが提供されたすべてのフォルダーで SubFolders.Edit 権限を持っている場合)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Add-OrchRoleToFolderUser -UserName john.doe -Roles Developer
```

現在のフォルダー（Development）でユーザー john.doe に Developer ロールを割り当てます。

### Example 2
```powershell
PS C:\> Add-OrchRoleToFolderUser -Path Orch1:\Finance -UserName jane.smith -Roles "Business Analyst", Viewer
```

Finance フォルダーでユーザー jane.smith に Business Analyst および Viewer ロールを割り当てます。

### Example 3
```powershell
PS Orch1:\Production> Add-OrchRoleToFolderUser -FullName "Alex Johnson" -Roles Administrator -WhatIf
```

Production フォルダーでユーザー Alex Johnson に Administrator ロールを割り当てる際に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Add-OrchRoleToFolderUser -Path Orch1:\Development, Orch1:\Testing -UserName team.lead -Roles "Team Lead"
```

Development および Testing フォルダーの両方でユーザー team.lead に Team Lead ロールを割り当てます。

### Example 5
```powershell
PS Orch1:\> Add-OrchRoleToFolderUser -Recurse -UserName support.user -Roles Viewer
```

現在の場所からすべてのフォルダーに再帰的に support.user に Viewer ロールを割り当てます。

### Example 6
```powershell
PS Orch1:\Finance> Get-OrchUser *analyst* | Add-OrchRoleToFolderUser -Roles "Business Analyst"
```

名前に analyst を含むすべてのユーザーを取得し、現在の Finance フォルダーで Business Analyst ロールを割り当てます。

### Example 7
```powershell
PS C:\> Add-OrchRoleToFolderUser -Path Orch1:\Development -Type DirectoryUser contractor.* -Roles Developer
```

Development フォルダーで User タイプでフィルタリングして、contractor.* パターンに一致するすべてのユーザーに Developer ロールを割り当てます。

## PARAMETERS

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダーレベルの最大数を指定します。

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

### -FullName
ロールを割り当てるユーザーのフルネームを指定します。UserName パラメーターの代替です。

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
ロール割り当てを行う対象フォルダーを指定します。

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

### -Recurse
指定されたパスからすべてのサブフォルダーに再帰的にロール割り当てを適用することを指定します。

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

### -Roles
ユーザーに割り当てるロールを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: FolderRoles

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UserName
ロールを割り当てるユーザーのユーザー名を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
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

### -Type
ロールを割り当てる際にフィルタリングするユーザータイプを指定します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。

ユーザーを識別するには -UserName または -FullName のいずれかを使用します。複数のロールを同時に割り当てることができます。ロール割り当てを実行する前に変更をプレビューするには -WhatIf を使用します。

## RELATED LINKS

[Get-OrchUser](Get-OrchUser.md)

[Get-OrchRole](Get-OrchRole.md)

[Remove-OrchRoleFromFolderUser](Remove-OrchRoleFromFolderUser.md)

[Get-OrchFolderUser](Get-OrchFolderUser.md)
