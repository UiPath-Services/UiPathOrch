---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Edit-OrchProcess

## SYNOPSIS
デフォルトのWebブラウザでプロセスの詳細を編集用に開きます。

## SYNTAX

```
Edit-OrchProcess [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
`Edit-OrchProcess`コマンドレットは、指定されたプロセスの編集用プロセス詳細ページをデフォルトのWebブラウザで開きます。これにより、PowerShellからOrchestrator Webインターフェースに便利にナビゲートして、プロセス設定の変更、プロセス詳細の表示、またはコマンドラインでは利用できない編集機能へのアクセスを行うことができます。

このコマンドレットは、PowerShellからWebインターフェースにすぐにナビゲートして、プロセス設定の編集、プロセス設定の更新、またはビジュアルプロセスデザイナーにアクセスするのに便利です。

複数のプロセス名を指定して、複数のプロセス編集ページを同時に開くことができます。[Ctrl+Space]または[Tab]を押してプロセス名のオートコンプリートを使用できます。

主要エンドポイント: プロセス編集ページへのブラウザナビゲーション（直接のAPIエンドポイントなし）

OAuth必要スコープ: 適用外（ブラウザナビゲーション）

必要な権限: Processes.Edit（編集ページへのブラウザアクセス用）

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1:\MyWorkspace
PS Orch1:\MyWorkspace> Edit-OrchProcess MsgBox
```

デフォルトのWebブラウザで"MsgBox"プロセスのプロセス編集ページを開きます。

### Example 2
```powershell
PS Orch1:\MyWorkspace> Edit-OrchProcess BackgroundProcess, BackgroundProcess
```

複数のプロセスのプロセス編集ページを個別のブラウザタブまたはウィンドウで開きます。

### Example 3
```powershell
PS Orch1:\> Get-OrchProcess -Name *Agent* | Edit-OrchProcess
```

名前に"Agent"が含まれるすべてのプロセスを取得し、それらの編集ページを開きます。

### Example 4
```powershell
PS Orch1:\MyWorkspace> Edit-OrchProcess My*
```

ワイルドカードパターンマッチングを使用して、名前が"My"で始まるすべてのプロセスの編集ページを開きます。

### Example 5
```powershell
PS Orch1:\> Edit-OrchProcess -Path Orch1:\Shared, Orch1:\root MsgBox
```

複数の指定されたフォルダから"MsgBox"プロセスの編集ページを開きます。

## PARAMETERS

### -Name
ブラウザで編集するプロセスの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押してオートコンプリートを使用できます。

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
ターゲットフォルダパスを指定します。指定されない場合、現在のフォルダが対象になります。このパラメータはパイプライン入力を受け取り、複数のパスを指定するためのワイルドカードをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
Write-Progressコマンドレットによって生成される進行状況バーなど、スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況更新にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspendです。

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

### System.Object
## NOTES
- これは、PowerShellからOrchestrator Webインターフェースにナビゲートするためのユーティリティコマンドレットです
- このコマンドレットには、アクティブなWebブラウザとOrchestratorインスタンスへのインターネット接続が必要です
- 複数のプロセス名を指定すると、ブラウザ設定に応じて複数のブラウザタブまたはウィンドウが開かれます
- ユーザーは、Webインターフェースでプロセスを編集するための適切な権限を持っている必要があります
- プロセス名にワイルドカードパターンがサポートされています

## RELATED LINKS

[Get-OrchProcess](Get-OrchProcess.md)
[New-OrchProcess](New-OrchProcess.md)
[Update-OrchProcess](Update-OrchProcess.md)
[Remove-OrchProcess](Remove-OrchProcess.md)
[about_UiPathOrch](about_UiPathOrch.md)
