---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchPersonalWorkspace

## SYNOPSIS
指定されたユーザーのパーソナルワークスペースアクセスを無効にします。

## SYNTAX

```
Disable-OrchPersonalWorkspace [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchPersonalWorkspaceコマンドレットは、UiPath Orchestrator内で指定されたユーザーのパーソナルワークスペース機能を無効にします。パーソナルワークスペースは、ユーザーが共有の組織フォルダとは独立して、自動化プロジェクトの開発、テスト、管理を行うための専用環境を提供します。

パーソナルワークスペースを無効にすると、既存のワークスペースデータを保持しながら、ユーザーが個人のワークスペース環境にアクセスできなくなります。これは、組織ポリシーの実施、リソース配分の管理、メンテナンス中の一時的なユーザーアクセス制限、または新しいセキュリティポリシーの実装に役立ちます。

内部的には、このコマンドレットは-MayHavePersonalWorkspace Falseパラメータを使用してUpdate-OrchUserを呼び出します。Enableに対応するコマンドレットとは異なり、このコマンドレットはパーソナルワークスペースの権限のみに影響し、ロボットセッションアクセスは変更しません。この対象を絞ったアプローチにより、ユーザー機能の細かい制御が可能になります。

このコマンドレットはテナントレベルで動作し、組織全体でユーザーのワークスペースアクセスを管理します。パーソナルワークスペースが無効にされたユーザーは、自動化活動のために共有の組織フォルダに制限されます。操作は、ワークスペースアクセスの即座の制限を確実にするために、Orchestratorキャッシュを自動的にクリアします。

主要エンドポイント: PATCH /odata/Users

OAuth必要スコープ: OR.Users または OR.Users.Write

必要な権限: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Disable-OrchPersonalWorkspace john.doe
```

現在のテナントのユーザーjohn.doeのパーソナルワークスペースアクセスを無効にします。

### Example 2
```powershell
PS C:\> Disable-OrchPersonalWorkspace -Path Orch1: jane.smith -WhatIf
```

Orch1テナントのjane.smithのパーソナルワークスペースを無効にする際の動作を表示します。

### Example 3
```powershell
PS Orch1:\> Disable-OrchPersonalWorkspace contractor1, contractor2, contractor3
```

現在のテナントの複数の契約者ユーザーのパーソナルワークスペースを無効にします。

### Example 4
```powershell
PS C:\> Disable-OrchPersonalWorkspace -Path Orch1: *temp* -Confirm
```

ユーザー名に"temp"が含まれるすべてのユーザーのパーソナルワークスペースを確認プロンプト付きで無効にします。

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Type External | Disable-OrchPersonalWorkspace -WhatIf
```

すべての外部ユーザーを取得し、パイプライン入力を使用してパーソナルワークスペースを無効にする際の動作を表示します。

### Example 6
```powershell
PS C:\> Disable-OrchPersonalWorkspace -Path Orch1:, Orch2: inactive.user1, inactive.user2
```

複数のテナントにわたって非アクティブなユーザーのパーソナルワークスペースを無効にします。

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
対象テナントドライブを指定します。指定されない場合、現在のテナントが対象になります。複数のテナントを対象とするテナントレベル操作用です。

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
パーソナルワークスペースを無効にするユーザーのユーザー名を指定します。一括操作のためのワイルドカードパターンをサポートします。

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
このコマンドレットは、指定されたユーザーのMayHavePersonalWorkspaceプロパティをFalseに設定するテナントレベルエンティティ操作です。Enable-OrchPersonalWorkspaceとは異なり、このコマンドレットはワークスペースアクセスのみに影響し、ロボットセッション権限は変更しません。パーソナルワークスペースは、共有の組織フォルダとは別の専用開発環境を提供します。ワークスペースアクセスを無効にすると、既存のワークスペースデータを保持しながら、ユーザーが共有の組織フォルダに制限されます。操作は、ワークスペースアクセスの即座の制限を確実にするために、Orchestratorキャッシュを自動的にクリアします。

## RELATED LINKS

[Enable-OrchPersonalWorkspace](Enable-OrchPersonalWorkspace.md)

[Get-OrchPersonalWorkspace](Get-OrchPersonalWorkspace.md)

[Get-OrchUser](Get-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)
