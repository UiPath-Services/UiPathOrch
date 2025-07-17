---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchProcess

## SYNOPSIS
プロセスを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchProcess [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchProcess コマンドレットは、UiPath Orchestrator テナント内または異なるテナント間で、ソースフォルダから宛先フォルダにプロセスをコピーします。このコマンドレットは、構成、パラメーター、メタデータを含むプロセスの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートします。プロセスには自動化ワークフローとその実行構成が含まれており、このコマンドレットは異なる環境間でプロセスをデプロイするために不可欠です。

-Name パラメーターを使用してコピーするプロセスを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のプロセスを効率的にコピーするためのワイルドカードパターンをサポートしています。

これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからプロセスをコピーし、宛先でフォルダ構造を維持できます。

主要エンドポイント: GET /odata/Releases({key})/UiPath.Server.Configuration.OData.GetRelease, GET /odata/Processes/UiPath.Server.Configuration.OData.GetPackageEntryPoints(key='{key}'), POST /odata/Releases

OAuth 必要なスコープ: OR.Execution

必要な権限: Processes.View, Processes.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchProcess InvoiceProcessing Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内のProductionフォルダにInvoiceProcessingプロセスをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchProcess -Path Orch1:\Development EmailAutomation Orch2:\Production
```

Orch1:\DevelopmentからOrch2:\ProductionにEmailAutomationプロセスをコピーし、テナント間プロセスコピーを示しています。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchProcess *Report*, *Invoice* Orch1:\Production -WhatIf
```

安全のため-WhatIfを使用して、現在のフォルダからProductionフォルダにReportまたはInvoiceが含まれる名前の複数のプロセスをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-OrchProcess -Path Orch1:\Development *Automation* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\DevelopmentからOrch2:\ProductionにAutomationが含まれるすべてのプロセスをテナント間コピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchProcess -Recurse *Daily* Orch2:\Finance -WhatIf
```

すべてのサブフォルダからDailyが含まれるプロセスを再帰的にOrch2:\Financeにコピーする場合に何が起こるかを示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchProcess *Scheduled* | Copy-OrchProcess -Destination Orch2:\Production
```

Scheduledが含まれる名前のすべてのプロセスを取得し、パイプライン入力を使用してOrch2:\Productionにコピーします。

## PARAMETERS

### -Destination
プロセスをコピーする宛先フォルダを指定します。

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
コピーするプロセスの名前を指定します。

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
すべてのサブフォルダからプロセスを再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Release
## NOTES
これはフォルダエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

プロセスには自動化ワークフローと実行構成が含まれます。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchProcess](Get-OrchProcess.md)

[Remove-OrchProcess](Remove-OrchProcess.md)

[Start-OrchProcess](Start-OrchProcess.md)

[Set-OrchProcess](Set-OrchProcess.md)
