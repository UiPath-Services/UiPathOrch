---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchQueue

## SYNOPSIS
キューを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchQueue [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchQueue コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースフォルダから宛先フォルダにキューをコピーします。このコマンドレットは、構成、スキーマ、メタデータを含むキューの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。キューは自動化プロセス内での作業項目の管理に使用され、複数のロボット間でワークロードを分散する方法を提供します。

-Name パラメーターを使用してコピーするキューを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のキューを効率的にコピーするためのワイルドカードパターンをサポートしています。キューをコピーすると、その構造と構成はコピーされますが、キューアイテム自体はコピーされないことに注意してください。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからキューをコピーし、宛先でフォルダ構造を維持できます。

プライマリエンドポイント: GET /odata/QueueDefinitions, GET /odata/QueueDefinitions/UiPath.Server.Configuration.OData.GetFoldersForQueue(id={id}), POST /odata/QueueDefinitions

OAuth 必要なスコープ: OR.Queues

必要な権限: Queues.View, Queues.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchQueue InvoiceQueue Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内のProductionフォルダにInvoiceQueueをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchQueue -Path Orch1:\Development ProcessingQueue Orch2:\Production
```

Orch1:\DevelopmentからOrch2:\ProductionにProcessingQueueをコピーし、テナント間キューコピーを示しています。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchQueue *Invoice*, *Report* Orch1:\Production -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダからProductionフォルダにInvoiceまたはReportが含まれる名前の複数のキューをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchQueue -Path Orch1:\Development *Processing* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\DevelopmentからOrch2:\ProductionにProcessingが含まれるすべてのキューをテナント間コピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchQueue -Recurse *Daily* Orch2:\Finance -WhatIf
```

すべてのサブフォルダからDailyが含まれるキューを再帰的にOrch2:\Financeにコピーする場合に何が起こるかを示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchQueue *Batch* | Copy-OrchQueue -Destination Orch2:\Production
```

Batchが含まれる名前のすべてのキューを取得し、パイプライン入力を使用してOrch2:\Productionにコピーします。

## PARAMETERS

### -Destination
キューをコピーする宛先フォルダを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
コピーするキューの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソースフォルダを指定します。指定しない場合、現在のフォルダがソースとして使用されます。

```yaml
Type: String
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

### -Confirm
コマンドレットを実行する前に確認を求めます。

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
コマンドレットを実行した場合の動作を示します。
コマンドレットは実行されません。

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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

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

### -Recurse
すべてのサブフォルダからキューを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

このコマンドレットはキューの構造と構成のみをコピーします。キューアイテムはコピーされません。必要に応じて、特定のキューアイテムをコピーするには Copy-OrchQueueItem を使用してください。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchQueue](Get-OrchQueue.md)

[Remove-OrchQueue](Remove-OrchQueue.md)

[Set-OrchQueue](Set-OrchQueue.md)

[Copy-OrchQueueItem](Copy-OrchQueueItem.md)
