---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchPersonalWorkspace

## SYNOPSIS
UiPath Orchestratorからパーソナルワークスペースフォルダを削除します。

## SYNTAX

```
Remove-OrchPersonalWorkspace [[-Name] <String[]>] [[-OwnerName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchPersonalWorkspaceコマンドレットは、UiPath Orchestratorからパーソナルワークスペースフォルダを完全に削除します。パーソナルワークスペースは、ユーザーが自動化プロジェクトの開発、テスト、および管理を行うための個別環境を提供する専用フォルダです。

このコマンドレットは、実際のフォルダ構造と、パーソナルワークスペース内に保存されているプロセス、アセット、その他の自動化成果物を含むすべての含有コンテンツを削除します。この操作は元に戻すことができず、ワークスペースデータを完全に削除します。

このコマンドレットはOrchestrator内のフォルダレベルで動作し、名前または所有者によって特定のパーソナルワークスペースフォルダをターゲットにします。-Nameパラメータを使用してワークスペースフォルダ名を直接指定するか、-OwnerNameパラメータを使用して所有ユーザーでワークスペースをターゲットにします。両方のパラメータは一括操作用のワイルドカードパターンをサポートします。

これは、適切なフォルダパスへのナビゲーションまたは-Pathパラメータを使用したターゲットパスの指定を必要とするフォルダレベルエンティティ操作です。この操作には、Orchestrator環境内でフォルダを削除するための適切な権限が必要です。

プライマリ エンドポイント: GET /odata/PersonalWorkspaces, DELETE /odata/Folders({folderId})

OAuth 必要なスコープ: OR.Folders

必要な権限: Units.View、(Units.Delete または SubFolders.Delete)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace -OwnerName john.doe -WhatIf
```

john.doeが所有するパーソナルワークスペースを削除する際に何が起こるかを表示します。

### Example 2
```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace -Name "john.doe Personal Workspace" -Confirm
```

確認プロンプトで特定のパーソナルワークスペースフォルダを削除します。

### Example 3
```powershell
PS C:\> Remove-OrchPersonalWorkspace -Path Orch1: -OwnerName temp.user1, temp.user2 -WhatIf
```

Orch1テナント内の複数の一時ユーザーのパーソナルワークスペースを削除する際に何が起こるかを表示します。

### Example 4
```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace -OwnerName *contractor* -Confirm
```

ユーザー名に"contractor"を含むすべてのユーザーのパーソナルワークスペースを確認プロンプトで削除します。

### Example 5
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace | Where-Object {$_.LastModified -lt (Get-Date).AddDays(-90)} | Remove-OrchPersonalWorkspace -WhatIf
```

過去90日間変更されていないパーソナルワークスペースを削除する際に何が起こるかを表示します。

### Example 6
```powershell
PS C:\> Remove-OrchPersonalWorkspace -Path Orch1:, Orch2: -OwnerName inactive.user1 -Confirm
```

複数のテナントにわたってinactive.user1のパーソナルワークスペースを確認付きで削除します。

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
削除するパーソナルワークスペースフォルダの名前を指定します。一括操作用のワイルドカードパターンをサポートします。

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

### -OwnerName
削除するパーソナルワークスペースフォルダの所有者名（ユーザー名）を指定します。一括操作用のワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: UserName

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象テナントドライブを指定します。指定しない場合は、現在のドライブが対象となります。パス指定を必要とするフォルダレベル操作用です。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PersonalWorkspace
## NOTES
このコマンドレットは、パーソナルワークスペースフォルダとすべての含有コンテンツを完全に削除するフォルダレベルエンティティ操作を実行します。この操作は元に戻すことができず、適切なフォルダ削除権限が必要です。パーソナルワークスペースは、ユーザーに専用の開発環境を提供します。特にワイルドカードパターンを使用する場合は、実行前に操作をプレビューするために常に-WhatIfを使用してください。この操作には、フォルダ階層に応じてUnits.DeleteまたはSubFolders.Delete権限が必要です。

## RELATED LINKS

[Get-OrchPersonalWorkspace](Get-OrchPersonalWorkspace.md)

[Enable-OrchPersonalWorkspace](Enable-OrchPersonalWorkspace.md)

[Disable-OrchPersonalWorkspace](Disable-OrchPersonalWorkspace.md)

[Get-OrchFolder](Get-OrchFolder.md)
