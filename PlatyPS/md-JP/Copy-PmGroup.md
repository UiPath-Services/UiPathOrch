---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-PmGroup

## SYNOPSIS
組織間でグループをコピーします。

## SYNTAX

```
Copy-PmGroup [[-GroupName] <String[]>] [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-PmGroup コマンドレットは、UiPath Platform Management を使用してソース組織から宛先組織にPlatform Managementグループをコピーします。このコマンドレットは、メンバー関連付けと権限を含むグループ構成のコピーを作成し、複数のUiPath組織間でのグループ管理を可能にします。

このコマンドレットは、複数の宛先組織への同時グループコピーをサポートします。グループは GroupName パラメーターで識別でき、複数のグループを効率的にコピーするためのワイルドカードパターンをサポートします。

このコマンドレットは、ローカルユーザー、ロボットアカウント、外部アプリケーション、ディレクトリユーザー、ディレクトリグループなどの同名のエンティティを、コピーしたグループに自動的に追加します。ただし、同名のエンティティが宛先組織に存在しない場合、自動的に作成されることはありません。一致する名前の既存エンティティのみがコピーされたグループに追加されます。

-GroupName パラメーターを使用してコピーするグループを指定し、-Destination パラメーターを使用してターゲット組織を指定します。-Path パラメーターを使用して、特定の組織コンテキスト内から操作していない場合に複数のソース組織を操作できます。

これはテナントエンティティコマンドレットです。-Path パラメーターはソースドライブ名（例：Orch1:, Orch2:）を指定し、-Destination はグループをコピーするターゲット組織ドライブを指定します。

主要エンドポイント: POST /api/Group

OAuth 必要なスコープ: PM.Group

必要な権限: 

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-PmGroup AdminGroup Orch2:
```

現在の組織（Orch1）からOrch2組織にAdminGroupをコピーします。

### Example 2
```powershell
PS C:\> Copy-PmGroup -Path Orch1: AnalystTeam Orch2:, Orch3:
```

Orch1からOrch2とOrch3の両方の組織にAnalystTeamグループをコピーします。

### Example 3
```powershell
PS Orch1:\> Copy-PmGroup DeveloperGroup, ViewerGroup Orch2: -WhatIf
```

現在の組織からOrch2にDeveloperGroupとViewerGroupをコピーする場合に何が起こるかを示します。

### Example 4
```powershell
PS C:\> Copy-PmGroup -Path Orch1: *Admin* Orch2:
```

ワイルドカードパターンを使用して、Orch1からOrch2にAdminが含まれる名前のすべてのグループをコピーします。

### Example 5
```powershell
PS Orch1:\> Get-PmGroup *Manager* | Copy-PmGroup -Destination Orch2:, Orch3:
```

Managerが含まれる名前のすべてのグループを取得し、パイプライン入力を使用してOrch2とOrch3の両方の組織にコピーします。

### Example 6
```powershell
PS C:\> Copy-PmGroup -Path Orch1: Orch2: -Confirm
```

確認プロンプトでOrch1からOrch2にすべてのグループをコピーします（-GroupNameが指定されない場合、すべてのグループがコピーされます）。

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

### -Destination
グループをコピーする宛先組織ドライブを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -GroupName
コピーするグループのグループ名を指定します。指定しない場合、すべてのグループがコピーされます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ソース組織ドライブを指定します。指定しない場合、現在の組織がソースとして使用されます。

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

### -WhatIf
コマンドレットを実行した場合の動作を示します。
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
### System.String
## OUTPUTS

### System.Object
## NOTES
これはテナントエンティティコマンドレットです。-Path パラメーターは、ソースと宛先の組織のドライブ名（例：Orch1:, Orch2:）を指定します。

グループには、メンバー関連付けと権限が含まれています。グループをコピーする際、同名のエンティティ（ローカルユーザー、ロボットアカウント、外部アプリケーション、ディレクトリユーザー、ディレクトリグループ）が宛先組織に存在する場合、自動的にコピーされたグループに追加されます。宛先に存在しないエンティティは自動的に作成されません。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Get-PmGroup](Get-PmGroup.md)

[New-PmGroup](New-PmGroup.md)

[Remove-PmGroup](Remove-PmGroup.md)

[Set-PmGroup](Set-PmGroup.md)
