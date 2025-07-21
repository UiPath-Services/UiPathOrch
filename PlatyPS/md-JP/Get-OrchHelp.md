---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchHelp

## SYNOPSIS
UiPathOrchモジュールのドキュメントとクイックスタートガイドを表示します。

## SYNTAX

```
Get-OrchHelp [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

`Get-OrchHelp` コマンドレットは、UiPathOrchモジュールのドキュメントとクイックスタートガイドを表示します。このコマンドは、モジュール情報、利用可能なリソース、およびUiPath Orchestrator自動化を開始するためのガイダンスへの即座のアクセスを提供します。

このコマンドレットは、ユーザーを適切なドキュメント、参照資料、およびUiPathOrchモジュールで効果的に作業するために必要な重要なコマンドに案内する集中ヘルプシステムとして機能します。

このコマンドレットは、モジュールの機能についてオリエンテーションが必要な新しいユーザー、UiPath Orchestratorと統合する開発者、およびモジュール使用パターンについて構造化されたガイダンスを必要とする自動化システムにとって特に価値があります。

このコマンドは、利用可能なリソースの概要とUiPathOrchモジュールでの作業に推奨される次のステップを提供するフォーマットされたテキストを返します。

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-OrchHelp
```

UiPathOrchモジュールのヘルプとドキュメントガイドを表示します。これにより、利用可能なリソースと入門情報の概要が提供されます。

### Example 2
```powershell
PS C:\> Get-OrchHelp | Out-Host
```

Out-Hostを使用してヘルプ情報を表示し、コンソールでの適切なフォーマットを確保します。出力フォーマットが重要なスクリプトで有用です。

### Example 3
```powershell
PS C:\> $helpText = Get-OrchHelp
PS C:\>
```

さらなる処理や分析のためにヘルプコンテンツを変数にキャプチャします。このアプローチにより、ヘルプテキストをプログラム的に操作できます。

### Example 4
```powershell
PS C:\> Get-OrchHelp | Out-File -FilePath "OrchModuleHelp.txt"
```

オフライン参照やドキュメント目的でヘルプコンテンツをテキストファイルに保存します。

### Example 5
```powershell
PS C:\> Get-OrchHelp | Select-String "Essential Commands" -A 5
```

重要なコマンドセクションなど、ヘルプコンテンツから特定のセクションを抽出します。これは、特定のガイダンス情報にアクセスする必要がある自動化スクリプトに有用です。

## PARAMETERS

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### System.String
## NOTES

このコマンドレットは、UiPathOrchモジュールを初めて使用するユーザーで、利用可能なリソースと推奨プラクティスに関するガイダンスが必要な場合に特に有用です。

このコマンドレットは、モジュールの探索と学習の出発点として機能する集中ヘルプシステムを提供します。

プログラム的アクセスまたは統合シナリオの場合、出力を必要に応じてキャプチャおよび処理できます。

このコマンドレットはパラメーターを必要とせず、モジュールインポート後すぐに実行してオリエンテーションとガイダンスを提供できます。

ヘルプコンテンツは自己完結型に設計されており、外部依存関係を必要とせずに追加リソースへの方向性を提供します。

## RELATED LINKS

[Get-OrchPSDrive]()

[Get-OrchCurrentUser]()

[Clear-OrchCache]()
