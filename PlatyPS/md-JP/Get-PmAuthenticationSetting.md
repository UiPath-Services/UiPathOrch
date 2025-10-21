---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmAuthenticationSetting

## SYNOPSIS
組織の認証設定を取得します。

## SYNTAX

```
Get-PmAuthenticationSetting [[-Path] <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Get-PmAuthenticationSetting` コマンドレットは、組織に構成されている外部IDプロバイダーとディレクトリ接続の設定を取得します。
これには、OAuth/OIDCプロバイダー（Azure ADなど）、SAML2プロバイダー、およびディレクトリサービスが含まれます。

このコマンドレットは、認証スキーム、クライアント資格情報、詳細な設定を含む `PmAuthenticationRoot` オブジェクトを返します。
`settingsExpanded` プロパティは、JSONの設定を自動的にPowerShellのHashtableに解析して、簡単にアクセスできるようにします。

主要エンドポイント: GET /api/AuthenticationSetting/getAll/{partitionGlobalId}

OAuth必要スコープ:

必要な権限:

## EXAMPLES

### Example 1: 認証設定を取得する
```powershell
PS Orch1:\> Get-PmAuthenticationSetting
```

このコマンドは、現在の組織の認証設定を取得します。

### Example 2: 展開された設定にアクセスする
```powershell
PS Orch1:\> $auth = Get-PmAuthenticationSetting
PS Orch1:\> $auth.externalIdentityProviderDto.displayName
aad

PS Orch1:\> $auth.settingsExpanded["ServiceCertificateUsage"]
SigningAndEncryption
```

この例は、IDプロバイダーの名前と、Hashtableを使用した展開された設定へのアクセス方法を示しています。

### Example 3: 認証が有効かどうかを確認する
```powershell
PS Orch1:\> $auth = Get-PmAuthenticationSetting
PS Orch1:\> if ($auth.externalIdentityProviderDto.isActive) {
>>     "認証が有効です: $($auth.externalIdentityProviderDto.displayName)"
>> }
認証が有効です: aad
```

この例は、外部認証が現在有効かどうかを確認する方法を示しています。

## PARAMETERS

### -Path
認証設定へのパスを指定します。デフォルトは現在のドライブ（組織）です。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: Current drive
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
コマンドレットによって生成される進行状況の更新に対して、PowerShellがどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.PmAuthenticationRoot
## NOTES
- `settingsExpanded` プロパティは、`externalIdentityProviderDto.settings` からJSONを解析することで自動的に設定されます
- JSON解析に失敗した場合、`settingsExpanded` は空のHashtableになります
- このコマンドレットは組織スコープで、単一の認証構成を返します

## RELATED LINKS
