---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTrigger

## SYNOPSIS
トリガーを取得します。

## SYNTAX

```
Get-OrchTrigger [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExpandDetails]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
対象フォルダ内の指定された名前を持つトリガーについての情報を出力します。対象フォルダは-Path、-Recurse、-Depthパラメータを使用して指定できます。これらが指定されていない場合、現在の場所が対象フォルダとして使用されます。トリガー名が指定されていない場合、対象フォルダ内のすべてのトリガーを出力します。

-Pathと-Nameパラメータに対しては、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

-Path、-Recurse、-Depthパラメータを指定する際は、コマンドレット名の直後に配置してください。この配置により、後続のパラメータの自動補完が正しく機能します。

プライマリ エンドポイント: GET /odata/ProcessSchedules, GET /odata/ProcessSchedules({processScheduleId})

OAuth 必要スコープ: OR.Jobs または OR.Jobs.Read

必要な権限: Schedules.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTrigger
```

現在の場所である「Shared」フォルダ内のすべてのトリガーを表示します。

### Example 2
```powershell
PS Orch1:\> Get-OrchTrigger -Recurse
```

現在のフォルダとそのすべてのサブフォルダ内のすべてのトリガーを表示します。ルートフォルダで実行すると、そのテナント内のすべてのフォルダのトリガーが表示されます。

### Example 3
```powershell
PS C:\> Get-OrchTrigger -Path Orch1:\ -Recurse *Schedule*
```

すべてのフォルダから再帰的に、名前に「Schedule」を含むトリガーを取得します。

### Example 4
```powershell
PS Orch1:\Shared> Get-OrchTrigger | select Path,Id,Name
```

選択された列のみで出力を表示します。ワイルドカードを含め、複数の列をカンマで区切って指定します。列名は[Ctrl+Space]または[Tab]で自動補完できます。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchTrigger | ConvertTo-Json
```

出力をJSON形式に変換し、Orchestratorからのデータの生のビューを提供します。

## PARAMETERS

### -Depth
対象フォルダへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダは含まれません。

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
取得するトリガーの名前を指定します。

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
対象フォルダを指定します。指定されていない場合、現在のフォルダが対象となります。

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

### -Recurse
操作が対象フォルダとそのすべてのサブフォルダを含むべきことを指定します。

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
{{ Fill CsvEncoding Description }}

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
{{ Fill ExportCsv Description }}

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
コマンドレットにGET /odata/ProcessSchedules({processScheduleId})を呼び出すよう指示します。

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
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメータをサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule
## NOTES
呼び出されるメインエンドポイント: GET /odata/ProcessSchedules

必要スコープ: OR.Jobs.Read



プライマリ エンドポイント: GET /odata/ProcessSchedules
OAuth 必要スコープ: OR.Jobs または OR.Jobs.Read
必要な権限: Schedules.View

## RELATED LINKS
