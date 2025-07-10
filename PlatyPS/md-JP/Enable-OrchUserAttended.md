---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchUserAttended

## SYNOPSIS
指定したユーザーの有人自動化を有効にします。

## SYNTAX

```
Enable-OrchUserAttended [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchUserAttended コマンドレットは、Orchestrator環境で指定したユーザーの有人自動化機能を有効にします。有人自動化により、ユーザーはロボットと協調して作業し、ユーザーの対話と監督を維持しながらユーザーセッションでプロセスを実行できます。

有効にすると、ユーザーは有人モードでロボットを実行できるようになります。これは、ロボットがユーザーのアクティブなセッションで実行され、ユーザーがアクセス権を持つアプリケーションやインターフェースと対話できることを意味します。これは、プロセス実行中に人間の監視や対話が必要なシナリオで特に有用です。

このコマンドレットは、有人自動化を有効にするユーザーを指定するためにUserNameパラメーターが必要です。複数のユーザーを同時に指定できます。この操作は、Orchestratorテナント内でのユーザーの権限と機能に影響します。

この設定は、ユーザーとロボットが協力してビジネスプロセスを効率的に完了する有人自動化シナリオを実装する組織にとって重要です。

主要エンドポイント: GET /odata/Users, GET /odata/Users({userId}), PUT /odata/Users({userId})

必要なOAuthスコープ: OR.Users

必要な権限: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> Enable-OrchUserAttended john.doe
```

ユーザー "john.doe" の有人自動化を有効にします。ユーザー名に位置パラメーターを使用します。

### Example 2
```powershell
PS C:\> Enable-OrchUserAttended -UserName alice.smith, bob.jones
```

複数のユーザーの有人自動化を同時に有効にします。これにより、alice.smithとbob.jonesの両方が有人モードでロボットを実行できるようになります。

### Example 3
```powershell
PS C:\> Enable-OrchUserAttended -UserName marketing.team -Path Orch1:, Orch2:
```

複数のOrchestratorインスタンスでmarketing.teamユーザーの有人自動化を有効にします。マルチテナント環境で有用です。

### Example 4
```powershell
PS C:\> Enable-OrchUserAttended -UserName admin.user -WhatIf
```

実際に変更を行わずに、admin.userに有人自動化を有効にした場合の動作を表示します。実行前の確認に有用です。

### Example 5
```powershell
PS C:\> Get-OrchUser -UserName *automation* | Enable-OrchUserAttended -Confirm
```

ユーザー名に"automation"が含まれるすべてのユーザーを検索し、各ユーザーの確認プロンプトとともに有人自動化を有効にします。

## PARAMETERS

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

### -Path
対象のフォルダーを指定します。指定しない場合、現在のフォルダーが対象になります。

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
有人自動化を有効にするユーザー名を指定します。複数のユーザーの有人自動化を同時に有効にするために、複数のユーザー名を指定できます。

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
このコマンドレットによって生成される進捗更新に対するPowerShellの応答方法を決定します。デフォルト値はContinueです。

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
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]

## OUTPUTS

### System.Object
## NOTES

ユーザーがこの機能を利用する前に、有人自動化がOrchestrator環境で適切にライセンスされ、設定されている必要があります。

このコマンドレットはユーザーの権限と機能を変更します。ユーザーが適切なライセンスを持ち、組織のセキュリティポリシーが指定されたユーザーに対して有人自動化を許可していることを確認してください。

有人自動化により、ユーザーはアクティブなセッションでロボットを実行できるようになります。これは、ロボットがユーザーと同じアクセス権限を持つことを意味します。この機能を有効にするときは、セキュリティへの影響を考慮してください。

逆の操作は、Disable-OrchUserAttendedを使用してユーザーから有人自動化機能を取り消すことができます。

特に複数のユーザーに対して有人自動化を有効にする場合は、変更を適用する前に-WhatIfを使用して変更をプレビューしてください。

## RELATED LINKS
