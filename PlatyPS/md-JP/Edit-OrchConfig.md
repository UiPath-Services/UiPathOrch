---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Edit-OrchConfig

## SYNOPSIS
UiPath Orchestrator設定ファイルを編集のために開きます。

## SYNTAX

```
Edit-OrchConfig [-EditorType <String>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Edit-OrchConfigコマンドレットは、UiPathOrchConfig.json設定ファイルをデフォルトまたは指定されたエディタで開きます。このファイルには、URL、認証設定、その他の接続パラメータを含む、すべてのUiPath OrchestratorドライブのConnectin設定が含まれています。

設定ファイルは以下の場所にあります：
([Environment]::GetFolderPath('MyDocuments'))\PowerShell\Modules\UiPathOrch\UiPathOrchConfig.json

主要エンドポイント: N/A（ローカルファイル操作）

OAuth必要スコープ: N/A

必要な権限: ユーザードキュメントフォルダへのファイルシステム書き込みアクセス

## EXAMPLES

### Example 1: デフォルトエディタで設定ファイルを開く
```powershell
PS C:\> Edit-OrchConfig
```

UiPathOrchConfig.jsonファイルをデフォルトのテキストエディタ（通常はWindowsのメモ帳）で開きます。

### Example 2: デフォルトJSONエディタで開く
```powershell
PS C:\> Edit-OrchConfig Default
```

.jsonファイルに関連付けられたデフォルトエディタ（例：Visual Studio Code、Notepad++、その他のJSONエディタ）を使用して設定ファイルを開きます。

## PARAMETERS

### -ProgressAction
このコマンドレットによって生成される進行状況更新にPowerShellがどのように応答するかを決定します。

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

### -EditorType
設定ファイルを開くために使用するエディタを指定します。有効な値は"Notepad"（Windowsのメモ帳を使用）と"Default"（.jsonファイルに関連付けられたデフォルトエディタを使用）です。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

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

### System.Object
## NOTES
設定ファイルを変更した後：
1. エディタでファイルを保存
2. PowerShellセッションを再起動
3. 必要なモジュールをインポート：Import-Module UiPathOrch, PSReadLine
4. Get-OrchPSDriveで設定を確認
5. Get-OrchCurrentUserで接続をテスト

設定ファイルにはOAuth設定を含む機密情報が含まれています。適切なファイル権限を確保し、このファイルの共有を避けてください。

## RELATED LINKS

[Get-OrchPSDrive](Get-OrchPSDrive.md)
[Get-OrchCurrentUser](Get-OrchCurrentUser.md)
[Clear-OrchCache](Clear-OrchCache.md)
