---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmExternalApplication

## SYNOPSIS
Platform Managementから外部アプリケーションを取得します。

## SYNTAX

```
Get-PmExternalApplication [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
`Get-PmExternalApplication`コマンドレットは、UiPath Platform Managementから外部アプリケーション情報を取得します。外部アプリケーションは、UiPathサービスとの認証および認可のために登録されたOAuthアプリケーションです。

このコマンドレットは、名前、ID、機密性設定を含む登録済みアプリケーションの詳細を返します。ワイルドカードを使用したアプリケーション名でのフィルタリングをサポートします。

これは任意のUiPathOrchドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作するPlatform Management APIコマンドレットです。

プライマリ エンドポイント: GET /api/ExternalClient/{partitionGlobalId}

OAuth 必要スコープ: PM.ExternalApplication または PM.ExternalApplication.Read

必要な権限: [PLACEHOLDER - External applications view permissions]

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1:\
PS Orch1:\> Get-PmExternalApplication
```

Platform Managementに登録されているすべての外部アプリケーションを取得します。

### Example 2
```powershell
PS Orch1:\> Get-PmExternalApplication -Name "*hoge*"
```

ワイルドカードパターンマッチングを使用して、名前に"hoge"を含む外部アプリケーションを取得します。

### Example 3
```powershell
PS Orch1:\> Get-PmExternalApplication | Where-Object isConfidential -eq $true
```

機密性を持つ外部アプリケーションのみを取得します。

### Example 4
```powershell
PS Orch1:\> Get-PmExternalApplication | Select-Object name, id, isConfidential | Format-Table
```

すべての外部アプリケーションを取得し、書式設定されたテーブルで表示します。

### Example 5
```powershell
PS Orch1:\> Get-PmExternalApplication -Name "uipathorch","MyBackgroundProcess"
```

名前で特定の外部アプリケーションを取得します。

## PARAMETERS

### -Name
取得する外部アプリケーションの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All applications
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象ドライブパスを指定します。このパラメータはパイプライン入力を受け入れ、ワイルドカードをサポートします。これはPlatform Management APIコマンドレットであるため、任意のUiPathOrchドライブで動作します。

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

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClient
## NOTES
- このコマンドレットはPlatform Management APIを使用し、任意のUiPathOrchドライブで動作します
- isConfidentialプロパティは、アプリケーションが機密または非機密として構成されているかどうかを示します
- 機密アプリケーションは通常、サーバー間認証に使用されます
- 非機密アプリケーションは、ユーザーインタラクティブ認証シナリオに使用されます

## RELATED LINKS

[New-PmExternalApplication](New-PmExternalApplication.md)
[Copy-PmExternalApplication](Copy-PmExternalApplication.md)
[Remove-PmExternalApplication](Remove-PmExternalApplication.md)
[about_UiPathOrch](about_UiPathOrch.md)
