---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchCredentialStore

## SYNOPSIS
Orchestratorから資格情報ストアを削除します。

## SYNTAX

```
Remove-OrchCredentialStore [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchCredentialStoreコマンドレットは、Orchestrator環境から資格情報ストアを永続的に削除します。資格情報ストアは、AWS Secrets Manager、Azure Key Vault、または組み込みのOrchestratorデータベースなど、オートメーションプロセス用の資格情報を安全に管理および提供する外部システムです。

プライマリ エンドポイント: DELETE /odata/CredentialStores({credentialStoreId})

OAuth 必要なスコープ: OR.Settings または OR.Settings.Write

必要な権限: Settings.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-OrchCredentialStore TestStore
```

現在のOrchestrator環境から"TestStore"という名前の資格情報ストアを削除します。

## PARAMETERS

### -Name
削除する資格情報ストアの名前を指定します。

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
Specifies the name of the target drives. If not specified, the current drive will be targeted.

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

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS

[Get-OrchCredentialStore](Get-OrchCredentialStore.md)
[Copy-OrchCredentialStore](Copy-OrchCredentialStore.md)
[Get-OrchAsset](Get-OrchAsset.md)
[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)
