---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Move-PmGroupMember

## SYNOPSIS
Platform Managementグループ間でグループメンバーを移動します。

## SYNTAX

```
Move-PmGroupMember [-GroupName] <String> [-UserName] <String[]> [-Destination] <String[]>
 [-KeepSource <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Move-PmGroupMemberコマンドレットは、あるPlatform Managementグループから別のグループにメンバーを移動します。このコマンドレットは組織レベルで動作し、組織全体でグループメンバーシップを管理します。

これは、Platform Management APIを呼び出す組織エンティティコマンドレットです。組織レベルで動作し、複数のテナントが同じ組織に属することができます。グループメンバーシップは一元管理され、組織内のすべてのテナントのアクセスに影響します。

デフォルトでは、メンバーを移動すると、ソースグループから削除され、宛先グループに追加されます。-KeepSourceを使用して、宛先グループに追加しながら、元のグループのメンバーシップを維持します。

プライマリ エンドポイント: POST /api/Directory/BulkResolveByName/{tenantId}, GET /api/Group/{tenantId}/{groupId}, PUT /api/Group/{groupId}

OAuth 必要なスコープ: PM.Group

必要な権限: Administration.Edit

## EXAMPLES

### Example 1
```powershell
Move-PmGroupMember "Administrators" "MyRobot4" "Everyone" -WhatIf
```

MyRobot4をAdministratorsグループからEveryoneグループに移動する場合の結果を表示します。

### Example 2
```powershell
Move-PmGroupMember "Administrators" "MyRobot4" "Everyone"
```

MyRobot4をAdministratorsグループからEveryoneグループに移動します。

### Example 3
```powershell
Move-PmGroupMember "Developers" "john.doe", "jane.smith" "TeamLead"
```

複数のユーザー（john.doeとjane.smith）をDevelopersグループからTeamLeadグループに移動します。

### Example 4
```powershell
Move-PmGroupMember -GroupName "TestGroup" -UserName "*Robot*" -Destination "AutomationUsers"
```

名前に"Robot"を含むすべてのメンバーをTestGroupからAutomationUsersグループに移動します。

### Example 5
```powershell
Move-PmGroupMember "Administrators" "service.account" "AutomationUsers" -KeepSource true
```

service.accountをAutomationUsersに移動し、Administratorsグループのメンバーシップを維持します。

### Example 6
```powershell
Move-PmGroupMember -Path Orch1:, Orch2: "OldGroup" "migration.user" "NewGroup" -Confirm
```

migration.userをOldGroupからNewGroupに複数のテナント間で確認付きで移動します。

### Example 7
```powershell
Get-PmGroupMember | Where-Object {$_.PathGroupName -like "*Temporary*"} | Move-PmGroupMember -Destination "PermanentGroup"
```

"Temporary"を含むグループからすべてのメンバーをPermanentGroupに移動します。グループメンバーシップ情報は、ByPropertyNameバインディングを使用してパイプライン経由で渡されます。

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
メンバーが移動される宛先グループの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -GroupName
メンバーが移動されるソースグループの名前を指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -KeepSource
宛先に移動した後、ソースグループにメンバーを残すかどうかを指定します。二重メンバーシップを維持するには"true"に設定します。

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
ターゲットテナントドライブの名前を指定します。グループメンバーシップの変更は、使用されるテナントドライブに関係なく、組織全体に影響します。

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

### -UserName
グループ間で移動するユーザーまたはロボットアカウントの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行した場合の結果を表示します。
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

### System.String
### System.String[]
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
