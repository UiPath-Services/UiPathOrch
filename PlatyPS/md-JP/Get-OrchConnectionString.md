---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchConnectionString

## SYNOPSIS
接続文字列を取得します。

## SYNTAX

```
Get-OrchConnectionString [[-Path] <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchConnectionString コマンドレットは、Orchestrator サービスがデータベースへの接続に使用する接続文字列を取得します。この情報は、データベース接続設定の確認、トラブルシューティングの実行、または現在の Orchestrator 構成の理解が必要なシステム管理者にとって有用です。

この接続文字列には、データベースサーバー名、データベース名、認証情報、およびその他の接続パラメーターが含まれます。セキュリティ上の理由から、パスワードなどの機密情報はマスクされるか、部分的に隠されている場合があります。

このコマンドレットはテナントレベルで動作し、指定されたテナントまたは現在のテナントの接続文字列情報を取得します。-Path パラメーターを使用して複数のテナントから接続文字列を取得できます。

プライマリエンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetConnectionString

OAuth 必須スコープ: OR.Settings または OR.Settings.Read

必要なアクセス許可: Settings.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchConnectionString
```

現在のテナントのデータベース接続文字列を取得します。

### Example 2
```powershell
PS C:\> Get-OrchConnectionString -Path Orch1:, Orch2:
```

複数のテナント（Orch1 と Orch2）からデータベース接続文字列を取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchConnectionString | Select-Object Path, Value
```

接続文字列を取得し、Path と Value プロパティのみを表示します。

### Example 4
```powershell
PS Orch1:\> Get-OrchConnectionString | ConvertTo-Json
```

接続文字列情報を JSON 形式で表示します。

## PARAMETERS

### -Path
接続文字列を取得するターゲットテナントドライブを指定します。指定されていない場合、現在のテナントがターゲットになります。

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

### UiPath.PowerShell.Entities.ResponseDictionaryItem
## NOTES

このコマンドレットはテナントレベルエンティティ操作として動作し、システム管理者がデータベース接続設定を確認できるようにします。接続文字列には機密情報が含まれる可能性があるため、セキュリティ上の理由から一部の情報がマスクされる場合があります。

接続文字列情報は、データベース接続の問題をトラブルシューティングしたり、Orchestrator の構成を理解したりする際に特に有用です。この操作には Settings.View アクセス許可が必要です。

プライマリエンドポイント: GET /odata/Settings/UiPath.Server.Configuration.OData.GetConnectionString
OAuth 必須スコープ: OR.Settings または OR.Settings.Read
必要なアクセス許可: Settings.View

## RELATED LINKS

[Get-OrchSetting](Get-OrchSetting.md)

[Get-OrchAuthenticationSetting](Get-OrchAuthenticationSetting.md)

[Get-OrchExecutionSetting](Get-OrchExecutionSetting.md)

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
