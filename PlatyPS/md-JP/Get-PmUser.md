---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmUser

## SYNOPSIS
Platform Managementからユーザーを取得します。

## SYNTAX

```
Get-PmUser [[-Email] <String[]>] [-Path <String[]>] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmUserコマンドレットは、UiPath Platform Managementサービスからユーザー情報を取得します。このコマンドレットは、ユーザーアカウント、メールアドレス、表示名、グループメンバーシップ、およびアカウントステータスを含む、プラットフォーム組織全体のユーザーデータにアクセスします。

Platform Managementユーザーは、複数のテナントに割り当てることができ、さまざまなUiPathサービスにアクセスできる組織レベルのエンティティです。このコマンドレットは、グループメンバーシップ、アカウントステータス、およびユーザータイプを含む包括的なユーザー情報を提供します。

このコマンドレットは、ユーザーデータのCSV形式へのエクスポートをサポートしており、ユーザー管理、監査、および外部システムとの統合に役立ちます。

プライマリ エンドポイント: GET /api/User/users/{partitionGlobalId}

OAuth 必要スコープ: PM.User または PM.User.Read

必要な権限: Platform Management ユーザー読み取り権限

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PmUser
```

Platform Managementサービスからすべてのユーザーを取得します。

### Example 2
```powershell
PS C:\> Get-PmUser john.doe@company.com
```

メールアドレスが"john.doe@company.com"の特定のユーザーを取得します。

### Example 3
```powershell
PS C:\> Get-PmUser *@company.com
```

ワイルドカードマッチングを使用して、"company.com"ドメインに属するメールアドレスを持つすべてのユーザーを取得します。

### Example 4
```powershell
PS C:\> Get-PmUser -Path Orch1:, Orch2:
```

複数のOrchestrator環境（Orch1およびOrch2）からユーザーを取得します。

### Example 5
```powershell
PS C:\> Get-PmUser | Where-Object {$_.isActive -eq $true} | Select-Object userName, displayName, lastLoginTime
```

アクティブなユーザーのみを取得し、ユーザー名、表示名、および最終ログイン時刻を表示します。

### Example 6
```powershell
PS C:\> Get-PmUser | ConvertTo-Json -Depth 3
```

すべてのユーザーを取得し、詳細な分析のために完全な構造をJSON形式で表示します。

### Example 7
```powershell
PS C:\> Get-PmUser -ExportCsv users.csv
```

すべてのPlatform Managementユーザーを、ユーザー管理またはインポート操作に使用できるCSVファイルにエクスポートします。

## PARAMETERS

### -CsvEncoding
-ExportCsvを使用する際の、エクスポートされるCSVファイルの文字エンコーディングを指定します。指定されていない場合、デフォルトでBOM付きUTF-8が使用され、Excelやその他のアプリケーションでの適切な表示が保証されます。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -Email
取得するユーザーのメールアドレスを指定します。このパラメータは、"UserName"エイリアスも受け入れます。パターンマッチングのためのワイルドカード文字（*および?）をサポートし、メールドメインまたは部分的なメールアドレスでユーザーを検索できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: UserName

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ExportCsv
ユーザーデータをCSVファイルとしてエクスポートするパスを指定します。エクスポートされたCSVには、インポート操作に適した形式のユーザー情報が含まれ、Email、Name、SurName、DisplayName、Type、BypassBasicAuthRestriction、InvitationAccepted、GroupNameなどの列が含まれます。CSVはデフォルトでBOM付きUTF-8エンコーディングを使用します。

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

### -Path
対象ドライブの名前を指定します。指定されていない場合、現在のドライブが対象となります。Platform Managementコマンドは、組織レベルのデータにアクセスするため、すべてのドライブタイプ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

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
このコマンドの処理中に、スクリプト、コマンドレット、またはプロバイダーによって生成される進捗更新にPowerShellがどのように応答するかを指定します。

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

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmUser
## NOTES

このコマンドレットは、Platform Management APIにアクセスします。これらのAPIは、テナントレベルではなく組織レベルで動作するため、すべてのドライブタイプ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

-ExportCsvパラメータは、デフォルトでBOM付きUTF-8エンコーディングのCSVファイルを生成し、文字エンコーディングの問題なしにExcelやその他のアプリケーションとの互換性を保証します。

ユーザーオブジェクトの完全な構造を調べるにはConvertTo-Jsonを使用してください。デフォルトの表示形式では表示されない可能性があるgroupIDsなどのネストされた配列が含まれています。

ユーザー管理操作では、エクスポートされたCSVを変更して、対応するインポートまたは更新コマンドと一緒に使用できます。

多数のユーザーを扱う場合は、アクティブステータスやグループメンバーシップなどの特定の条件に基づいて結果をフィルタリングするためにWhere-Objectの使用を検討してください。

## RELATED LINKS

[New-PmUser](New-PmUser.md)
[Update-PmUser](Update-PmUser.md)
[Remove-PmUser](Remove-PmUser.md)
[Copy-PmUser](Copy-PmUser.md)
[Search-PmDirectory](Search-PmDirectory.md)
