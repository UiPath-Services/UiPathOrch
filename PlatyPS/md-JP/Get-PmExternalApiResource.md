---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmExternalApiResource

## SYNOPSIS
Platform Managementから外部APIリソースを取得します。

## SYNTAX

```
Get-PmExternalApiResource [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-PmExternalApiResource コマンドレットは、UiPath Platform Managementから外部APIリソース情報を取得します。外部APIリソースは、UiPathプラットフォーム内から外部システムやサービスへの接続を管理するために使用されます。

このコマンドレットは、設定されたAPIリソースの詳細情報を返し、外部システムとの統合を管理および監視するために役立ちます。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /api/externalApiResources

OAuth必須スコープ：[PLACEHOLDER - Platform Management API resource scopes]

必須権限：[PLACEHOLDER - Platform Management API resource permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-PmExternalApiResource
```

現在のインスタンスからすべての外部APIリソースを取得します。

### Example 2
```powershell
PS Orch1:\> Get-PmExternalApiResource *api*
```

ワイルドカードパターンマッチングを使用して、名前に"api"を含む外部APIリソースを取得します。

### Example 3
```powershell
PS C:\> Get-PmExternalApiResource -Path Orch1:, Orch2:
```

複数のインスタンスから外部APIリソースを取得します。

### Example 4
```powershell
PS Orch1:\> Get-PmExternalApiResource | ConvertTo-Json -Depth 2
```

すべての外部APIリソースを取得し、詳細な分析のために完全な構造をJSON形式で表示します。

## PARAMETERS

### -Name
取得する外部APIリソースの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべてのリソースが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All resources
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象のパスを指定します。指定しない場合、現在の場所が使用されます。このパラメーターは、複数のインスタンスを指定するためのパイプライン入力を受け入れます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### UiPath.PowerShell.Entities.ExternalResource
## NOTES
- このコマンドレットは、Platform Management環境で動作します
- 外部APIリソースは、UiPathプラットフォームと外部システム間の接続を管理します
- リソース情報には、接続詳細、認証設定、可用性ステータスが含まれます
- 外部APIリソースの管理には適切な権限が必要です

主要エンドポイント：GET /api/externalApiResources
OAuth必須スコープ：[PLACEHOLDER - Platform Management API resource scopes]
必須権限：[PLACEHOLDER - Platform Management API resource permissions]

## RELATED LINKS

[about_UiPathOrch](about_UiPathOrch.md)
