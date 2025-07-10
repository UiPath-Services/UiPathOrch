---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Open-OrchLogLocation

## SYNOPSIS
UiPathOrchコマンドレットのHTTPログが出力されるフォルダを開きます。

## SYNTAX

```
Open-OrchLogLocation [[-Path] <String>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
`Open-OrchLogLocation`コマンドレットは、UiPathOrchモジュールのコマンドレットによって生成されたHTTPリクエスト/レスポンスログを含むフォルダを開きます。これらのログは、UiPath OrchestratorとのAPI通信のデバッグやトラブルシューティングに便利です。

デフォルトでは、コマンドレットはシステムのデフォルトログ場所を開きます。オプションで`-Path`パラメータを使用してカスタムパスを指定し、別の場所を開くことができます。

ログファイルには、UiPathOrch PowerShellモジュールとUiPath Orchestrator Web APIエンドポイント間で交換されるHTTPリクエストとレスポンスの詳細情報が含まれています。

プライマリ エンドポイント: ローカルユーティリティ関数（APIエンドポイントなし）

OAuth 必要なスコープ: 該当なし（ローカルユーティリティ関数）

必要な権限: 該当なし（ローカルユーティリティ関数）

## EXAMPLES

### Example 1
```powershell
PS C:\> Open-OrchLogLocation
```

UiPathOrch HTTPログが保存されているデフォルトのログフォルダ場所を開きます。

### Example 2
```powershell
PS C:\> Open-OrchLogLocation -Path "C:\CustomLogs"
```

デフォルトのログ場所ではなく、指定されたカスタムフォルダパスを開きます。

## PARAMETERS

### -Path
デフォルトのログ場所の代わりに開く別のフォルダパスを指定します。指定されていない場合、コマンドレットはUiPathOrch HTTPログのデフォルトシステムログフォルダを開きます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: Default system log folder
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Write-Progressコマンドレットによって生成される進行状況バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況更新にPowerShellがどのように応答するかを指定します。有効な値は、SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
- これは、他のUiPathOrchコマンドレットによって生成されたログファイルにアクセスするためのユーティリティコマンドレットです
- 実際のログファイルには、デバッグ目的でHTTPリクエスト/レスポンスの詳細が含まれています
- ログファイルには、認証トークンなどの機密情報が含まれている場合があります

## RELATED LINKS

[about_UiPathOrch](about_UiPathOrch.md)
