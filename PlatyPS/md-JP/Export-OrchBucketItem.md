---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Export-OrchBucketItem

## SYNOPSIS
UiPath Orchestrator ストレージバケットからローカルファイルシステムにバケットアイテムをエクスポートします。

## SYNTAX

```
Export-OrchBucketItem [[-Name] <String[]>] [[-FullPath] <String[]>] [[-Destination] <String>] [-Path <String>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Export-OrchBucketItem コマンドレットは、UiPath Orchestrator ストレージバケットからローカルファイルシステムにファイルやフォルダーをエクスポートします。名前、フルパスで特定のアイテムをエクスポートしたり、バケットからすべてのアイテムをエクスポートできます。このコマンドレットはエクスポート中にフォルダー構造を保持し、複数のフォルダーやバケットにわたる再帰的操作をサポートします。

主要エンドポイント: GET /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetReadUri?path={fullPath}

OAuth必要スコープ: OR.Administration

必要な権限: Buckets.View および BlobFiles.View

## EXAMPLES

### Example 1:
```powershell
PS Orch1:\Shared> Export-OrchBucketItem
```

現在のフォルダー内のすべてのバケットからすべてのファイルを、ローカルドライブの現在のディレクトリにエクスポートします。

### Example 2:
```powershell
PS Orch1:\Shared> Export-OrchBucketItem MyBucket
```

現在のフォルダー内のTestBucketバケットからすべてのファイルを、ローカルドライブの現在のディレクトリにエクスポートします。

### Example 3:
```powershell
PS Orch1:\Shared> Export-OrchBucketItem * Book1.csv, Untitled.txt c:\tmp\backup
```

現在のフォルダー内のすべてのバケットから指定されたファイルを、指定されたローカルディレクトリにエクスポートします。

### Example 4:
```powershell
PS C:\> Export-OrchBucketItem -Path Orch1:\ -Recurse -Destination c:\tmp\backup
```

Sharedフォルダーとそのサブフォルダー内のすべてのバケットからすべてのファイルを、指定されたローカルディレクトリにエクスポートします。

### Example 5:
```powershell
PS Orch1:\Shared> Export-OrchBucketItem * *.csv -WhatIf
```

実際のエクスポートを実行せずに、SharedフォルダーのすべてのバケットからエクスポートされるCSVファイルを表示します。

## PARAMETERS

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

### -Depth
エクスポート操作に含むサブフォルダーの最大深度を指定します。-Recurseと組み合わせて使用する場合のみ適用されます。

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
バケットアイテムがエクスポートされるローカルディレクトリパスを指定します。指定されない場合、アイテムはローカルドライブの現在のディレクトリにエクスポートされます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -FullPath
それぞれのバケット内でエクスポートするバケットアイテムのフルパスを指定します（例："folder/file.txt" または "file.txt"）。FullPathにはバケット名自体は含まれません。ワイルドカード文字をサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
エクスポートするバケットアイテムの名前を指定します。ワイルドカード文字をサポートし、対象場所のすべてのバケットで複数のアイテムにマッチします。

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
バケットを検索するUiPath Orchestratorパスを指定します。このパラメーターを使用して特定のフォルダーまたはドライブの場所を対象にします。

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

### -Recurse
コマンドレットが現在の場所とすべてのサブフォルダーのバケットから再帰的にアイテムをエクスポートすることを示します。

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

### -ProgressAction
コマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。

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

### CommonParameters
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメーターをサポートします。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
### System.String

## OUTPUTS

### System.Object

## NOTES
エクスポートされたファイルは、宛先ディレクトリ内で元のバケットフォルダー構造を維持します。複数のバケットからエクスポートする場合、各バケットは宛先パス内に独自のサブディレクトリを作成します。

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)
[Get-OrchBucketItem](Get-OrchBucketItem.md)
[Import-OrchBucketItem](Import-OrchBucketItem.md)
[Remove-OrchBucketItem](Remove-OrchBucketItem.md)
