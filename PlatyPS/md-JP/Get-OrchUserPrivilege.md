---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUserPrivilege

## SYNOPSIS
権限の統合モデルに基づいてユーザー権限を取得します。

## SYNTAX

```
Get-OrchUserPrivilege [[-UserName] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchUserPrivilegeコマンドレットは、UiPath Orchestratorから包括的なユーザー権限情報を取得します。このコマンドレットは、Orchestrator環境全体のユーザー権限、ロール割り当て、アクセスレベル、およびセッション権限に関する詳細な洞察を提供します。

これはテナントエンティティコマンドレットです。-Pathパラメータは、ドライブ名（例：Orch1:、Orch2:）を使用して対象テナントを指定します。指定されていない場合、現在のテナントが対象となります。

このコマンドレットは、明示的および継承されたロール、アクセス権限、有人セッション権限、個人ワークスペース権限、および更新ポリシーを含む広範な権限情報を返します。各権限カテゴリは、明示的な割り当て、グループメンバーシップからの継承権限、およびユーザーに適用される有効（最終）権限を示します。

この情報は、ユーザーの完全な権限状況を理解し、アクセス問題のトラブルシューティング、ユーザー権限の監査、および適切なセキュリティ構成の確保に不可欠です。

プライマリ エンドポイント: GET /api/Users/GetPrivileges?userId={userId}

OAuth 必要スコープ: OR.Users または OR.Users.Read

必要な権限: Users.View

## EXAMPLES

### Example 1
```powershell
Get-OrchUserPrivilege
```

現在のテナント内のすべてのユーザーの権限情報を取得します。

### Example 2
```powershell
Get-OrchUserPrivilege john.doe
```

現在のテナント内のユーザー"john.doe"の権限情報を取得します。

### Example 3
```powershell
Get-OrchUserPrivilege *admin*
```

名前に"admin"を含むすべてのユーザーの権限情報を取得します。

### Example 4
```powershell
Get-OrchUserPrivilege -Path Orch1:, Orch2: administrator
```

複数のテナントにわたって"administrator"ユーザーの権限情報を取得します。

### Example 5
```powershell
Get-OrchUserPrivilege | Where-Object {$_.ExplicitRoles.Count -gt 0}
```

明示的なロール割り当てを持つすべてのユーザーを取得します（グループから継承されただけでなく）。

### Example 6
```powershell
Get-OrchUserPrivilege | Select-Object UserName, AccessLevel, @{Name="TotalRoles"; Expression={$_.EffectiveRoles.Count}}
```

すべてのユーザー権限を取得し、ユーザー名、アクセスレベル、および有効ロール総数を表示します。

### Example 7
```powershell
Get-OrchUser admin* | Get-OrchUserPrivilege | ConvertTo-Json -Depth 5
```

パイプライン経由で管理者ユーザーの権限情報を取得し、詳細な権限構造をJSONにエクスポートします。

## PARAMETERS

### -Path
ドライブ名を使用して対象テナントの名前を指定します。指定されていない場合、現在のテナントが対象となります。

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
権限情報を取得するユーザー名を指定します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.UserPrivilege
## NOTES

プライマリ エンドポイント: GET /api/Users/GetPrivileges
OAuth 必要スコープ: OR.Users または OR.Users.Read
必要な権限: Users.View

## RELATED LINKS
