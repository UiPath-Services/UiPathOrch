---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPackage

## SYNOPSIS
UiPath Orchestratorからプロセスパッケージを取得します。

## SYNTAX

```
Get-OrchPackage [[-Id] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
プロセスパッケージは、テナントフィード（テナント全体スコープ）とフォルダフィード（フォルダ固有スコープ）の両方に存在します。このコマンドレットは、コンテキストに応じてテナントエンティティとフォルダエンティティの両方として動作します。

最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurseパラメータを使用してターゲットフォルダを指定してください。テナント全体のパッケージアクセスの場合は、-Pathパラメータでテナントドライブ（例：Orch1:）を指定してください。

Get-OrchPackage コマンドレットは、UiPath Orchestratorからプロセスパッケージを取得します。このコマンドレットを使用すると、テナントフィードまたはフォルダフィードからパッケージをリストし、IDでフィルタリング、ターゲットフォルダの指定、サブフォルダの再帰的包含などのオプションを使用できます。

プライマリエンドポイント: GET /odata/Processes&feedId={feedId}

OAuth必須スコープ: OR.Execution または OR.Execution.Read

必要な権限: (Packages.View - テナントフィード内のパッケージをリスト) および (FolderPackages.View - フォルダフィード内のパッケージをリスト)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchPackage
```

現在のフォルダコンテキストですべてのプロセスパッケージを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchPackage -Recurse
```

すべてのフォルダから再帰的にすべてのプロセスパッケージを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchPackage -Path Orch1:\Production *Process*
```

Productionフォルダから「ProcessA」で始まるIDを持つプロセスパッケージを取得します。

### Example 4
```powershell
PS C:\> Get-OrchPackage -Path Orch1: *Blank*
```

テナントフィード（テナント全体スコープ）からTestProcessパッケージを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchPackage -Recurse | Select-Object Path, Id, Version, IsActive
```

すべてのパッケージを再帰的に取得し、Pathを最初に表示して主要なプロパティを表示します。

## PARAMETERS

### -Id
取得するプロセスパッケージのIdを指定します。ワイルドカード文字と複数の値をサポートします。

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
ターゲットフォルダを指定します。指定されていない場合は、現在のフォルダがターゲットになります。複数のフォルダパスをサポートします。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。

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

### -Recurse
操作にターゲットフォルダとそのすべてのサブフォルダを含めることを指定します。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Package
## NOTES
- アクティブな接続が存在しない場合、コマンドレットは自動的にUiPath Orchestratorへの接続を確立します。非機密アプリケーションの場合、最初のコマンドレット実行時にPKCE認証（ブラウザログイン）が開始されます。

プライマリエンドポイント: GET /odata/Libraries
OAuth必須スコープ: OR.Execution または OR.Execution.Read
必要な権限: Libraries.View

## RELATED LINKS

[Import-OrchPackage](Import-OrchPackage.md)
[Export-OrchPackage](Export-OrchPackage.md)
[Remove-OrchPackage](Remove-OrchPackage.md)
