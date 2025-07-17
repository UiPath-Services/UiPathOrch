---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchJobMedia

## SYNOPSIS
指定されたジョブのメディアファイルを削除します。

## SYNTAX

```
Remove-OrchJobMedia [-JobId] <Int64[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchJobMediaコマンドレットは、指定されたジョブIDに関連付けられたメディアファイルを削除します。実行メディアファイルの管理とクリーンアップに使用されます。

プライマリ エンドポイント: POST /odata/ExecutionMedia/UiPath.Server.Configuration.OData.DeleteMediaByJobId

OAuth 必要なスコープ: OR.Monitoring または OR.Monitoring.Write

必要な権限: ExecutionMedia.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-OrchJobMedia -JobId 12345
```

ジョブID 12345に関連付けられたメディアファイルを削除します。

## PARAMETERS

### -Depth
対象フォルダへの再帰処理の深度を指定します。深度0は現在の場所のみを示し、サブフォルダは含まれません。

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

### -JobId
メディアファイルを削除するジョブのIDを指定します。

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
対象フォルダを指定します。指定しない場合は、現在のフォルダが対象となります。

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
操作に対象フォルダとそのすべてのサブフォルダを含める必要があることを指定します。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
