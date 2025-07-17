---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPersonalWorkspace

## SYNOPSIS
UiPath Orchestratorから個人ワークスペースを取得します。

## SYNTAX

```
Get-OrchPersonalWorkspace [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-OrchPersonalWorkspace コマンドレットは、UiPath Orchestratorから個人ワークスペース情報を取得します。個人ワークスペースは、特定のユーザーに割り当てられた個別の自動化環境であり、開発、テスト、個人的な自動化活動のための分離されたスペースを提供します。

個人ワークスペースは、ユーザーに専用の環境を提供することで個人の生産性を向上させ、共有の組織リソースに影響を与えることなく、自動化プロジェクトを独立して開発、テスト、管理できます。各ワークスペースは特定のユーザーに関連付けられ、開発活動の分離を提供します。

このコマンドレットは、テナントエンティティで動作し、ワークスペースの所有権、アクティビティステータス、最後のログイン時刻、探索ユーザーに関する情報を提供します。個人ワークスペースは、プロセステスト、個人自動化、分離された開発環境など、さまざまな自動化開発シナリオをサポートします。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /odata/PersonalWorkspaces

OAuth必須スコープ：OR.Folders または OR.Folders.Read

必須権限：Folders.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace
```

Orch1テナントのすべての個人ワークスペースを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace *workspace*
```

ワイルドカードパターンマッチングと位置パラメーターを使用して、名前に"workspace"を含む個人ワークスペースを取得します。

### Example 3
```powershell
PS C:\> Get-OrchPersonalWorkspace -Path Orch1:, Orch2:
```

クロステナント分析のために、Orch1とOrch2の両方のテナントから個人ワークスペースを取得します。-Pathパラメーターが任意の場所からの実行を可能にすることを示しています。

### Example 4
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace | ConvertTo-Json -Depth 2
```

すべての個人ワークスペースを取得し、所有権とアクティビティステータスを含むワークスペースプロパティの詳細分析のために、構造をJSON形式で表示します。

### Example 5
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace | Where-Object {$_.IsActive -eq $true}
```

パイプラインフィルタリングを使用して、アクティブな個人ワークスペースのみを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace | Select-Object Path, Name, OwnerName, IsActive, LastLogin
```

マルチテナント識別のためにPathを最初に表示し、選択されたプロパティで個人ワークスペースを取得します。

## PARAMETERS

### -Name
取得する個人ワークスペースの名前を指定します。パターンマッチングのためのワイルドカード（*と?）をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All workspaces
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
個人ワークスペースを検索するテナントパスを指定します。テナントドライブ名（例：Orch1:、Orch2:）を使用してください。指定しない場合、現在のテナントが使用されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current tenant
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
コマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.PersonalWorkspace
## NOTES

このコマンドレットは、テナントエンティティで動作し、Orchestrator環境内の任意の場所から実行できます。

**個人ワークスペース情報：**
- Id：ワークスペースの一意識別子
- Key：API操作のためのワークスペースキー
- Name：個人ワークスペースの表示名
- IsActive：ワークスペースが現在アクティブかどうか
- OwnerId：ワークスペース所有者の一意識別子
- OwnerName：ワークスペース所有者の表示名
- LastLogin：最後のワークスペースアクセスのタイムスタンプ
- ExploringUserIds：探索アクセスを持つユーザーIDの配列

**ワークスペースの状態：**
- Active：ワークスペースが現在使用可能
- Inactive：ワークスペースが無効または一時停止
- Exploring：ワークスペースに探索アクセスを持つ追加ユーザーがいる

**一般的な使用例：**
- 組織全体での個人ワークスペース使用状況の監視
- クリーンアップまたは再アクティブ化のための非アクティブワークスペースの識別
- ワークスペースの所有権とアクセスパターンの追跡
- 個人ワークスペースの割り当てと利用の監査
- ワークスペースの活動と関与に関するレポートの生成

**管理シナリオ：**
- 個人ワークスペースリソースの容量計画
- ユーザーオンボーディングとワークスペースプロビジョニングの追跡
- アーカイブまたは削除のためのワークスペースの特定
- 協調ワークスペース使用（探索ユーザー）の監視

**Path選択に関する重要な注意事項：**
結果でSelect-Objectを使用する場合、各ワークスペースがどのテナントに属するかを特定するため、常にPathを最初のプロパティとして含めてください。これは、複数のテナントにわたってワークスペースを管理する際に不可欠です。

詳細な所有権とアクセス情報を含む完全なワークスペースオブジェクト構造を探索するには、ConvertTo-Jsonを使用してください。

主要エンドポイント：GET /odata/PersonalWorkspaces
OAuth必須スコープ：OR.Folders または OR.Folders.Read
必須権限：Units.View

## RELATED LINKS

[Remove-OrchPersonalWorkspace](Remove-OrchPersonalWorkspace.md)

[Enable-OrchPersonalWorkspace](Enable-OrchPersonalWorkspace.md)

[Disable-OrchPersonalWorkspace](Disable-OrchPersonalWorkspace.md)
