---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchProcess

## SYNOPSIS
プロセスを取得します。

## SYNTAX

```
Get-OrchProcess [[-Name] <String[]>] [-ExpandDetails] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Orchestratorフォルダからプロセス情報を取得します。このコマンドレットはデフォルトで基本的なプロセスメタデータを取得し、プロセス設定、引数、ランタイム構成を含む詳細情報を展開するオプションがあります。

プロセスは特定のフォルダスコープ内で動作するフォルダエンティティです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定してください。

-Pathパラメータの複数の値は、ワイルドカードを含むカンマ区切りのテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

-Path、-Recurse、および-Depthパラメータを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメータの自動補完が正しく機能します。

主要エンドポイント: GET /odata/Releases/?$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint

OAuth必須スコープ: OR.Execution または OR.Execution.Read

必要な権限: Processes.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchProcess
```

現在のフォルダからプロセスを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchProcess -Recurse
```

現在のフォルダとすべてのサブフォルダからプロセスを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchProcess -Recurse *Invoice*
```

すべてのフォルダから名前に"Invoice"を含むプロセスを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchProcess -Path Orch1:\Shared, Orch1:\Finance -Recurse
```

特定のフォルダとそのサブフォルダからプロセスを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchProcess -Recurse MainProcess, *Test*
```

すべてのフォルダから名前パターンによって特定のプロセスを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExpandDetails | Where-Object {$_.ProcessSettings.AutopilotForRobots.Enabled}
```

展開された詳細を使用してAutopilot for Robotsが有効なプロセスを取得します。

### Example 7
```powershell
PS Orch1:\> Get-OrchProcess -Recurse | Where-Object {$_.JobPriority -eq "High"}
```

すべてのフォルダから高優先度のプロセスを取得します。

### Example 8
```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExportCsv C:\Reports\Processes.csv
```

UTF-8 BOMエンコーディングですべてのプロセスをCSVにエクスポートします。エクスポートされたCSVは、Import-Csv | New-OrchProcess または Import-Csv | Update-OrchProcess を使用してインポートできます。

## PARAMETERS

### -Depth
フォルダ再帰の深度を指定します。深度0は現在のフォルダのみをターゲットにします。

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
取得するプロセスの名前を指定します。ワイルドカードと複数の値をサポートします。

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
ターゲットフォルダを指定します。複数のフォルダにはカンマ区切りの値を使用します。ワイルドカードをサポートします。指定されていない場合は、現在のフォルダをターゲットにします。

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
コマンドレット実行中の進行状況情報の表示方法を制御します。

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
ターゲットフォルダとそのすべてのサブフォルダを操作に含めます。

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

### -CsvEncoding
CSVエクスポートのエンコーディングを指定します。デフォルトはExcel互換性のためのBOM付きUTF-8です。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
BOM付きUTF-8エンコーディングで結果をCSVファイルにエクスポートします。内部IDを人間が読める名前に自動変換します。対応するImportコマンドレットと組み合わせて使用できます。

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

### -ExpandDetails
ProcessSettings、Arguments、VideoRecordingSettingsを含む詳細なプロセス情報を取得します。このパラメータがない場合、これらのプロパティはnull値を返します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Release
## NOTES
プロセスエンティティはフォルダスコープです。フォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

AutopilotForRobots、VideoRecordingSettings、詳細なArgumentsメタデータなどのProcessSettingsプロパティにアクセスする必要がある場合は、-ExpandDetailsを使用してください。

-ExportCsvパラメータは、内部IDの代わりに人間が読める名前を持つインポート対応CSVファイルを作成します。

主要エンドポイント: GET /odata/Releases
OAuth必須スコープ: OR.Execution または OR.Execution.Read
必要な権限: Processes.View

## RELATED LINKS

[New-OrchProcess](New-OrchProcess.md)

[Update-OrchProcess](Update-OrchProcess.md)

[Remove-OrchProcess](Remove-OrchProcess.md)

[Copy-OrchProcess](Copy-OrchProcess.md)
