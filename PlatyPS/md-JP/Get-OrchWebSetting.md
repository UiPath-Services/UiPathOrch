---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchWebSetting

## SYNOPSIS
Web構成設定を取得します。

## SYNTAX

```
Get-OrchWebSetting [[-Key] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchWebSettingコマンドレットは、UiPath OrchestratorからWeb構成設定を取得します。Web設定は、Webインターフェース、ユーザーエクスペリエンス、セキュリティポリシー、およびシステム動作のさまざまな側面を制御します。

これはテナントエンティティコマンドレットです。-Pathパラメータは、ドライブ名（例：Orch1:、Orch2:）を使用して対象テナントを指定します。指定されていない場合、現在のテナントが対象となります。

Web設定には、認証構成、ユーザーインターフェース設定、セキュリティポリシー、機能切り替え、および統合設定が含まれます。これらの設定は、ユーザーがOrchestratorとどのように対話するかに影響し、システム全体の動作を制御します。

プライマリ エンドポイント: GET /odata/WebSettings

OAuth 必要スコープ: OR.Settings または OR.Settings.Read

必要な権限: Settings.View

## EXAMPLES

### Example 1
```powershell
Get-OrchWebSetting
```

現在のテナント内のすべてのWeb設定を取得します。

### Example 2
```powershell
Get-OrchWebSetting Authentication
```

"Authentication"という名前のWeb設定を取得します。

### Example 3
```powershell
Get-OrchWebSetting *Auth*
```

名前に"Auth"を含むすべてのWeb設定を取得します。

### Example 4
```powershell
Get-OrchWebSetting -Path Orch1:, Orch2: SessionTimeout
```

複数のテナントにわたって"SessionTimeout"Web設定を取得します。

### Example 5
```powershell
Get-OrchWebSetting | Where-Object {$_.Category -eq "Security"}
```

セキュリティカテゴリのすべてのWeb設定を取得します。

### Example 6
```powershell
Get-OrchWebSetting | Select-Object Name, Value, Category, Description | Format-Table
```

すべてのWeb設定を取得し、主要なプロパティで書式設定されたテーブルで表示します。

### Example 7
```powershell
Get-OrchWebSetting | Where-Object {$_.IsModified -eq $true} | ConvertTo-Json
```

デフォルト値から変更されたすべてのWeb設定を取得し、JSON形式でエクスポートします。

## PARAMETERS

### -Key
取得する設定のキーを指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ResponseDictionaryItem
## NOTES

プライマリ エンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetWebSettings
OAuth 必要スコープ: OR.Settings または OR.Settings.Read
必要な権限: Settings.View

## RELATED LINKS
