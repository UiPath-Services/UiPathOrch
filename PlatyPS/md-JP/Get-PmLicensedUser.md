---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmLicensedUser

## SYNOPSIS
UiPath Platform Managementからライセンスユーザーを取得します。

## SYNTAX

```
Get-PmLicensedUser [[-Name] <String[]>] [[-Email] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmLicensedUserコマンドレットは、UiPath Platform Managementからライセンスユーザーに関する情報を取得します。このコマンドレットは、UiPathライセンスが割り当てられたユーザーについて、連絡先情報、ライセンスタイプ、使用履歴を含む詳細を提供します。

返される各オブジェクトには、ユーザー識別情報、ライセンスバンドル情報、最終使用タイムスタンプ、および孤立ステータス（ユーザーアカウントがシステムにまだ存在するかどうかを示す）が含まれます。

このコマンドレットは、Platform Management APIにアクセスし、すべてのUiPath Orchestratorドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

プライマリ エンドポイント: GET /portal_/api/license/accountant/UserLicense/user/page

OAuth 必要スコープ: [PLACEHOLDER]

必要な権限: [PLACEHOLDER]

## EXAMPLES

### Example 1: すべてのライセンスユーザーを取得
```powershell
PS Orch1:\> Get-PmLicensedUser
```

Platform Managementシステム内のすべてのライセンスユーザーに関する情報を取得します。

### Example 2: メールアドレスでライセンスユーザーを取得
```powershell
PS Orch1:\> Get-PmLicensedUser user1@example.com, user2@example.com
```

メールアドレスで複数のユーザーのライセンス情報を取得します。

### Example 3: ライセンスの詳細を取得して構造を調べる
```powershell
PS Orch1:\> Get-PmLicensedUser | Select-Object -First 1 | ConvertTo-Json -Depth 5
```

最初のライセンスユーザーレコードを取得し、詳細な分析のために完全なオブジェクト構造をJSON形式で表示します。

### Example 4: 特定のドライブからライセンスユーザーを取得
```powershell
PS C:\> Get-PmLicensedUser -Path Orch1:, Orch2:
```

指定された複数のOrchestratorドライブからライセンスユーザー情報を取得します。

### Example 5: アクティブなライセンスユーザーをフィルタリング
```powershell
PS Orch1:\> Get-PmLicensedUser | Where-Object {$_.orphan -eq $false} | Select-Object displayName, email, userBundleLicenseNames, lastInUse
```

すべてのライセンスユーザーを取得し、孤立していない（アクティブな）ユーザーをフィルタリングして、ライセンスタイプを含む主要な情報を表示します。

### Example 6: ライセンスタイプ別にユーザーをグループ化
```powershell
PS Orch1:\> Get-PmLicensedUser | Where-Object {$_.orphan -eq $false} | ForEach-Object { $_.userBundleLicenseNames } | Group-Object | Sort-Object Count -Descending
```

アクティブなライセンスユーザーを取得し、ライセンスタイプ別にグループ化してライセンス配布を表示します。

## PARAMETERS

### -Email
取得するライセンスユーザーのメールアドレスを指定します。このパラメータは、メールフィールドに基づいてユーザーをフィルタリングします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
取得するライセンスユーザーの名前を指定します。このパラメータは、名前フィールドに基づいてユーザーをフィルタリングします。

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

### -Path
対象ドライブの名前を指定します。指定されていない場合、現在のドライブが対象となります。Platform Management APIは、すべてのOrchestratorドライブで動作します。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進捗更新にPowerShellがどのように応答するかを指定します。

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

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.NuLicensedUser
## NOTES
- このコマンドレットは、Platform Management APIにアクセスし、すべてのUiPathドライブタイプで動作します
- orphanプロパティは、ユーザーアカウントがシステムにまだ存在するかどうかを示します（false = アクティブ、true = 孤立）
- userBundleLicensesには、ライセンスコード（例："ATTUNU"、"RPADEVNU"）が含まれます
- userBundleLicenseNamesには、人間が読めるライセンス名（例："Attended - Named User"）が含まれます
- アクティブなユーザーアカウントと孤立したユーザーアカウントを区別するためにフィルタリングを使用します
- lastInUseタイムスタンプは、ユーザーが最後にUiPathサービスにアクセスした時刻を示します
- Platform Managementコマンドレット（"Pm"で始まる）は、組織横断的なライセンス情報を提供します

## RELATED LINKS

[Get-PmLicensedGroup]()

[Add-PmLicenseToPmLicensedGroup]()

[Remove-PmLicenseFromPmLicensedGroup]()

[Get-PmUser]()
