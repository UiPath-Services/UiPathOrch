---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmLicensedGroup

## SYNOPSIS
Platform Managementからライセンスグループを取得します。

## SYNTAX

```
Get-PmLicensedGroup [[-GroupName] <String[]>] [[-UserName] <String[]>] [-ExpandAllocation] [-Path <String[]>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmLicensedGroupコマンドレットは、UiPath Platform Managementからライセンスグループ情報を取得します。このコマンドレットは組織レベルで動作し、組織全体のユーザーグループのライセンス割り当てを管理します。

これは、Platform Management APIを呼び出す組織エンティティコマンドレットです。複数のテナントが同じ組織に属することができる組織レベルで動作します。ライセンスグループは、組織内のさまざまなユーザーグループにライセンスがどのように配布および割り当てられるかを定義します。

Platform Managementライセンスグループは、集中ライセンス管理を提供し、管理者が組織内のすべてのテナントでライセンスを割り当て、追跡、管理できるようにします。これにより、効率的なライセンス利用と適切なコンプライアンス管理が確保されます。

プライマリ エンドポイント: GET /api/licensedgroups

OAuth 必要スコープ: [PLACEHOLDER]

必要な権限: Administration.View

## EXAMPLES

### Example 1
```powershell
Get-PmLicensedGroup
```

現在の組織からすべてのライセンスグループを取得します。

### Example 2
```powershell
Get-PmLicensedGroup DeveloperGroup
```

"DeveloperGroup"という名前のライセンスグループを取得します。

### Example 3
```powershell
Get-PmLicensedGroup *Admin*
```

名前に"Admin"を含むすべてのライセンスグループを取得します。

### Example 4
```powershell
Get-PmLicensedGroup -Path Orch1:, Orch2:
```

複数のテナントドライブ経由でアクセスして、組織からライセンスグループを取得します。

### Example 5
```powershell
Get-PmLicensedGroup | Where-Object {$_.LicenseCount -gt 10}
```

10個以上のライセンスが割り当てられているすべてのライセンスグループを取得します。

### Example 6
```powershell
Get-PmLicensedGroup | Select-Object Name, LicenseType, LicenseCount, UsedLicenses | Format-Table
```

すべてのライセンスグループを取得し、ライセンス割り当て情報をテーブル形式で表示します。

### Example 7
```powershell
Get-PmLicensedGroup | Export-Csv "LicensedGroups.csv" -NoTypeInformation
```

すべてのライセンスグループを取得し、分析用に情報をCSVファイルにエクスポートします。

## PARAMETERS

### -CsvEncoding
{{ Fill CsvEncoding Description }}

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

### -ExpandAllocation
ライセンスが割り当てられたグループ内のユーザーを表示します。

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

### -ExportCsv
{{ Fill ExportCsv Description }}

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

### -GroupName
取得するグループ名を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象テナントドライブの名前を指定します。ライセンスグループデータは、使用するテナントドライブに関係なく組織全体のものです。

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
取得するユーザー名を指定します。これは、-ExpandAllocationが指定されている場合にのみ有効です。

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

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.NuLicensedGroup
### UiPath.PowerShell.Entities.NuLicensedGroupMember
## NOTES

## RELATED LINKS
