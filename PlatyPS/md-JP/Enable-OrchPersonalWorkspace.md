---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchPersonalWorkspace

## SYNOPSIS
指定したユーザーのパーソナルワークスペースとロボットセッションアクセスを有効にします。

## SYNTAX

```
Enable-OrchPersonalWorkspace [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchPersonalWorkspace コマンドレットは、UiPath Orchestrator内で指定したユーザーのパーソナルワークスペース機能とロボットセッションアクセスの両方を有効にします。この操作により、ユーザーは個別の環境で包括的な開発・実行機能を利用できるようになります。

パーソナルワークスペースは、共有の組織フォルダーとは独立して、自動化プロジェクトを開発、テスト、管理するための専用環境をユーザーに提供します。このコマンドレットはロボットセッションアクセスも有効にし、ユーザーがワークスペース環境内で有人自動化プロセスを実行できるようにします。

内部的に、このコマンドレットは -MayHavePersonalWorkspace True と -MayHaveRobotSession True の両方のパラメーターを使用して Update-OrchUser を呼び出します。この二重構成により、ユーザーはワークスペース開発機能と実行権限の両方を受け取り、完全な自動化開発ライフサイクルをサポートします。

このコマンドレットはテナントレベルで動作し、組織全体のユーザーワークスペースアクセスを管理します。パーソナルワークスペースが有効化されたユーザーは、専用のワークスペースと共有の組織フォルダーの両方にアクセスできます。操作は自動的にOrchestratorキャッシュをクリアして、新しい機能への即座のアクセスを保証します。

主要エンドポイント: PATCH /odata/Users

必要なOAuthスコープ: OR.Users または OR.Users.Write

必要な権限: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Enable-OrchPersonalWorkspace john.doe
```

現在のテナントでユーザー john.doe のパーソナルワークスペースとロボットセッションアクセスを有効にします。

### Example 2
```powershell
PS C:\> Enable-OrchPersonalWorkspace -Path Orch1: jane.smith -WhatIf
```

Orch1テナントでjane.smithのパーソナルワークスペースを有効にしたときに何が起こるかを表示します。

### Example 3
```powershell
PS Orch1:\> Enable-OrchPersonalWorkspace developer1, developer2, developer3
```

現在のテナントで複数の開発者のパーソナルワークスペースを有効にします。

### Example 4
```powershell
PS C:\> Enable-OrchPersonalWorkspace -Path Orch1: *citizen* -Confirm
```

"citizen"を含むユーザー名のすべてのユーザーのパーソナルワークスペースを確認プロンプトとともに有効にします。

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Type Internal | Where-Object {$_.Department -eq "RPA"} | Enable-OrchPersonalWorkspace
```

RPA部門のすべての内部ユーザーを取得し、パイプライン入力を使用してパーソナルワークスペースを有効にします。

### Example 6
```powershell
PS C:\> Enable-OrchPersonalWorkspace -Path Orch1:, Orch2: newuser1, newuser2
```

複数のテナントにわたってnewuser1とnewuser2のパーソナルワークスペースを有効にします。

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
対象のテナントドライブを指定します。指定しない場合、現在のテナントが対象になります。複数のテナントを対象とするテナントレベル操作に使用します。

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
パーソナルワークスペースを有効にするユーザーのユーザー名を指定します。バルク操作用のワイルドカードパターンをサポートします。

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
このコマンドレットは共通パラメーターをサポートしています: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, -WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]

## OUTPUTS

### System.Object
## NOTES
このコマンドレットは、指定されたユーザーのMayHavePersonalWorkspaceとMayHaveRobotSessionの両方のプロパティをTrueに設定するテナントレベルエンティティ操作です。パーソナルワークスペースは、共有の組織フォルダーとは分離された専用の開発環境を提供します。ロボットセッション機能により、ユーザーはワークスペース環境内で有人自動化プロセスを実行できます。操作は自動的にOrchestratorキャッシュをクリアして、新しい権限の即座の利用可能性を保証します。

## RELATED LINKS

[Disable-OrchPersonalWorkspace](Disable-OrchPersonalWorkspace.md)

[Get-OrchPersonalWorkspace](Get-OrchPersonalWorkspace.md)

[Get-OrchUser](Get-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)
