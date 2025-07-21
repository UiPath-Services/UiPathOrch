---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchLibrary

## SYNOPSIS
ライブラリパッケージをUiPath Orchestratorにインポートします。

## SYNTAX

```
Import-OrchLibrary [-Source] <String[]> [[-Path] <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Import-OrchLibraryコマンドレットは、ライブラリパッケージ（.nupkgファイル）をUiPath Orchestratorにインポートします。ライブラリは、アクティビティ、ワークフロー、その他のリソースを含む再利用可能なコンポーネントで、異なるプロセス間で共有できます。このコマンドレットを使用すると、ライブラリパッケージをアップロードして展開し、オートメーションプロジェクトで使用できるようにします。

このコマンドレットは、各ライブラリのインポート状況情報（成功または失敗の詳細を含む）を返します。

-Pathパラメータは、ターゲットドライブを指定します。指定されていない場合は、現在のドライブがターゲットになります。

これは、Orchestratorインスタンスにライブラリをインポートするテナントレベルの操作です。

プライマリ エンドポイント: POST /odata/Libraries/UiPath.Server.Configuration.OData.UploadPackage

OAuth 必要なスコープ: OR.Execution

必要な権限: Libraries.Create

## EXAMPLES

### Example 1:
```powershell
PS Orch1:\> Import-OrchLibrary c:
```

Cドライブの現在の場所にあるすべてのライブラリ*.nupkgファイルを、現在のOrchestratorテナントにインポートします。

### Example 2:
```powershell
PS Orch1:\> Import-OrchLibrary C:\Libraries
```

指定されたフォルダ内のすべてのライブラリ*.nupkgファイルを、現在のOrchestratorテナントにインポートします。

### Example 3:
```powershell
PS C:\Libraries> Import-OrchLibrary . Orch1:, Orch2: -WhatIf
```

現在のディレクトリ（.）からすべてのライブラリ*.nupkgファイルを取得し、Orch1:とOrch2:の両方にインポートします。

### Example 4:
```powershell
PS Orch1:\> Import-OrchLibrary C:\Libraries\*mylib*.nupkg, C:\Libraries\*test*.nupkg -Confirm
```

複数のライブラリパッケージを一度の操作でインポートします。

## PARAMETERS

### -Path
ターゲットドライブの名前を指定します。指定されていない場合は、現在のドライブがターゲットになります。このパラメータを使用すると、複数のOrchestratorインスタンスにライブラリをインポートできます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

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
インポートするライブラリパッケージ（.nupkgファイル）のファイルパスを指定します。複数のファイルパスを配列として指定できます。ファイルは存在し、有効なUiPathライブラリパッケージである必要があります。

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
コマンドレットを実行する前に確認を求めます。これにより、ライブラリをインポートする前に追加の安全性チェックが提供されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
実際にインポート操作を実行せずに、コマンドレットを実行した場合の結果を表示します。これは、変更を行う前にどのライブラリがインポートされるかを確認するのに便利です。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

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

### UiPath.PowerShell.Entities.BulkItemDtoOfString
## NOTES
- ライブラリファイルは、UiPath Studioまたはその他のUiPathツールによって作成された有効な.nupkgパッケージである必要があります
- 結果のStatusプロパティは、インポートが成功（"OK"）したか失敗したかを示します
- 同じバージョンで既に存在するライブラリをインポートすると、Orchestratorの設定によって異なる動作が発生する場合があります
- 実行前にインポート操作をプレビューするには、-WhatIfパラメータを使用してください
- Bodyプロパティには、IdやVersionを含むインポートされたライブラリに関するJSON形式の詳細が含まれています
- 大きなライブラリファイルは、ネットワーク速度とパッケージサイズによってインポートに時間がかかる場合があります
- このコマンドレットはテナントレベルで動作し、Orchestratorインスタンス全体にライブラリをインポートします

## RELATED LINKS

[Get-OrchLibrary]()

[Export-OrchLibrary]()

[Remove-OrchLibrary]()

[Get-OrchLibraryVersion]()
