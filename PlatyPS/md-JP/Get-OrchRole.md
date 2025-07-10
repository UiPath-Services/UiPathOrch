---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchRole

## SYNOPSIS
ロールを取得します。

## SYNTAX

```
Get-OrchRole [-Name <String[]>] [-Path <String[]>] [-ExpandPermission] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorテナントからロール情報を取得します。ロールはユーザーの権限とアクセス権を定義し、テナントレベルまたはフォルダレベルのいずれかになります。このコマンドレットはロールメタデータを取得し、詳細な権限情報を展開できます。

ロールはテナント全体のスコープで動作するテナントエンティティです。ドライブ名でターゲットテナントを指定するには-Pathパラメータを使用します。

プライマリエンドポイント: GET /odata/Roles?$expand=Permissions

OAuth必須スコープ: OR.Users または OR.Users.Read

必要な権限: Roles.View または Units.Edit または SubFolders.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchRole
```

現在のテナントからすべてのロールを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchRole *Admin*
```

名前に「Admin」を含むロールを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchRole -Path Orch1:, Orch2:
```

複数のテナントからロールを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchRole -ExpandPermission | Where-Object {$_.PermissionName -eq "Assets"}
```

展開された詳細を使用してAssets権限を持つロールを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchRole | Where-Object {$_.IsStatic -eq $false}
```

変更可能なカスタム（非静的）ロールを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchRole -ExportCsv C:\Reports\Roles.csv
```

UTF-8 BOMエンコーディングですべてのロールをCSVにエクスポートします。エクスポートされたCSVは、Import-Csv | Set-OrchRoleを使用してインポートできます。

## PARAMETERS

### -ExpandPermission
各ロールの詳細な権限情報を展開し、Assets.View、Processes.Editなどの個別の権限を表示します。

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
ドライブ名でターゲットテナントを指定します。複数のテナントにはカンマ区切りの値を使用します。指定されていない場合は、現在のテナントをターゲットにします。

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
コマンドレット実行中の進行状況情報の表示方法を制御します。

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

### -Name
取得するロールの名前を指定します。ワイルドカードと複数の値をサポートします。

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

### -CsvEncoding
CSVエクスポートのエンコーディングを指定します。デフォルトはExcel互換性のためのBOM付きUTF-8です。

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
BOM付きUTF-8エンコーディングで結果をCSVファイルにエクスポートします。内部IDを人間が読める名前に自動変換します。対応するImportコマンドレットと組み合わせて使用できます。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Role
### UiPath.PowerShell.Entities.OrchRolePermissionExpanded
## NOTES
ロールエンティティはテナントスコープです。テナント全体で動作し、フォルダ固有ではありません。

ロールはTenantまたはFolderタイプのいずれかになります。静的ロールは組み込みで変更できませんが、カスタムロールは編集できます。

詳細な権限分析が必要な場合や特定の権限でロールをフィルタリングしたい場合は、-ExpandPermissionを使用してください。

-ExportCsvパラメータは、内部IDの代わりに人間が読める名前を持つインポート対応CSVファイルを作成します。

プライマリエンドポイント: GET /odata/Roles
OAuth必須スコープ: OR.Users または OR.Users.Read
必要な権限: Roles.View

## RELATED LINKS

[Set-OrchRole](Set-OrchRole.md)

[Copy-OrchRole](Copy-OrchRole.md)

[Remove-OrchRole](Remove-OrchRole.md)

[Add-OrchRoleToFolderUser](Add-OrchRoleToFolderUser.md)
