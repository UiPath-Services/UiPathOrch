---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchJobMedia

## SYNOPSIS
指定したフォルダーからジョブメディアファイルをローカルファイルにエクスポートします。

## SYNTAX

```
Export-OrchJobMedia [[-JobId] <Int64[]>] [[-Destination] <String>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Export-OrchJobMedia コマンドレットは、UiPath Orchestratorフォルダーからジョブ実行に関連するメディアファイル（スクリーンショット、録画、ログ）をローカルストレージにエクスポートします。このコマンドレットは、トラブルシューティング、コンプライアンス、または監査目的でジョブ実行アーティファクトのバックアップ、アーカイブ、または詳細分析を可能にします。

ジョブメディアには、実行スクリーンショット、動画録画、実行ログ、および自動化ジョブ実行中に生成されるその他の診断ファイルが含まれます。これらのファイルをエクスポートすることで、オフライン分析、長期アーカイブ、またはトラブルシューティング目的でサポートチームと共有できます。

-JobIdパラメーターを使用して、エクスポートするジョブのメディアファイルを指定します。-Destinationパラメーターは、エクスポートされたメディアファイルの保存先を指定します。このコマンドレットは、複数のジョブからのメディアエクスポートをサポートし、宛先ディレクトリでジョブIDごとにファイルを整理します。

これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定します。-Recurseパラメーターは、すべてのサブフォルダーからジョブメディアをエクスポートします。

主要エンドポイント: [PLACEHOLDER - 具体的なAPIエンドポイント]

必要なOAuthスコープ: OR.Jobs または OR.Jobs.Read

必要な権限: Jobs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Export-OrchJobMedia 12345
```

現在のフォルダーからジョブID 12345のすべてのメディアファイルをデフォルトの宛先にエクスポートします。

### Example 2
```powershell
PS C:\> Export-OrchJobMedia -Path Orch1:\Production -JobId 67890 -Destination "C:\JobMedia"
```

ProductionフォルダーからジョブID 67890のすべてのメディアファイルをC:\JobMediaディレクトリにエクスポートします。

### Example 3
```powershell
PS Orch1:\Development> Export-OrchJobMedia 11111, 22222, 33333 -Destination "C:\Exports" -WhatIf
```

現在のフォルダーから複数のジョブIDのメディアファイルをC:\Exportsディレクトリにエクスポートした場合の動作を表示します。

### Example 4
```powershell
PS C:\> Export-OrchJobMedia -Path Orch1:\Development -Recurse -Destination "C:\AllJobMedia"
```

DevelopmentフォルダーとすべてのサブフォルダーからすべてのジョブメディアファイルをC:\AllJobMediaディレクトリにエクスポートします。

### Example 5
```powershell
PS Orch1:\Production> Export-OrchJobMedia -Depth 2 -Destination "C:\BackupMedia" -Confirm
```

現在のフォルダーと最大2レベルのサブフォルダーからすべてのジョブメディアファイルを確認プロンプトとともにC:\BackupMediaにエクスポートします。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchJob -State Faulted | Export-OrchJobMedia -Destination "C:\FailedJobs"
```

すべての失敗したジョブを取得し、トラブルシューティング目的でパイプライン入力を使用してそれらのメディアファイルをC:\FailedJobsにエクスポートします。

## PARAMETERS

### -Depth
-Recurseパラメーターを使用するときに含めるサブフォルダーレベルの最大数を指定します。

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

### -Destination
エクスポートされたメディアファイルが保存される宛先ディレクトリを指定します。指定しない場合、ファイルは現在のディレクトリに保存されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -JobId
メディアファイルをエクスポートするジョブIDを指定します。指定しない場合、すべてのジョブのメディアファイルがエクスポートされます。

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ソースフォルダーを指定します。指定しない場合、現在のフォルダーがソースとして使用されます。

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
すべてのサブフォルダーからジョブメディアファイルを再帰的にエクスポートすることを指定します。

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

### -Confirm
コマンドレットの実行前に確認を求めます。

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
コマンドレットが実行された場合の動作を表示します。
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

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初にSet-Locationコマンドレット（cdコマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダーを指定します。

ジョブメディアファイルには、スクリーンショット、動画録画、実行ログが含まれます。大きなメディアファイルや複数のジョブでは、エクスポート操作に時間がかかる場合があります。メディアエクスポート用の十分なディスク容量を確保してください。メディアファイルは宛先ディレクトリでジョブIDごとに整理されます。実際の実行前のテストには-WhatIfを使用してください。

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchJobMedia](Get-OrchJobMedia.md)

[Remove-OrchJobMedia](Remove-OrchJobMedia.md)

[Start-OrchJob](Start-OrchJob.md)
