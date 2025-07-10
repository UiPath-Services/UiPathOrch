---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchFolderMachineInherit

## SYNOPSIS
FolderMachineの継承を有効にして、サブフォルダに伝播できるようにします。

## SYNTAX

```
Enable-OrchFolderMachineInherit [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
指定されたFolderMachineの継承を有効にし、PropagateToSubFoldersプロパティをtrueに設定します。これにより、マシン構成がフォルダ階層内のサブフォルダに継承されるようになります。

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.ToggleFolderMachineInherit

OAuth required scopes: OR.Folders or OR.Folders.Write

Required permissions: Units.Edit or SubFolders.Edit

このコマンドレットはフォルダエンティティで動作し、ターゲットフォルダへの移動または-Pathパラメーターを使用したターゲットフォルダの指定が必要です。

## EXAMPLES

### Example 1
`powershell
PS Orch1:\Production> Enable-OrchFolderMachineInherit ProductionMachine
`

現在のフォルダで「ProductionMachine」という名前のマシンの継承を有効にし、サブフォルダに伝播できるようにします。

### Example 2
`powershell
PS C:\> Enable-OrchFolderMachineInherit -Path Orch1:\Production ProductionMachine
`

現在の場所を変更せずに、ProductionフォルダでProductionMachineという名前のマシンの継承を有効にします。

### Example 3
`powershell
PS C:\> Enable-OrchFolderMachineInherit -Path Orch1:\Development Machine1, Machine2 -WhatIf
`

複数のマシンの継承を有効にする場合の動作を、実際にコマンドを実行せずに表示します。

### Example 4
`powershell
PS Orch1:\Production> Get-OrchFolderMachine ProductionMachine | ConvertTo-Json | Select-String PropagateToSubFolders
`

コマンドレットの実行後にPropagateToSubFoldersプロパティを確認して、現在の継承状態を検証します。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認メッセージを表示します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -Name
継承を有効にするFolderMachineの名前を指定します。このパラメーターは、パターンマッチングのためのワイルドカード文字（*および?）を受け入れます。これは位置パラメーターのため、パラメーター名を省略できます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
FolderMachineを検索するフォルダパスを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。現在の場所を変更せずに特定のフォルダを対象にしたい場合に、このパラメーターを使用します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -WhatIf
コマンドレットが実行された場合に何が起こるかを表示します。コマンドレットは実行されません。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -ProgressAction
{{ Fill ProgressAction Description }}

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
## OUTPUTS

### System.Object
## NOTES

このコマンドレットはフォルダエンティティで動作し、次のいずれかが必要です：
- Set-Location（cd）を使用してターゲットフォルダに移動、または
- -Pathパラメーターを使用してターゲットフォルダを指定

**重要：** 最適なPowerShell IntelliSenseサポートのために、両方のパラメーターを使用する場合は、Nameパラメーターの前に-Pathを指定してください。

PropagateToSubFoldersプロパティは、FolderMachine構成がサブフォルダに継承されるかどうかを決定します。有効になると、フォルダ階層内のサブフォルダがこのマシン構成を継承します。

PropagateToSubFoldersプロパティを確認して現在の継承状態を確認するには、Get-OrchFolderMachineを使用します。

安全のため、実行前に-WhatIfを使用して変更をプレビューすることを検討してください。

## RELATED LINKS

[Disable-OrchFolderMachineInherit](Disable-OrchFolderMachineInherit.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)
