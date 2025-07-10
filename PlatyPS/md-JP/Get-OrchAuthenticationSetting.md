---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAuthenticationSetting

## SYNOPSIS
UiPath Orchestrator から認証とシステム構成設定を取得します。

## SYNTAX

```
Get-OrchAuthenticationSetting [[-Key] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchAuthenticationSetting コマンドレットは、UiPath Orchestrator から認証とシステム構成設定を取得します。これらの設定は、認証、承認、アプリケーションの動作、およびビルド情報、カルチャ設定、外部サービス統合を含むシステム構成のさまざまな側面を制御します。

設定には、認証構成（Auth.*）、アプリケーションプロパティ（Application.*）、ビルド情報（Build.*）、ローカライゼーション設定、トークン認証オプション、テレメトリ構成が含まれます。各設定は、構成パラメーターとその現在の値を示すキーと値のペアとして返されます。

認証設定は、テナント全体のスコープで動作するテナントエンティティです。ドライブ名（例：Orch1:、Orch2:）でターゲットテナントを指定するには、-Path パラメーターを使用します。

このコマンドレットはテナントレベルエンティティ操作として動作し、指定された Orchestrator 環境からシステム全体の構成設定を取得します。これらの設定は通常読み取り専用で、現在のシステム構成を反映します。

プライマリエンドポイント: GET /odata/Settings

OAuth 必須スコープ: OR.Settings または OR.Settings.Read

必要なアクセス許可: Settings.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting
```

すべての認証とシステム設定を取得し、すべての構成パラメーターのキーと値のペアを表示します。

### Example 2
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting Auth.*
```

ワイルドカードキーフィルタリングを使用して認証関連の設定を取得します。

### Example 3
```powershell
PS C:\> Get-OrchAuthenticationSetting -Path Orch1:, Orch2: -Key *Token*
```

複数のテナントから "Token" を含むキーの設定を取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchAuthenticationSetting | Select-Object Path, Key, Value | Sort-Object Key
```

Path を最初に表示して、キーでアルファベット順にソートされたすべての設定を表示します。

## PARAMETERS

### -Key
取得する認証設定のキーを指定します。柔軟な設定選択のためのワイルドカードパターンをサポートします。

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
Accept wildcard characters: True
```

### -ProgressAction
{{ ProgressAction の説明を入力 }}

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

### UiPath.PowerShell.Entities.ResponseDictionaryItem
## NOTES
このコマンドレットは、システム構成設定にアクセスするためのテナントレベルエンティティ操作です。これらの設定は、認証動作、アプリケーションプロパティ、システム機能を制御します。ほとんどの設定は読み取り専用で、現在の Orchestrator 構成を反映します。特定の構成カテゴリを見つけるには、キーパターンによるフィルタリングを使用してください。この操作には Settings.View アクセス許可が必要です。

プライマリエンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetAuthenticationSettings
OAuth 必須スコープ: OR.Settings または OR.Settings.Read
必要なアクセス許可: Settings.View

## RELATED LINKS

[Get-OrchSetting](Get-OrchSetting.md)

[Get-OrchExecutionSetting](Get-OrchExecutionSetting.md)

[Set-OrchSetting](Set-OrchSetting.md)

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
