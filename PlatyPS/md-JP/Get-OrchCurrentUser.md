---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchCurrentUser

## SYNOPSIS
ターゲットドライブのテナントに接続中のユーザーを取得します。

## SYNTAX

```
Get-OrchCurrentUser [[-Path] <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchCurrentUser` コマンドレットは、指定されたOrchestratorドライブに対して現在認証されているユーザーに関する情報を取得します。これには、ID、ユーザー名、フルネーム、メールアドレス、ユーザータイプ、および割り当てられたロールなどのユーザー詳細情報が含まれます。

このコマンドレットは、非機密アプリケーション設定で接続されたドライブ（ユーザースコープ認証）に対してのみ使用できます。機密アプリケーションドライブ（アプリスコープ認証）では使用できません。

複数のOrchestratorインスタンスが異なる認証コンテキストで構成されている場合、現在のユーザーはドライブごとに異なる可能性があります。

-Pathパラメーターには、カンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値のオートコンプリートを使用できます。

主要エンドポイント: /odata/Users/UiPath.Server.Configuration.OData.GetCurrentUser

OAuth必要スコープ: OR.Users または OR.Users.Read

必要な権限: [プレースホルダー - 確認が必要]

## EXAMPLES

### Example 1
```powershell
PS C:\> cd Orch1:\
PS Orch1:\> Get-OrchCurrentUser
```

現在のOrchestratorドライブの現在のユーザー情報を取得します。

### Example 2
```powershell
PS C:\> Get-OrchCurrentUser -Path Orch1:,Orch2:
```

複数のOrchestratorドライブの現在のユーザー情報を取得して、認証コンテキストを比較します。

### Example 3
```powershell
PS Orch1:\> $user = Get-OrchCurrentUser
PS Orch1:\> Write-Host "現在のユーザー: $($user.FullName) ($($user.EmailAddress))"
PS Orch1:\> Write-Host "ロール: $($user.RolesList -join ', ')"
```

現在のユーザーを取得し、ユーザーとそのロールに関するフォーマットされた情報を表示します。

### Example 4
```powershell
PS Orch1:\> Get-OrchCurrentUser | Select-Object UserName, FullName, EmailAddress, Type, RolesList
```

現在のユーザーを取得し、主要なプロパティのみを表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchCurrentUser | Format-List
```

現在のユーザー情報を取得し、詳細なリスト形式で表示します。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットとなります。複数のドライブパスを指定して、異なるOrchestratorインスタンス間での現在のユーザーを確認できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: 現在のドライブ
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
Write-Progressコマンドレットによって生成される進行状況バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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

### UiPath.PowerShell.Entities.User
## NOTES
- このコマンドレットは、非機密アプリケーション接続（ユーザースコープ認証）でのみ動作します
- 機密アプリケーションドライブ（アプリスコープ認証）ではこの操作をサポートしません
- 現在のユーザーは、設定されたドライブごとに異なる可能性があります
- RolesListプロパティには、現在のユーザーに割り当てられたすべてのロールが含まれます
- Typeプロパティは、ユーザーアカウントタイプを示します（例：DirectoryUser）

主要エンドポイント: GET /odata/Users/UiPath.Server.Configuration.OData.GetCurrentUserExtended
OAuth必要スコープ: [プレースホルダー - 確認が必要]
必要な権限: [プレースホルダー - 確認が必要]

## RELATED LINKS

[Get-OrchUser](Get-OrchUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
