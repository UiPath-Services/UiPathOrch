---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmExternalApplication

## SYNOPSIS
組織間で外部アプリケーションをコピーします。

## SYNTAX

```
Copy-PmExternalApplication [[-Name] <String[]>] [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-PmExternalApplication コマンドレットは、UiPath Process Mining のソース組織から宛先組織に外部アプリケーションをコピーします。このコマンドレットは、認証設定、権限、グループ関連付けを含む外部アプリケーション構成のコピーを作成し、複数の組織環境間での外部アプリケーション管理を可能にします。

このコマンドレットは、複数の宛先組織への同時外部アプリケーションコピーをサポートします。外部アプリケーションは Name パラメーターで識別でき、複数のアプリケーションを効率的にコピーするためのワイルドカードパターンをサポートします。

アプリケーションが所属するグループが宛先組織に存在しない場合、コピー操作中に自動的に作成され、完全なアプリケーション構成の転送が保証されます。

-Name パラメーターを使用してコピーする外部アプリケーションを指定し、-Destination パラメーターを使用してターゲット組織を指定します。-Path パラメーターを使用して、特定の組織コンテキスト内から操作していない場合に複数のソース組織を操作できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:, Orch2:）を指定し、-Destination は外部アプリケーションをコピーするターゲット組織ドライブを指定します。

プライマリエンドポイント: [PLACEHOLDER - requires verification of Platform Management External Application copy endpoint]

OAuth 必要なスコープ: [PLACEHOLDER - requires verification of Platform Management External Application scopes]

必要な権限: [PLACEHOLDER - requires verification of Platform Management External Application permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmExternalApplication DataConnector Orch2:
```

現在の組織（Orch1）からOrch2組織にDataConnector外部アプリケーションをコピーします。

### Example 2
```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: APIIntegration Orch2:, Orch3:
```

Orch1からOrch2とOrch3の両方の組織にAPIIntegration外部アプリケーションをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-PmExternalApplication ReportingApp, DashboardApp Orch2: -WhatIf
```

現在の組織からOrch2にReportingAppとDashboardAppをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: *Integration* Orch2:
```

ワイルドカードパターンを使用して、Orch1からOrch2にIntegrationが含まれる名前のすべての外部アプリケーションをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-PmExternalApplication *Analytics* | Copy-PmExternalApplication -Destination Orch2:, Orch3:
```

Analyticsが含まれる名前のすべての外部アプリケーションを取得し、パイプライン入力を使用してOrch2とOrch3の両方の組織にコピーします。

### Example 6
```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: Orch2: -Confirm
```

確認プロンプトでOrch1からOrch2にすべての外部アプリケーションをコピーします（-Nameが指定されない場合、すべてのアプリケーションがコピーされます）。

## PARAMETERS

### -Confirm
コマンドレットを実行する前に確認を求めます。

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

### -Destination
外部アプリケーションをコピーする宛先組織ドライブを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
コピーする外部アプリケーションの名前を指定します。指定しない場合、すべての外部アプリケーションがコピーされます。

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
ソース組織ドライブを指定します。指定しない場合、現在の組織がソースとして使用されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行した場合の動作を示します。
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
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### System.String[]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClientCreated
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先の組織のドライブ名（例：Orch1:, Orch2:）を指定します。

外部アプリケーションには、認証設定、権限、グループ関連付けが含まれています。関連するグループが宛先組織に存在しない場合、自動的に作成されます。環境間でコピーする場合は、外部アプリケーション構成が宛先環境に適切であることを確認してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-PmExternalApplication](Get-PmExternalApplication.md)

[New-PmExternalApplication](New-PmExternalApplication.md)

[Remove-PmExternalApplication](Remove-PmExternalApplication.md)

[Set-PmExternalApplication](Set-PmExternalApplication.md)
