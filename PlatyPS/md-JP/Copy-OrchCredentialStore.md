---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchCredentialStore

## SYNOPSIS
テナント間で認証情報ストアをコピーします。

## SYNTAX

```
Copy-OrchCredentialStore [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchCredentialStore コマンドレットは、UiPath Orchestrator 内のソーステナントから宛先テナントに認証情報ストアをコピーします。このコマンドレットは、設定とメタデータを含む認証情報ストアの完全なコピーを作成します。

このコマンドレットは、複数の宛先テナントに同時に認証情報ストアをコピーすることをサポートしています。認証情報ストアには、外部認証情報管理システムと認証プロバイダーのセキュリティ設定が含まれています。

-Name パラメーターを使用してコピーする認証情報ストアを指定し、-Destination パラメーターを使用してターゲットテナントを指定します。-Path パラメーターを使用すると、複数の Orchestrator インスタンスで作業する際にソーステナントを指定できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:、Orch2:）を指定し、-Destination は認証情報ストアをコピーするターゲットテナントドライブを指定します。

主要エンドポイント: GET /odata/CredentialStores, GET /odata/CredentialStores({id}), POST /odata/CredentialStores

OAuth 必要スコープ: OR.Settings

必要な権限: Settings.View, Settings.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchCredentialStore AzureKeyVault Orch2:
```

現在のテナント（Orch1）から Orch2 テナントに AzureKeyVault 認証情報ストアをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchCredentialStore -Path Orch1: CyberArkVault Orch2:, Orch3:
```

Orch1 から Orch2 と Orch3 の両方のテナントに CyberArkVault 認証情報ストアをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-OrchCredentialStore AzureKeyVault, HashiCorpVault Orch2: -WhatIf
```

現在のテナントから Orch2 に AzureKeyVault と HashiCorpVault 認証情報ストアをコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchCredentialStore -Path Orch1: *Vault* Orch2:
```

ワイルドカードを使用して、Orch1 から Orch2 に名前に Vault を含むすべての認証情報ストアをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-OrchCredentialStore *Azure* | Copy-OrchCredentialStore -Destination Orch2:, Orch3:
```

名前に Azure を含むすべての認証情報ストアを取得し、Orch2 と Orch3 の両方のテナントにコピーします。

### Example 6
```powershell
PS C:\> Copy-OrchCredentialStore -Path Orch1: ExternalVault Orch2: -Confirm
```

確認プロンプトを表示して、Orch1 から Orch2 に ExternalVault 認証情報ストアをコピーします。

## PARAMETERS

### -Destination
認証情報ストアをコピーする宛先テナントドライブを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
コピーする認証情報ストアの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソーステナントドライブを指定します。指定しない場合、現在のテナントがソースとして使用されます。

```yaml
Type: String
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

### -Confirm
コマンドレットの実行前に確認を求めます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。
コマンドレットは実行されません。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

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
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先テナントのドライブ名（例：Orch1:、Orch2:）を指定します。

効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。認証情報ストアには、外部認証情報管理システムのセキュリティ設定が含まれています。

## RELATED LINKS

[Get-OrchCredentialStore](Get-OrchCredentialStore.md)

[Set-OrchCredentialStore](Set-OrchCredentialStore.md)

[Remove-OrchCredentialStore](Remove-OrchCredentialStore.md)
