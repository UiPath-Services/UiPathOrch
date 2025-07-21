---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchFolderMachine

## SYNOPSIS
フォルダーに割り当てられたマシンを取得します。

## SYNTAX

```
Get-OrchFolderMachine [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
ターゲットフォルダーに割り当てられた、指定された名前のマシンに関する情報を出力します。ターゲットフォルダーは、-Path、-Recurse、-Depthパラメーターを使用して指定できます。これらが指定されていない場合は、現在の場所がターゲットフォルダーとして使用されます。マシン名が指定されていない場合は、ターゲットフォルダーに割り当てられたすべてのマシンを出力します。

-Pathと-Nameパラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値のオートコンプリートを使用できます。

-Path、-Recurse、-Depthパラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターのオートコンプリートが正しく機能します。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder(key={folderId})

OAuth必要スコープ: OR.Folders または OR.Folders.Read

必要な権限: (Units.View または SubFolders.View - 任意のフォルダーのマシンを取得するか、ユーザーがフォルダーにSubFolders.View権限を持っている場合のみ)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchFolderMachine
```

現在の場所である"Shared"フォルダーに割り当てられたすべてのマシンを表示します。

### Example 2
```powershell
PS Orch1:\> Get-OrchFolderMachine -Recurse
```

現在のフォルダーとそのすべてのサブフォルダーに割り当てられたすべてのマシンを表示します。ルートフォルダーで実行すると、そのテナントのすべてのフォルダーに割り当てられたすべてのマシンが表示されます。

### Example 3
```powershell
PS Orch1:\> Get-OrchFolderMachine -Recurse Machine*
```

現在のフォルダーとそのサブフォルダーに割り当てられた、名前が"Machine"で始まるマシンを表示します。これは、特定の名前パターンを持つマシンがどのフォルダーに含まれているかを特定するのに便利です。

### Example 4
```powershell
PS Orch1:\> Get-OrchFolderMachine -Path Orch1:\\Production, Orch1:\\Development Template*
```

ProductionとDevelopmentフォルダーに割り当てられた、名前が"Template"で始まるマシンを表示します。

### Example 5
```powershell
PS C:\> Get-OrchFolderMachine -Recurse -Path Orch1:\,Orch2:\
```

Orch1:とOrch2:に割り当てられたすべてのマシンを表示します。

### Example 6
```powershell
PS C:\> Get-OrchFolderMachine -Recurse | select Path,Id,Name
```

選択した列のみで出力を表示します。ワイルドカードを含む、カンマで区切られた複数の列を指定します。列名は[Ctrl+Space]または[Tab]でオートコンプリートできます。

### Example 7
```powershell
PS C:\> Get-OrchFolderMachine -Recurse | Export-Csv c:folderMachines.csv
```

出力をCSVファイルにエクスポートします。CSVファイルはC:ドライブの現在の場所に配置されます。`select`と組み合わせて含める列を指定することで、CSV形式をカスタマイズできます。`ii c:`を試してC:ドライブの現在の場所を開きます。

### Example 8
```powershell
PS C:\> Get-OrchFolderMachine -Recurse | ConvertTo-Json
```

出力をJSON形式に変換し、Orchestratorからのデータの生のビューを提供します。

### Example 9
```powershell
PS C:\> Get-OrchFolderMachine -Recurse -ExportCsv C:\Reports\FolderMachines.csv
```

レポートまたはバックアップ目的で、すべてのフォルダーマシン割り当てをCSVファイルにエクスポートします。CSVには、人間が読める名前とフォルダー割り当てが含まれます。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダーは含まれません。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
取得するフォルダーマシンの名前を指定します。

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
ターゲットフォルダーを指定します。指定しない場合は、現在のフォルダーがターゲットとなります。

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
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。デフォルト値はContinueです。

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
操作がターゲットフォルダーとそのすべてのサブフォルダーを含むように指定します。

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

### -CsvEncoding
-ExportCsvを使用する場合にエクスポートされるCSVファイルのエンコーディングを指定します。デフォルトは、Excel互換性のためのBOM付きUTF-8です。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
取得したフォルダーマシンを指定されたパスのCSVファイルにエクスポートします。CSVには、内部IDの代わりに人間が読める名前が含まれ、Excel互換性のためにBOM付きUTF-8エンコーディングを使用します。

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

### UiPath.PowerShell.Entities.MachineFolder
## NOTES
呼び出される主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder({folderId})

必要なスコープ: OR.Folders.Read

## RELATED LINKS
