---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchFolderUser

## SYNOPSIS
UiPath Orchestratorでフォルダーに割り当てられたユーザーとグループを取得します。

## SYNTAX

```
Get-OrchFolderUser [[-UserName] <String[]>] [[-FullName] <String[]>] [-Type <String[]>] [-IncludeInherited]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchFolderUser` コマンドレットは、UiPath Orchestrator内のフォルダーに割り当てられたユーザーとグループを取得します。このコマンドレットは、フォルダーレベルのアクセス制御を可視化し、特定のフォルダーにアクセス権を持つユーザーとグループ、およびそれらに割り当てられたロールと権限を表示します。

各フォルダーユーザーエントリには、ユーザーエンティティ（Type: DirectoryUserまたはDirectoryGroupを含む）、UserName、HasAlertsEnabledステータス、RobotType機能（Attended、Unattended）、および権限を持つ割り当てられたRolesに関する情報が含まれます。UserEntityには、FullName、AuthenticationSource、ロボット機能などの詳細が含まれます。

このコマンドレットはフォルダーエンティティ操作として動作し、適切なフォルダーコンテキストへの移動または-Pathパラメーターを使用したターゲットフォルダーの指定が必要です。サブフォルダーからユーザーを含めるには-Recurseパラメーターを、再帰レベルを制御するには-Depthを使用します。

主要エンドポイント: GET /odata/FolderUsers

OAuth必要スコープ: OR.Users または OR.Users.Read

必要な権限: Users.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchFolderUser
```

現在のSharedフォルダーに割り当てられたすべてのユーザーとグループを取得し、Id、UserEntity.Type、UserEntity.UserName、HasAlertsEnabled、RobotType、およびRolesを表示します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchFolderUser | ConvertTo-Json -Depth 3
```

完全なUserEntityの詳細とOriginおよびInheritedFromFolderプロパティを持つRole情報を含む、詳細なフォルダーユーザープロパティをJSON形式で表示します。

### Example 3
```powershell
PS C:\> Get-OrchFolderUser -Path Orch1:\Shared -UserName *admin*
```

Sharedフォルダー内で、ユーザー名に"admin"を含むすべてのフォルダーユーザーを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchFolderUser -Recurse -Type DirectoryUser
```

効率的なフィルタリングのために-Typeパラメーターを使用して、すべてのフォルダーにわたって割り当てられたすべての個人ユーザー（グループではない）を取得します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchFolderUser | ConvertTo-Json -Depth 3
```

完全なUserEntityの詳細とOriginおよびInheritedFromFolderプロパティを持つRole情報を含む、詳細なフォルダーユーザープロパティをJSON形式で表示します。

### Example 6
```powershell
PS Orch1:\> Get-OrchFolderUser -Recurse | Group-Object {$_.UserEntity.Type} | Select-Object Name, Count, @{Name="UserNames";Expression={($_.Group.UserEntity.UserName | Sort-Object -Unique) -join ", "}}
```

フォルダーユーザーをエンティティタイプ別にグループ化し、各タイプの実際のユーザー名とユーザー数を表示して、ユーザー分布の明確な概要を提供します。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深度を指定します。深度0は現在の場所のみを示します。より高い値はより多くのサブフォルダーレベルを含みます。

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
取得するフォルダーユーザーのフルネームを指定します。

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

### -IncludeInherited
親フォルダーから継承されたユーザーも取得することを指定します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
検索するターゲットフォルダーを指定します。指定しない場合は、現在のフォルダーコンテキストが使用されます。パス指定が必要なフォルダーエンティティ操作用です。

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

### -Recurse
検索操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。包括的なフォルダーユーザー発見に不可欠です。

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

### -UserName
フォルダーユーザーをフィルタリングするユーザー名を指定します。柔軟なユーザー選択のためにワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。

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

### -CsvEncoding
-ExportCsvを使用する場合にエクスポートされるCSVファイルのエンコーディングを指定します。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
取得したフォルダーユーザーを指定されたパスのCSVファイルにエクスポートします。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
取得するユーザーエンティティのタイプを指定します。有効な値には、DirectoryUserとDirectoryGroupがあります。

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

### UiPath.PowerShell.Entities.UserRoles
## NOTES
このコマンドレットは、フォルダーコンテキストへの移動または-Pathパラメーターを使用したパス指定が必要なフォルダーエンティティ操作です。このコマンドレットは、個人ユーザー（DirectoryUser）とグループ（DirectoryGroup）の両方をロール割り当てとともに表示するフォルダーレベルのアクセス制御を明らかにします。UserEntity.Typeはユーザーとグループを区別します。Rolesには、権限が直接"Assigned"されているか継承されているかを示すOrigin情報が含まれます。この操作には、ターゲットフォルダーでのUsers.View権限が必要です。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder
OAuth必要スコープ: OR.Users または OR.Users.Read
必要な権限: Users.View

## RELATED LINKS

[Add-OrchFolderUser](Add-OrchFolderUser.md)

[Remove-OrchFolderUser](Remove-OrchFolderUser.md)

[Set-OrchFolderUser](Set-OrchFolderUser.md)

[Get-OrchUser](Get-OrchUser.md)
