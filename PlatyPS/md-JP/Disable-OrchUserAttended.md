---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchUserAttended

## SYNOPSIS
指定されたユーザーの有人ロボットセッション機能を無効にします。

## SYNTAX

```
Disable-OrchUserAttended [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchUserAttendedコマンドレットは、UiPath Orchestrator内の指定されたユーザーの有人ロボットセッション機能を無効にします。有人ロボットはユーザーのデスクトップで実行され、手動またはユーザーの操作によってトリガーされます。これは、サーバーマシンで自動実行される無人ロボットとは対照的です。

有人ロボット機能を無効にすると、ユーザーはロボットセッションを確立できなくなり、ローカルマシンで有人自動化プロセスを実行できなくなります。これにより、有人自動化実行が効果的にブロックされますが、ロール権限に基づく、ジョブの表示、共有フォルダへのアクセス、アセットの管理などの他のOrchestrator機能へのユーザーアクセスは維持されます。

内部的には、このコマンドレットは-MayHaveRobotSession Falseパラメータを使用してUpdate-OrchUserを呼び出します。このターゲットアプローチは、個人ワークスペースアクセスや一般的なOrchestrator機能などの他のユーザー権限に影響を与えることなく、ロボットセッション機能を特に制御します。

このコマンドレットはテナントレベルで動作し、組織全体のユーザーロボットセッション機能を管理します。Personal Workspaceの対応機能とは異なり、このコマンドレットは自動的にキャッシュをクリアしないため、ロボットセッション制御のみに焦点を当てたより軽量な操作になります。

主要エンドポイント: PATCH /odata/Users

OAuth必要スコープ: OR.Users または OR.Users.Write

必要な権限: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Disable-OrchUserAttended john.doe
```

現在のテナントのユーザーjohn.doeの有人ロボットセッション機能を無効にします。

### Example 2
```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1: jane.smith -WhatIf
```

Orch1テナントのjane.smithの有人機能を無効にする場合の動作を表示します。

### Example 3
```powershell
PS Orch1:\> Disable-OrchUserAttended contractor1, contractor2, contractor3
```

現在のテナントの複数の契約者ユーザーの有人ロボット機能を無効にします。

### Example 4
```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1: *external* -Confirm
```

確認プロンプトを表示して、ユーザー名に"external"が含まれるすべてのユーザーの有人機能を無効にします。

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Type External | Disable-OrchUserAttended -WhatIf
```

すべての外部ユーザーを取得し、パイプライン入力を使用して有人機能を無効にする場合の動作を表示します。

### Example 6
```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1:, Orch2: viewer.user1, viewer.user2
```

複数のテナントにわたってビューアーユーザーの有人機能を無効にします。

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

### -Path
ターゲットテナントドライブを指定します。指定されない場合、現在のテナントが対象になります。複数のテナントを対象とするテナントレベル操作用です。

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

### -UserName
有人ロボット機能を無効にするユーザーのユーザー名を指定します。一括操作用のワイルドカードパターンをサポートします。

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

### System.String[]

## OUTPUTS

### System.Object
## NOTES
このコマンドレットは、指定されたユーザーのMayHaveRobotSessionプロパティをFalseに設定するテナントレベルエンティティ操作です。有人ロボット機能により、ユーザーはユーザーの操作を伴うデスクトップマシンで自動化プロセスを実行できます。これらの機能を無効にすると、ロボットセッションの確立と有人自動化実行が防止されますが、他のユーザー権限は保持されます。このコマンドレットは個人ワークスペース権限とは独立して動作し、Orchestratorキャッシュを自動的にクリアしません。

## RELATED LINKS

[Enable-OrchUserAttended](Enable-OrchUserAttended.md)

[Get-OrchUser](Get-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)

[Get-OrchMachineSession](Get-OrchMachineSession.md)
