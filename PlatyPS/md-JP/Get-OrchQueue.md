---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchQueue

## SYNOPSIS
キューを取得します。

## SYNTAX

```
Get-OrchQueue [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorフォルダからキュー情報を取得します。キューは、ロボットが情報やワークアイテムを組織的に交換できるデータリポジトリであり、有人および無人自動化シナリオの両方をサポートします。

このコマンドレットは、定義、再試行ポリシー、暗号化設定、SLA構成、保持ポリシー、組織単位の割り当てを含むキュー情報を返します。フォルダエンティティで動作し、フォルダ階層全体での再帰的取得をサポートします。

-Nameおよび-Pathパラメータの複数の値は、ワイルドカードを含むカンマ区切りのテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

-Path、-Recurse、および-Depthパラメータを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメータの自動補完が正しく機能します。

プライマリエンドポイント: GET /odata/QueueDefinitions

OAuth必須スコープ: OR.Queues または OR.Queues.Read

必要な権限: Queues.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchQueue
```

現在のフォルダからキューを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchQueue -Recurse
```

すべてのフォルダから再帰的にキューを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchQueue -Recurse *Invoice*
```

すべてのフォルダから名前に「Invoice」を含むキューを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchQueue -Path Orch1:\Shared, Orch1:\Finance -Recurse
```

特定のフォルダとそのサブフォルダからキューを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchQueue -Recurse | Where-Object {$_.Encrypted -eq $true}
```

すべてのフォルダから暗号化されたキューを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchQueue -Recurse | Where-Object {$_.MaxNumberOfRetries -gt 3}
```

3回を超える再試行回数が構成されたキューを取得します。

### Example 7
```powershell
PS Orch1:\> Get-OrchQueue -Recurse -ExportCsv C:\Reports\Queues.csv
```

UTF-8 BOMエンコーディングですべてのキューをCSVにエクスポートします。エクスポートされたCSVは、New-OrchQueueおよびUpdate-OrchQueueコマンドレットを使用してインポートできます。

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
取得するキューの名前を指定します。ワイルドカードと複数の値をサポートします。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および -WarningVariable の共通パラメータをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition
## NOTES
キューエンティティはフォルダスコープです。フォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

キューは、自動化ワークフローとデータ処理を管理するための再試行ポリシー、暗号化、SLA設定、保持ポリシーを含むさまざまな構成をサポートします。

-ExportCsvパラメータは、内部IDの代わりに人間が読める名前を持つインポート対応CSVファイルを作成します。

プライマリエンドポイント: GET /odata/QueueDefinitions
OAuth必須スコープ: OR.Queues または OR.Queues.Read
必要な権限: Queues.View

## RELATED LINKS

[New-OrchQueue](New-OrchQueue.md)

[Update-OrchQueue](Update-OrchQueue.md)

[Remove-OrchQueue](Remove-OrchQueue.md)

[Copy-OrchQueue](Copy-OrchQueue.md)
