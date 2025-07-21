---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchCredentialStore

## SYNOPSIS
UiPath Orchestrator で構成された資格情報ストアを取得します。

## SYNTAX

```
Get-OrchCredentialStore [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchCredentialStore コマンドレットは、UiPath Orchestrator で構成された資格情報ストアを取得します。資格情報ストアは、自動化プロセスで使用される資格情報の安全な保存と管理を提供する外部システムです。

資格情報ストアは、Azure Key Vault、HashiCorp Vault、CyberArk などの外部セキュリティシステムとの統合を可能にし、パスワード、API キー、その他の機密情報の一元的な管理を提供します。各ストアには、Name、Description、Type、ExternalName、Id などのプロパティが含まれます。

資格情報ストアはテナントレベルエンティティで、テナント全体のスコープで動作します。-Path パラメーターを使用して、ドライブ名（例：Orch1:、Orch2:）でターゲットテナントを指定できます。

このコマンドレットはテナントレベル操作として動作し、指定された Orchestrator 環境から構成された資格情報ストアを取得します。これらのストアは自動化プロセスで安全な資格情報管理を提供するために使用されます。

主要エンドポイント: GET /odata/CredentialStores

OAuth 必須スコープ: OR.Administration または OR.Administration.Read

必要なアクセス許可: CredentialStores.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchCredentialStore
```

現在のテナントからすべての資格情報ストアを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchCredentialStore -Name "*Vault*"
```

名前に "Vault" を含む資格情報ストアを取得します。

### Example 3
```powershell
PS C:\> Get-OrchCredentialStore -Path Orch1:, Orch2:
```

複数のテナント（Orch1 と Orch2）から資格情報ストアを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchCredentialStore | Select-Object Path, Name, Type, ExternalName
```

すべての資格情報ストアを取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchCredentialStore | Where-Object Type -eq "AzureKeyVault"
```

Azure Key Vault タイプの資格情報ストアのみを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchCredentialStore | ConvertTo-Json -Depth 2
```

すべての資格情報ストア情報を JSON 形式で詳細表示します。

## PARAMETERS

### -Name
取得する資格情報ストアの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのストアが返されます。

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
ターゲットテナントドライブを指定します。指定されていない場合、現在のテナントがターゲットになります。テナントレベル操作用。

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
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.CredentialStore
## NOTES

このコマンドレットはテナントレベルエンティティ操作として動作し、組織全体の資格情報ストア構成にアクセスします。資格情報ストアは、自動化プロセスでの安全な資格情報管理の重要な構成要素です。

サポートされる資格情報ストアタイプには、Azure Key Vault、HashiCorp Vault、CyberArk、その他の外部セキュリティシステムが含まれます。各ストアは、そのタイプと構成に固有のプロパティとオプションを持ちます。

資格情報ストアの構成と管理には、適切なセキュリティアクセス許可と外部システムとの統合設定が必要です。

主要エンドポイント: GET /odata/CredentialStores
OAuth 必須スコープ: OR.Administration または OR.Administration.Read
必要なアクセス許可: CredentialStores.View

## RELATED LINKS

[Copy-OrchCredentialStore](Copy-OrchCredentialStore.md)

[Remove-OrchCredentialStore](Remove-OrchCredentialStore.md)

[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)

[Get-OrchAsset](Get-OrchAsset.md)
