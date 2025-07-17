---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchPackage

## SYNOPSIS
UiPath Orchestratorにパッケージをインポートします。

## SYNTAX

```
Import-OrchPackage [-Source] <String[]> [[-Path] <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Import-OrchPackage コマンドレットは、UiPath Orchestratorにパッケージファイルをインポートします。パッケージは、自動化プロセス、ライブラリ、またはその他のUiPath成果物を含むことができる圧縮されたファイルです。

このコマンドレットは、ローカルファイルシステムからOrchestratorにパッケージをアップロードし、デプロイメントと配布のために利用できるようにします。パッケージのインポートは、自動化の展開と管理の重要な部分です。

-Pathパラメーターの複数の値は、カンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：POST /odata/Packages

OAuth必須スコープ：OR.Packages または OR.Packages.Write

必須権限：Packages.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Import-OrchPackage C:\LocalFeed\MyProcess.1.0.1.nupkg
```

ローカルファイルシステムからパッケージをOrchestratorのテナントフィードにインポートします。

### Example 2
```powershell
PS Orch1:\Development> Import-OrchPackage C:\LocalFeed\MyProcess.1.0.1.nupkg
```

ローカルファイルシステムからパッケージをOrchestratorのフォルダフィードにインポートします。Orch1:\Developmentフォルダにフィードがない場合には、テナントフィードにインポートされます。

### Example 3
```powershell
PS C:\LocalFeed> Import-OrchPackage MyProcess.1.0.1.nupkg -Path Orch1:, Orch2:
```

複数のOrchestratorインスタンスのテナントフィードに同じパッケージをインポートします。

### Example 4
```powershell
PS LocalFeed:\> Import-OrchPackage . -Path Orch1: -Confirm
```

フォルダ内のすべてのパッケージファイルを一括でインポートします。インポート先は、Orch1: ドライブのカレントフォルダになります。カレントフォルダの場所によらず、必ずテナントフィードにインポートしたいときは -Path に Orch1:\ を指定してください。

## PARAMETERS

### -Path
パッケージをインポートする対象のOrchestratorドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、複数のOrchestratorインスタンスを指定するためのパイプライン入力を受け入れます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### -Source
{{ Fill Source Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Recurse
{{ Fill Recurse Description }}

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメーターをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString
## NOTES
- このコマンドレットは、Orchestratorドライブで動作します
- パッケージファイルは.nupkg拡張子を持つ必要があります
- パッケージのインポートには適切な権限が必要です
- 既存のパッケージと同じ名前とバージョンのパッケージをインポートすると、既存のパッケージが置き換えられる場合があります
- 大きなパッケージファイルのインポートには時間がかかる場合があります

主要エンドポイント：POST /odata/Packages
OAuth必須スコープ：OR.Packages または OR.Packages.Write
必須権限：Packages.Create

## RELATED LINKS

[Get-OrchPackage](Get-OrchPackage.md)
[Remove-OrchPackage](Remove-OrchPackage.md)
[about_UiPathOrch](about_UiPathOrch.md)
