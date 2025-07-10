---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchMachine

## SYNOPSIS
Orchestratorからマシンを削除します。

## SYNTAX

```
Remove-OrchMachine [-Name] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Remove-OrchMachineコマンドレットは、Orchestrator環境からマシンを完全に削除します。マシンは、UiPathロボットをホストし、自動化プロセスを実行できる物理または仮想コンピュータを表します。

マシンが削除されると、そのマシンに関連付けられたすべてのロボットもOrchestratorから削除されます。この操作は元に戻すことができないため、特に自動化実行に積極的に使用されているマシンを削除する前には、慎重に検討する必要があります。

このコマンドレットは、操作をプレビューする-WhatIfや削除前に確認を求める-Confirmなどの安全機能をサポートしています。重要な自動化インフラストラクチャの誤った削除を避けるために、マシンを削除する際にはこれらのパラメータを使用することを強く推奨します。

プライマリ エンドポイント: GET /odata/Machines, DELETE /odata/Machines({machineId})

OAuth 必要なスコープ: OR.Machines または OR.Machines.Write

必要な権限: Machines.Delete

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-OrchMachine TestMachine
```

現在のOrchestrator環境から"TestMachine"という名前のマシンを削除します。

### Example 2
```powershell
PS C:\> Remove-OrchMachine TestMachine1, TestMachine2 -WhatIf
```

指定されたマシンが削除された場合に何が起こるかを実際に削除を実行せずに表示します。

### Example 3
```powershell
PS C:\> Remove-OrchMachine Temp* -Confirm
```

名前が"Temp"で始まるすべてのマシンを削除し、各削除前に確認を求めます。

### Example 4
```powershell
PS C:\> Remove-OrchMachine OldMachine -Path Orch1:, Orch2:
```

複数のOrchestrator環境から"OldMachine"という名前のマシンを削除します。

### Example 5
```powershell
PS C:\> Get-OrchMachine | Where-Object {$_.Type -eq "Template" -and $_.Name -like "*Test*"} | Remove-OrchMachine -WhatIf
```

名前に"Test"を含むテンプレートマシンを特定し、実際に削除を実行せずに削除対象を表示します。

### Example 6
```powershell
PS C:\> Remove-OrchMachine UnusedMachine -Confirm | Out-Null
```

確認プロンプトでマシンを削除し、出力を抑制します。

## PARAMETERS

### -Name
削除するマシンの名前を指定します。パターンマッチング用のワイルドカード文字（*および?）をサポートします。複数の名前が指定された場合、一致するすべてのマシンが削除されます。

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
対象ドライブの名前を指定します。指定しない場合は、現在のドライブが対象となります。このパラメータを使用して、複数のOrchestrator環境から同時にマシンを削除します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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
コマンドレットを実行する前に確認を求めます。操作を元に戻すことができず、それらのマシン上のすべてのロボットに影響するため、マシンを削除する際には特に重要です。

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
コマンドレットを実行した場合に何が起こるかを表示します。コマンドレットは実行されません。実際に操作を実行する前にどのマシンが削除されるかをプレビューするために、このパラメータを使用します。

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
このコマンドレットは共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

このコマンドレットはテナントエンティティに対して動作します。これは、特定のフォルダではなく、Orchestratorテナント全体のマシンに影響することを意味します。

マシンを削除する前に、それらがプロセスやジョブを積極的に実行していないことを確認してください。特定のマシンで実行中のジョブを確認するには、Get-OrchJobを使用できます。

マシンを削除すると、そのマシンに関連付けられたすべてのロボットも削除されます。ロボット構成を保持する必要がある場合は、削除前にCopy-OrchMachineを使用してマシン構成をバックアップすることを検討してください。

テンプレートマシンとシステム生成マシンは、Orchestrator環境の適切な機能に必要な場合があるため、細心の注意を払って削除する必要があります。

複数のマシンに一致する可能性があるワイルドカードパターンを使用する場合は特に、実際の実行前に操作をプレビューするために-WhatIfパラメータを使用してください。

削除操作は元に戻すことができません。マシンが削除されると、復元できないため、必要に応じて再作成する必要があります。

## RELATED LINKS

[Get-OrchMachine](Get-OrchMachine.md)
[New-OrchMachine](New-OrchMachine.md)
[Update-OrchMachine](Update-OrchMachine.md)
[Copy-OrchMachine](Copy-OrchMachine.md)
[Get-OrchRobot](Get-OrchRobot.md)
