---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchHelp

## SYNOPSIS
UiPathOrch モジュールのドキュメントとクイックスタートガイドを表示します。

## SYNTAX

```
Get-OrchHelp [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchHelp は、UiPathOrch モジュールの包括的なドキュメントとクイックスタートガイドを表示します。このコマンドレットは以下の情報を提供します：

- モジュールのインストールパス
- 利用可能なドキュメント（LLM向けテキストファイルとPDFマニュアル）
- 重要なコマンドの一覧
- LLM（大規模言語モデル）向けの使用方法とヒント

初回利用時やモジュールの機能を確認したい場合に実行することで、効率的にUiPath Orchestratorの操作を開始できます。

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-OrchHelp
```

UiPathOrch モジュールの完全なヘルプ情報を表示します。モジュールパス、利用可能なドキュメント、重要なコマンド、クイックスタートガイドが表示されます。

## PARAMETERS

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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
- このコマンドレットは、UiPath Orchestrator を操作する LLM（AI）エージェントに特に有用です
- 利用可能な PSDrives を確認するため、常に最初に Get-OrchPSDrive を実行してください
- 必須の実行ルールについては、01-Essentials.txt ファイルを確認してください
- PDF マニュアルは英語版と日本語版の両方が利用可能です

## RELATED LINKS

[Get-OrchPSDrive]()

[Get-OrchCurrentUser]()

[Clear-OrchCache]()
