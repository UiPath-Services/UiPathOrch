---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Stop-OrchJob

## SYNOPSIS
実行中または待機中のUiPathロボットジョブを停止します。

## SYNTAX

```
Stop-OrchJob [-Id] <Int64[]> [-Force] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Orchestratorフォルダで実行中または待機中のUiPathロボットジョブを停止します。このコマンドレットは、ジョブを正常に停止するか、応答しないジョブを強制終了することができます。

実行中状態のジョブは正常に停止され、現在のアクティビティを完了してクリーンアップを実行できます。待機中状態のジョブは、実行が開始される前にキャンセルされます。

応答しないジョブを強制的に終了するには-Forceパラメータを使用しますが、これにより不完全なクリーンアップやデータの不整合が発生する可能性があります。

主要エンドポイント: POST /odata/Jobs/UiPath.Server.Configuration.OData.StopJobs

OAuth必須スコープ: OR.Jobs

必要な権限: Jobs.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Stop-OrchJob 123456 -WhatIf
```

実際にジョブを停止せずに、ジョブを停止した場合に何が起こるかを表示します。

### Example 2
```powershell
PS Orch1:\Shared> Stop-OrchJob 123456
```

指定されたジョブを正常に停止します。

### Example 3
```powershell
PS Orch1:\> Stop-OrchJob 123456, 789012 -Confirm
```

確認プロンプトを表示して複数のジョブを停止します。

### Example 4
```powershell
PS Orch1:\> Stop-OrchJob 123456 -Force
```

応答しないジョブを強制的に終了します。

### Example 5
```powershell
PS Orch1:\> Get-OrchJob -State Running | Stop-OrchJob -WhatIf
```

パイプライン入力を使用して、どの実行中ジョブが停止されるかを表示します。

### Example 6
```powershell
PS Orch1:\> Get-OrchJob -State Pending -ReleaseName TestProcess | Stop-OrchJob -Confirm
```

特定のプロセスの待機中ジョブを確認付きでキャンセルします。

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

### -Force
正常なシャットダウンなしでジョブを強制終了します。不完全なクリーンアップの原因となる可能性があるため、注意して使用してください。

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

### -Id
停止するジョブのIDを指定します。複数の値をサポートします。

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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

### -Confirm
ジョブを停止する前に確認を求めます。複数のジョブを停止する場合に推奨されます。

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
実際にジョブを停止せずに、コマンドレットを実行した場合に何が起こるかを表示します。安全性確認のために推奨されます。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int64[]
### UiPath.PowerShell.Entities.Job
### System.String[]
## OUTPUTS

### System.Object
## NOTES
ジョブエンティティはフォルダスコープです。フォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

実行中状態のジョブは、-Forceが指定されていない限り正常に停止されます。待機中状態のジョブは即座にキャンセルされます。

特にGet-OrchJobからのパイプライン入力を使用する場合は、実際の実行前に-WhatIfを使用してジョブ終了をプレビューしてください。

ジョブが応答せず正常に停止できない場合にのみ-Forceを使用してください。強制終了により不完全なクリーンアップやデータの不整合が発生する可能性があります。

複数のジョブを停止する場合は、各ジョブの終了を個別に確認するために-Confirmを使用してください。

## RELATED LINKS

[Start-OrchJob](Start-OrchJob.md)

[Get-OrchJob](Get-OrchJob.md)

[Open-OrchJob](Open-OrchJob.md)
