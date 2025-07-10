---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmRobotAccount

## SYNOPSIS
Platform Managementからロボットアカウントを取得します。

## SYNTAX

### Default (Default)
```
Get-PmRobotAccount [[-Name] <String[]>] [-Path <String[]>] [-ExpandGroup] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

### ExportCsv
```
Get-PmRobotAccount [-Path <String[]>] [-ExportCsv <String>] [[-CsvEncoding] <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmRobotAccountコマンドレットは、UiPath Platform Managementからロボットアカウント情報を取得します。このコマンドレットは組織レベルで動作し、組織全体のロボットサービスアカウントを管理します。

これは、Platform Management APIを呼び出す組織エンティティコマンドレットです。複数のテナントが同じ組織に属することができる組織レベルで動作します。ロボットアカウントは、組織内のすべてのテナントにわたって自動化プロセスとロボット認証に使用されるサービスアカウントです。

Platform Managementロボットアカウントは、自動化シナリオ向けの集中ID管理を提供し、組織全体で安全で一貫したロボット認証を可能にします。これらのアカウントは、パフォーマンスの向上とメモリ使用量の削減のため、同じ組織内のテナント間で共有されます。

プライマリ エンドポイント: GET /api/robotaccounts

OAuth 必要スコープ: [PLACEHOLDER]

必要な権限: Administration.View

## EXAMPLES

### Example 1
```powershell
Get-PmRobotAccount
```

現在の組織からすべてのロボットアカウントを取得します。

### Example 2
```powershell
Get-PmRobotAccount ServiceAccount01
```

「ServiceAccount01」という名前のロボットアカウントを取得します。

### Example 3
```powershell
Get-PmRobotAccount *Automation*
```

名前に「Automation」を含むすべてのロボットアカウントを取得します。

### Example 4
```powershell
Get-PmRobotAccount -Path Orch1:, Orch2:
```

複数のテナントドライブ経由でアクセスして、組織からロボットアカウントを取得します。

### Example 5
```powershell
Get-PmRobotAccount | Where-Object {$_.IsActive -eq $true}
```

すべてのアクティブなロボットアカウントを取得します。

### Example 6
```powershell
Get-PmRobotAccount | Select-Object Name, EmailAddress, IsActive, CreationTime | Format-Table
```

すべてのロボットアカウントを取得し、主要なプロパティをテーブル形式で表示します。

### Example 7
```powershell
Get-PmRobotAccount | Where-Object {$_.LastLoginDate -lt (Get-Date).AddDays(-30)} | Select-Object Name, LastLoginDate
```

過去30日以内にログインしていないロボットアカウントを取得します。未使用のアカウントを特定するのに役立ちます。

## PARAMETERS

### -CsvEncoding
{{ Fill CsvEncoding Description }}

```yaml
Type: Encoding
Parameter Sets: ExportCsv
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExpandGroup
{{ Fill ExpandGroup Description }}

```yaml
Type: SwitchParameter
Parameter Sets: Default
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
Parameter Sets: ExportCsv
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
取得するロボットアカウントの名前を指定します。

```yaml
Type: String[]
Parameter Sets: Default
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
対象テナントドライブの名前を指定します。ロボットアカウントデータは、使用するテナントドライブに関係なく組織全体のものです。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメータをサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmRobotAccount
### UiPath.PowerShell.Entities.PmRobotAccountExpanded
## NOTES

## RELATED LINKS
