---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchFolderUser

## SYNOPSIS
フォルダからユーザーの割り当てを解除します。

## SYNTAX

```
Remove-OrchFolderUser [[-UserName] <String[]>] [[-FullName] <String[]>] [-NoMatchWarning] [-Path <String[]>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

プライマリ エンドポイント: POST /odata/Folders({folderId})/UiPath.Server.Configuration.OData.RemoveUserFromFolder

OAuth 必要なスコープ: OR.Folders または OR.Folders.Write

必要な権限: (Units.Edit または SubFolders.Edit - 任意のフォルダからユーザーを削除する場合、または呼び出し元が提供されたフォルダでSubFolders.Edit権限を持つ場合のみ)

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -UserName
割り当てを解除するユーザーのユーザー名を指定します。

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

### -FullName
割り当てを解除するユーザーのフルネームを指定します。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
