---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchBucket

## SYNOPSIS
ストレージバケットを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchBucket [[-Name] <String[]>] [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchBucket コマンドレットは、UiPath Orchestrator テナント内またはテナント間で、ソースフォルダから宛先フォルダにストレージバケットをコピーします。このコマンドレットは、設定とメタデータを含むストレージバケットの完全なコピーを作成します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートしています。ストレージバケットは、異なる環境間での一貫性を保つため、またはバックアップと展開の目的でコピーできます。

-Name パラメーターを使用してコピーするストレージバケットを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、複数のバケットを効率的にコピーするためのワイルドカードパターンをサポートしています。

これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからストレージバケットをコピーし、宛先でフォルダ構造を維持します。

主要エンドポイント: GET /odata/Buckets, POST /odata/Buckets

OAuth 必要スコープ: OR.Administration または OR.Administration.Read または OR.Administration.Write

必要な権限: Buckets.View, Buckets.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchBucket DocumentStorage Orch1:\Production
```

位置パラメーターを使用して、現在のフォルダ（Development）から同じテナント内の Production フォルダに DocumentStorage バケットをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchBucket -Path Orch1:\Development FileStorage Orch2:\Production
```

テナント間バケットコピーを実証して、Orch1:\Development から Orch2:\Production に FileStorage バケットをコピーします。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchBucket *Storage*, *Archive* Orch1:\Production -WhatIf
```

安全のため -WhatIf を使用して、現在のフォルダから Production フォルダに Storage や Archive を含む名前の複数のバケットをコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchBucket -Path Orch1:\Development *Data* Orch2:\Production
```

テナント間コピーでワイルドカードを使用して、Orch1:\Development から Orch2:\Production に名前に Data を含むすべてのバケットをコピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchBucket -Recurse *Backup* Orch2:\Finance -WhatIf
```

すべてのサブフォルダから Backup を含むすべてのバケットを再帰的に Orch2:\Finance にコピーする場合に何が起こるかを表示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchBucket *Shared* | Copy-OrchBucket -Destination Orch2:\Production
```

名前に Shared を含むすべてのバケットを取得し、ワイルドカードフィルタリングでパイプライン入力を使用して Orch2:\Production にコピーします。

## PARAMETERS

### -Destination
宛先フォルダを指定します。

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
コピーするストレージバケットの名前を指定します。

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
コマンドレットを実行した場合に何が起こるかを表示します。
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
ストレージバケットをすべてのサブフォルダから再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Bucket
## NOTES
これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

このコマンドレットは、テナント内とテナント間の両方のコピーをサポートしています。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)

[Remove-OrchBucket](Remove-OrchBucket.md)
