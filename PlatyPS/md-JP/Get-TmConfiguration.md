---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmConfiguration

## SYNOPSIS
Test Managerの構成設定を取得します。

## SYNTAX

```
Get-TmConfiguration [[-Path] <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-TmConfiguration`コマンドレットは、UiPath Test Managerの構成設定を取得します。これには、エンドポイント構成、基本設定、アイデンティティサーバー構成、ストレージ構成、検索構成、プラットフォーム構成、およびAzure DevOps、Jira、ServiceNowなどのさまざまなサードパーティ統合が含まれます。

構成オブジェクトには、Test Managerが外部システムと統合し、テスト自動化ワークフローを管理するためにどのように構成されているかについての詳細な情報が含まれています。

-Pathパラメータに対しては、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

プライマリ エンドポイント: [PLACEHOLDER - Test Manager configuration API endpoint]

OAuth 必要スコープ: [PLACEHOLDER - Test Manager configuration scopes]

必要な権限: [PLACEHOLDER - Test Manager configuration permissions]

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1Tm:
PS Orch1Tm:\> Get-TmConfiguration
```

現在のTest Managerインスタンスの完全なTest Manager構成を取得します。

### Example 2
```powershell
PS Orch1Tm:\> $config = Get-TmConfiguration
PS Orch1Tm:\> $config.basicConfiguration
```

Test Manager構成を取得し、基本構成設定のみを表示します。

### Example 3
```powershell
PS Orch1Tm:\> Get-TmConfiguration | Select-Object endpoints, platformConfiguration
```

Test Manager構成を取得し、エンドポイントとプラットフォーム構成プロパティのみを表示します。

## PARAMETERS

### -Path
Test Manager構成へのパスを指定します。このパラメータはパイプライン入力を受け入れ、ワイルドカードをサポートします。指定されていない場合、現在の場所の構成が取得されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
Write-Progressコマンドレットによって生成される進捗バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進捗更新にPowerShellがどのように応答するかを指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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

### UiPath.PowerShell.Entities.TmConfig
## NOTES
- このコマンドレットは、Test Managerインスタンスへのアクセスが必要です
- 構成オブジェクトには、Azure DevOps、Jira（Server、Cloud Basic Auth、Cloud OAuth）、ServiceNow、WebHook、XRay、Redmine、qTest、およびSAPを含むさまざまな統合の設定が含まれています
- 対応する統合が構成されていない場合、一部の構成プロパティはnullの可能性があります

## RELATED LINKS

[Get-TmServerInfo](Get-TmServerInfo.md)
[Get-TmProjectSetting](Get-TmProjectSetting.md)
[about_UiPathOrch](about_UiPathOrch.md)
