---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUserSession

## SYNOPSIS
ユーザーセッション情報を取得します。

## SYNTAX

```
Get-OrchUserSession [-State <String[]>] [-Type <String[]>] [-OrderBy <String[]>] [-Skip <UInt64>]
 [-First <UInt64>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchUserSessionコマンドレットは、UiPath Orchestratorからアクティブおよび履歴のユーザーセッション情報を取得します。ユーザーセッションは、ユーザーとOrchestrator間のインタラクティブな接続を表し、ログイン時刻、セッション持続時間、および接続詳細が含まれます。

これはテナントエンティティコマンドレットです。-Pathパラメータは、ドライブ名（例：Orch1:、Orch2:）を使用して対象テナントを指定します。指定されていない場合、現在のテナントが対象となります。

セッション情報は、ユーザーアクティビティの監視、セキュリティ監査、ライセンス使用状況の追跡、および接続問題のトラブルシューティングに役立ちます。このコマンドレットは、ユーザーアクセスパターンとシステム利用状況に関する洞察を提供します。

プライマリ エンドポイント: GET /odata/UserSessions

OAuth 必要スコープ: OR.Users または OR.Users.Read

必要な権限: Users.View

## EXAMPLES

### Example 1
```powershell
Get-OrchUserSession
```

現在のテナント内のすべてのユーザーのセッション情報を取得します。

### Example 2
```powershell
Get-OrchUserSession john.doe
```

ユーザー"john.doe"のセッション情報を取得します。

### Example 3
```powershell
Get-OrchUserSession *admin*
```

名前に"admin"を含むすべてのユーザーのセッション情報を取得します。

### Example 4
```powershell
Get-OrchUserSession -Path Orch1:, Orch2: developer
```

複数のテナントにわたって"developer"ユーザーのセッション情報を取得します。

### Example 5
```powershell
Get-OrchUserSession | Where-Object {$_.IsActive -eq $true}
```

現在アクティブなすべてのユーザーセッションを取得します。

### Example 6
```powershell
Get-OrchUserSession | Select-Object UserName, LoginTime, LastActivity, IsActive, SessionDuration
```

すべてのユーザーセッションを取得し、主要なタイミングおよびステータス情報を表示します。

### Example 7
```powershell
Get-OrchUser | Get-OrchUserSession | Where-Object {$_.LastActivity -gt (Get-Date).AddHours(-1)}
```

過去1時間以内にアクティブだったすべてのユーザーのセッション情報を取得します。ユーザー情報はByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

### -OrderBy
取得するセッションのソート対象項目を指定します。

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

### -State
取得するセッションの状態を指定します。

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

### -Type
取得するセッションのタイプを指定します。

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

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。スキップするオブジェクトの数を入力してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
指定された数のオブジェクトのみを取得します。取得するオブジェクトの数を入力してください。

```yaml
Type: UInt64
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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Session
## NOTES

プライマリ エンドポイント: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetGlobalSessions
OAuth 必要スコープ: OR.Robots または OR.Robots.Read
必要な権限: Robots.View

## RELATED LINKS
