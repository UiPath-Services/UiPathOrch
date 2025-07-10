---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUser

## SYNOPSIS
UiPath Orchestratorからユーザーを取得します。

## SYNTAX

```
Get-OrchUser [[-UserName] <String[]>] [[-FullName] <String[]>] [-Type <String[]>] [-ExpandDetails]
 [-Path <String[]>] [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorテナントからユーザー情報を取得します。これには、Orchestrator環境で認証および操作を行うことができる個別ユーザー、グループ、およびロボットアカウントが含まれます。

デフォルトでは、このコマンドレットは基本的なユーザー情報を効率的に提供するリストAPI エンドポイントを使用します。-ExpandDetailsパラメータが指定された場合、コマンドレットは個別のユーザー詳細APIを呼び出して、詳細な通知設定、セッション権限、およびロボットプロビジョニング設定を含む包括的な情報を取得します。

ユーザーは、テナント全体のスコープで動作するテナントエンティティです。-Pathパラメータを使用して、ドライブ名で対象テナントを指定します。

プライマリ エンドポイント: GET /odata/Users

OAuth 必要スコープ: OR.Users または OR.Users.Read

必要な権限: Users.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchUser
```

現在のテナントからすべてのユーザーを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchUser *admin*
```

ユーザー名に「admin」を含むユーザーを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchUser -Type DirectoryUser
```

ディレクトリユーザーのみを取得します（グループとロボットアカウントを除く）。

### Example 4
```powershell
PS Orch1:\> Get-OrchUser -ExpandDetails | Where-Object {$_.IsActive -eq $false}
```

拡張詳細を使用して非アクティブなユーザーを取得します。

### Example 5
```powershell
PS C:\> Get-OrchUser -Path Orch1:, Orch2:
```

複数のテナントからユーザーを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchUser -Type DirectoryUser -FullName John*
```

フルネームが「John」で始まるディレクトリユーザーを取得します。

### Example 7
```powershell
PS Orch1:\> Get-OrchUser administrators | ConvertTo-Json -Depth 2
```

詳細なユーザー情報を取得し、UserRolesなどのネストされたプロパティを含む完全な構造を表示します。

### Example 8
```powershell
PS Orch1:\> Get-OrchUser -ExportCsv C:\Reports\Users.csv
```

すべてのユーザーをUTF-8 BOMエンコーディングでCSVにエクスポートします。エクスポートされたCSVは、Import-Csv | Add-OrchUserまたはImport-Csv | Update-OrchUserを使用してインポートできます。

## PARAMETERS

### -FullName
フィルタリング対象のフルネームを指定します。ワイルドカードと複数の値をサポートします。

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

### -Path
ドライブ名で対象テナントを指定します。複数のテナントにはカンマ区切りの値を使用します。指定されていない場合、現在のテナントを対象とします。

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
コマンドレット実行中の進捗情報の表示方法を制御します。

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

### -UserName
取得するユーザー名を指定します。ワイルドカードと複数の値をサポートします。

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

### -ExpandDetails
ログインプロバイダー履歴、テナント詳細、およびアカウントIDを含む追加の詳細ユーザー情報を取得します。このパラメータを指定しない場合、LoginProviders配列、TenancyName、AccountIdなどのプロパティは空の値を返します。

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

### -CsvEncoding
CSVエクスポートのエンコーディングを指定します。デフォルトはExcel互換性のためのUTF-8 with BOMです。

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

### -ExportCsv
結果をUTF-8 BOMエンコーディングでCSVファイルにエクスポートします。内部IDを自動的に人間が読める名前に変換します。エクスポートされたCSVは、Import-Csvと一緒に使用してAdd-OrchUserにパイプし、一括操作を行うことができます。

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
フィルタリング対象のユーザータイプを指定します。有効な値には、DirectoryUser、DirectoryGroup、User、Robotが含まれます。

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
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメータをサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.User
## NOTES
ユーザーエンティティはテナントスコープであり、テナント全体で動作します。

ログインプロバイダー履歴、テナント詳細（TenancyName）、およびアカウントIDなどの追加ユーザー情報にアクセスする必要がある場合は、-ExpandDetailsを使用してください。ロール、通知設定、およびロボットプロビジョニングを含む基本ユーザー情報は-ExpandDetailsなしで利用できますが、LoginProviders配列やAccountIdなどの一部のプロパティには拡張が必要です。

-ExportCsvパラメータは、内部IDの代わりに人間が読める名前を使用して、インポート準備ができたCSVファイルを作成します。

ユーザータイプには、DirectoryUser（個別ユーザー）、DirectoryGroup（ユーザーグループ）、User（ローカルユーザー）、およびRobot（ロボットアカウント）が含まれます。



プライマリ エンドポイント: GET /odata/Users
OAuth 必要スコープ: OR.Users または OR.Users.Read
必要な権限: Users.View

## RELATED LINKS

[Add-OrchUser](Add-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)

[Remove-OrchUser](Remove-OrchUser.md)

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
