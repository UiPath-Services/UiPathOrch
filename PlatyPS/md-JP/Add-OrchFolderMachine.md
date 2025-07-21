---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchFolderMachine

## SYNOPSIS
マシンをフォルダーに割り当てます。

## SYNTAX

```
Add-OrchFolderMachine [-Name] <String[]> [-PropagateToSubFolders <String>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Add-OrchFolderMachine コマンドレットは、UiPath Orchestrator テナント内の特定のフォルダーにマシンを割り当てます。このコマンドレットを使用すると、フォルダー構造内でマシンを整理し、プロセス実行のために特定のマシンにアクセスできるフォルダーを制御できます。

このコマンドレットは、複数のフォルダーに同時にマシンを割り当てることをサポートし、指定された場合はマシンの割り当てをサブフォルダーに伝播できます。このコマンドレットを使用して、フォルダー階層で整理された異なるチーム、プロジェクト、または環境間でのマシン割り当てを管理します。

-Name パラメーターを使用して割り当てるマシンを指定し、-Path パラメーターを使用して対象フォルダーを指定します。-PropagateToSubFolders パラメーターは、マシンの割り当てをすべてのサブフォルダーが継承するかどうかを制御します。

これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。-Recurse パラメーターは、すべてのサブフォルダーにマシンを割り当てることを可能にし、フォルダー構造全体で一貫したマシンの可用性を維持します。

主要エンドポイント: POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth 必要スコープ: OR.Folders

必要なアクセス許可: (Units.Edit または SubFolders.Edit - 任意のフォルダーの関連付けにマシンを更新する、またはユーザーが提供されたすべてのフォルダーで SubFolders.Edit 権限を持っている場合のみ)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Add-OrchFolderMachine Robot01
```

Robot01 マシンを現在のフォルダー（Production）に割り当てます。

### Example 2
```powershell
PS C:\> Add-OrchFolderMachine -Path Orch1:\Development Robot01, Robot02
```

Robot01 および Robot02 マシンを Development フォルダーに割り当てます。

### Example 3
```powershell
PS Orch1:\Finance> Add-OrchFolderMachine Bot* -PropagateToSubFolders True
```

Bot で始まる名前のすべてのマシンを Finance フォルダーに割り当て、その割り当てをすべてのサブフォルダーに伝播します。

### Example 4
```powershell
PS C:\> Add-OrchFolderMachine -Path Orch1:\Development, Orch1:\Testing SharedBot -WhatIf
```

SharedBot マシンを Development と Testing の両方のフォルダーに割り当てる際の動作を表示します。

### Example 5
```powershell
PS Orch1:\> Add-OrchFolderMachine -Recurse ProductionBot01 -PropagateToSubFolders True
```

現在の場所からすべてのフォルダーに再帰的に ProductionBot01 を割り当て、それらのサブフォルダーに伝播します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchMachine Template* | Add-OrchFolderMachine -PropagateToSubFolders False
```

Template で始まる名前のすべてのマシンを取得し、サブフォルダーに伝播せずに現在のフォルダーに割り当てます。

## PARAMETERS

### -Depth
-Recurse パラメーター使用時に含めるサブフォルダーレベルの最大数を指定します。

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

### -Name
フォルダーに割り当てるマシンの名前を指定します。

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

### -Path
マシンを割り当てる対象フォルダーを指定します。

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
指定されたパスからすべてのサブフォルダーに再帰的にマシンを割り当てることを指定します。

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
コマンドレットを実行した場合の動作を表示します。
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

### -PropagateToSubFolders
マシンの割り当てをすべてのサブフォルダーに伝播するかどうかを指定します。有効な値は True と False です。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダーエンティティコマンドレットです。最初に Set-Location コマンドレット（cd コマンド）を使用して対象フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用して対象フォルダーを指定してください。

-PropagateToSubFolders パラメーターは継承動作を制御します。マシンの割り当てを実行する前に、-WhatIf を使用して変更を事前に確認してください。

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)

[Remove-OrchFolderMachine](Remove-OrchFolderMachine.md)

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)
