---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchUser

## SYNOPSIS
ユーザーをテナントに追加します。

## SYNTAX

```
Add-OrchUser [[-Type] <String[]>] [-UserName] <String[]> [[-Roles] <String[]>] [-IsExternalLicensed <String>]
 [-MayHaveUserSession <String>] [-MayHaveRobotSession <String>] [-MayHaveUnattendedSession <String>]
 [-MayHavePersonalWorkspace <String>] [-RestrictToPersonalWorkspace <String>] [-UpdatePolicyType <String>]
 [-UpdatePolicyVersion <String>] [-UR_UserName <String>] [-UR_CredentialStore <String>] [-UR_Password <String>]
 [-UR_CredentialExternalName <String>] [-UR_CredentialType <String>] [-UR_LimitConcurrentExecution <String>]
 [-ES_TracingLevel <String>] [-ES_StudioNotifyServer <String>] [-ES_LoginToConsole <String>]
 [-ES_ResolutionWidth <Int32>] [-ES_ResolutionHeight <Int32>] [-ES_ResolutionDepth <Int32>]
 [-ES_FontSmoothing <String>] [-ES_AutoDownloadProcess <String>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-OrchUser コマンドレットは、組織の既存のユーザーを UiPath Orchestrator テナントに割り当てます。このコマンドレットは新しいユーザーを作成しません。代わりに、組織内に既に存在するユーザー、または組織やテナントと連携している Active Directory 内に存在するユーザーを割り当てます。

テナントに割り当てることができる組織ユーザーを取得するには Get-PmUser を使用します。連携している Active Directory ドメインからユーザーを検索するには Search-OrchDirectory を使用します。

割り当てるユーザーに対して、ロール、セッション権限、実行設定（ES_*）、無人ロボット資格情報（UR_*）など、さまざまなテナント固有の設定を指定できます。このコマンドレットでは、割り当てるユーザーのライセンス、ワークスペース制限、および更新ポリシーを設定できます。

複数のインスタンスで作業する場合は、-Path パラメーターを使用して特定の Orchestrator ドライブをターゲットにします。-UserName パラメーターは、既存の組織またはディレクトリユーザーのユーザー名を受け入れます。

主要エンドポイント: GET /odata/Users?$expand=OrganizationUnits,UserRoles, GET /odata/Roles?$expand=Permissions, GET /api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All, POST /odata/Users

OAuth 必要スコープ: OR.Users または OR.Users.Read または OR.Users.Write

必要なアクセス許可: (Users.View または Units.Edit または SubFolders.Edit), (Roles.View または Units.Edit または SubFolders.Edit)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser testuser1 -WhatIf
```

組織のユーザー testuser1 を現在のテナントに追加します。

### Example 2
```powershell
PS C:\> Add-OrchUser -Path Orch1:, Orch2: DirectoryUser testuser2 Developer -WhatIf
```

ユーザー testuser2 を Orch1 と Orch2 の両方のテナントに Developer ロールで追加します。

### Example 3
```powershell
PS Orch1:\> Add-OrchUser DirectoryUser testuser3 Administrator, Developer -MayHaveUserSession True -WhatIf
```

testuser3 を Administrator および Developer ロールで追加し、ユーザーに Orchestrator Web アクセスを有効にします。

### Example 4
```powershell
PS Orch1:\> Add-OrchUser DirectoryRobot RobotAccount1 -WhatIf
```

RobotAccount1 を DirectoryRobot ユーザータイプとして追加します。

### Example 5
```powershell
PS C:\> Import-Csv users.csv | Add-OrchUser -WhatIf
```

CSV ファイルから Orch1 テナントにユーザーを追加する際に何が起こるかを表示します。この CSV ファイルは Get-OrchUser -ExportCsv で生成できます。
最小限の CSV 形式:
Path,Type,UserName,Roles
Orch1:,DirectoryUser,john.doe,Developer
Orch1:,DirectoryUser,jane.smith,Administrator

### Example 6
```powershell
PS Orch1:\> Get-PmUser *@uipath.com | Add-OrchUser -Roles Developer -WhatIf
```

contoso.com メールドメインを持つすべての組織ユーザーを取得し、現在のテナントに Developer ロールで追加します。

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

### -Path
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。

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

### -Roles
ユーザーに追加するロールを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: TenantRoles

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Type
追加するユーザーのタイプを指定します。有効な値: DirectoryUser、DirectoryRobot、DirectoryGroup、DirectoryExternalApplication

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

### -UserName
追加するユーザーのユーザー名を指定します。

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

### -ES_AutoDownloadProcess
プロセスの自動ダウンロード設定を指定します。

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

### -ES_FontSmoothing
フォントスムージング設定を指定します。

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

### -ES_LoginToConsole
コンソールへのログイン設定を指定します。

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

### -ES_ResolutionDepth
画面解像度の色深度を指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ES_ResolutionHeight
画面解像度の高さを指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ES_ResolutionWidth
画面解像度の幅を指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ES_StudioNotifyServer
Studio サーバー通知設定を指定します。

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

### -ES_TracingLevel
トレースレベルを指定します。

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

### -MayHavePersonalWorkspace
ユーザーが個人ワークスペースを持つことができるかどうかを指定します。

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

### -MayHaveRobotSession
ユーザーがロボットセッションを持つことができるかどうかを指定します。

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

### -MayHaveUnattendedSession
ユーザーが無人セッションを持つことができるかどうかを指定します。

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

### -MayHaveUserSession
ユーザーがユーザーセッションを持つことができるかどうかを指定します。

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

### -RestrictToPersonalWorkspace
ユーザーを個人ワークスペースに制限するかどうかを指定します。

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

### -UpdatePolicyType
更新ポリシーのタイプを指定します。

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

### -UpdatePolicyVersion
更新ポリシーのバージョンを指定します。

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

### -UR_CredentialStore
無人ロボット用の資格情報ストアを指定します。

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

### -UR_CredentialType
無人ロボット用の資格情報タイプを指定します。

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

### -UR_LimitConcurrentExecution
無人ロボットの同時実行制限を指定します。

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

### -UR_Password
無人ロボット用のパスワードを指定します。

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

### -UR_UserName
無人ロボット用のユーザー名を指定します。

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

### -UR_CredentialExternalName
無人ロボット用の外部資格情報名を指定します。

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

### -IsExternalLicensed
ユーザーが外部ライセンスを持っているかどうかを指定します。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES

## RELATED LINKS
