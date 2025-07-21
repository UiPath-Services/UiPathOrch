---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchFolderMachineInherit

## SYNOPSIS
FolderMachineの継承を無効化し、サブフォルダーへの伝播を防ぎます。

## SYNTAX

```
Disable-OrchFolderMachineInherit [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
指定されたFolderMachineの継承を無効化し、PropagateToSubFoldersプロパティをfalseに設定します。これにより、マシン構成がフォルダー階層内のサブフォルダーに継承されることを防ぎます。

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.ToggleFolderMachineInherit

OAuth required scopes: OR.Folders または OR.Folders.Write

Required permissions: (Units.Edit または SubFolders.Edit - マシンのサブフォルダーへの伝播は、Units.Edit権限が提供されている場合、または提供されたすべてのフォルダーにSubFolders.Edit権限がある場合のみ)

このコマンドレットはフォルダーエンティティを操作するため、対象フォルダーへの移動か、-Pathパラメータを使用した対象フォルダーの指定が必要です。

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Disable-OrchFolderMachineInherit ProductionMachine
```

現在のフォルダーにある「ProductionMachine」という名前のマシンの継承を無効化し、サブフォルダーへの伝播を防ぎます。

### Example 2
```powershell
PS C:\> Disable-OrchFolderMachineInherit -Path Orch1:\Development TestMachine
```

現在の場所を変更することなく、Developmentフォルダーにある「TestMachine」という名前のマシンの継承を無効化します。

### Example 3
```powershell
PS C:\> Disable-OrchFolderMachineInherit -Path Orch1:\Production Machine1, Machine2 -WhatIf
```

実際にコマンドを実行することなく、複数のマシンの継承を無効化した場合に何が起こるかを表示します。

### Example 4
```powershell
PS Orch1:\Production> Get-OrchFolderMachine ProductionMachine | ConvertTo-Json | Select-String PropagateToSubFolders
```

コマンドレット実行後にPropagateToSubFoldersプロパティを確認して、現在の継承状態を検証します。

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

### -Name
継承を無効化するFolderMachineの名前を指定します。このパラメータはパターンマッチング用のワイルドカード文字（*と?）を受け入れます。これは位置パラメータなので、パラメータ名を省略できます。

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
FolderMachineを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（*と?）をサポートします。現在の場所を変更することなく特定のフォルダーを対象にしたい場合にこのパラメータを使用します。

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

### -WhatIf
コマンドレットを実行した場合に何が起こるかを表示します。コマンドレットは実行されません。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

このコマンドレットはフォルダーエンティティを操作するため、以下のいずれかが必要です：
- Set-Location (cd)を使用した対象フォルダーへの移動、または
- -Pathパラメータを使用した対象フォルダーの指定

**重要:** PowerShell IntelliSenseサポートを最適化するために、両方のパラメータを使用する場合は-PathをNameパラメータの前に指定してください。

PropagateToSubFoldersプロパティは、FolderMachine構成がサブフォルダーに継承されるかどうかを決定します。無効化すると、フォルダー階層内のサブフォルダーはこのマシン構成を継承しません。

PropagateToSubFoldersプロパティを確認することで、現在の継承状態を検証するにはGet-OrchFolderMachineを使用します。

安全のため、実行前に-WhatIfを使用して変更をプレビューすることを検討してください。

## RELATED LINKS

[Enable-OrchFolderMachineInherit](Enable-OrchFolderMachineInherit.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)
