---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchFolderMachine

## SYNOPSIS
フォルダからマシンの割り当てを解除します。

## SYNTAX

```
Remove-OrchFolderMachine [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

プライマリ エンドポイント: POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth 必要なスコープ: OR.Folders または OR.Folders.Write

必要な権限: (Units.Edit または SubFolders.Edit - 任意のフォルダ関連付けにマシンを更新する場合、または提供されたすべてのフォルダでユーザーがSubFolders.Edit権限を持つ場合のみ)

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
削除するフォルダマシンの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
