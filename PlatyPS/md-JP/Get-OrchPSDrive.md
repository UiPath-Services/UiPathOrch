---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPSDrive

## SYNOPSIS
構成されたすべてのUiPath Orchestratorドライブとその接続ステータスをリストします。

## SYNTAX

```
Get-OrchPSDrive [-Path <String[]>] [-Force] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchPSDrive コマンドレットは、構成されたすべてのUiPath Orchestrator PowerShellドライブに関する情報を取得します。このコマンドレットは環境検証に不可欠であり、利用可能な接続とそのステータスを確認するために、任意のUiPathOrchセッションで最初に実行すべきコマンドです。

プライマリエンドポイント: N/A (ローカル構成)

OAuth必須スコープ: N/A

必要な権限: N/A

## EXAMPLES

### Example 1: 構成されたすべてのドライブをリスト
```powershell
PS C:\> Get-OrchPSDrive
```

接続の詳細とともに、構成されたすべてのUiPath Orchestratorドライブをリストします。

### Example 2: 短縮されたスコープを表示
```powershell
PS C:\> Get-OrchPSDrive Orch1: | Select-Object -ExpandProperty Scope
```

UiPathOrchは内部的に長いスコープを同等の短い形式に変換します。これらの短縮されたスコープは構成ファイルに自動的に書き戻されませんが、上記のコマンドを使用してそれらを確認できます。

### Example 3: ドライブに関連するすべての情報を表示

```powershell
PS C:\> Get-OrchPSDrive | Select-Object *
```

`PartitionGlobalId`、`TenantId`、`TenantKey`、`AccessToken`などのテナント関連情報を確認できます。この情報は、接続が確立されているテナントに対してのみ表示されることに注意してください。接続を強制するには、`-Force`スイッチパラメータを使用してください。

## PARAMETERS

### -Force
APIバージョンと接続ステータスを含む完全な情報を取得するために、有効なドライブへの接続を強制します。デフォルトでは、Get-OrchPSDriveは認証API呼び出しと非機密アプリ構成での潜在的なブラウザログインプロンプトを避けるために、未接続のドライブに対しては構成ファイル情報のみを表示します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。

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

### -Path
結果をフィルタリングするドライブ名パターンを指定します。ワイルドカードとカンマ区切りの値をサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.OrchPSDrive
## NOTES
このコマンドレットは、環境と利用可能な接続を確認するために、任意のUiPathOrchセッションの開始時に常に実行すべきです。
- デフォルトでは、接続せずにすべてのドライブの構成情報を表示します
- 接続済みドライブには追加の詳細が表示されます：ApiVersion、CurrentUser、TenantId、AccessToken
- 未接続のドライブは認証のオーバーヘッドを避けるために構成ファイル情報のみを表示します
- すべての有効なドライブに接続して完全な接続詳細を取得するには-Forceを使用してください
- 非機密アプリドライブは-Force使用時にブラウザログイン（PKCE）が必要な場合があります

## RELATED LINKS

[Edit-OrchConfig](Edit-OrchConfig.md)
[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
[Clear-OrchCache](Clear-OrchCache.md)
