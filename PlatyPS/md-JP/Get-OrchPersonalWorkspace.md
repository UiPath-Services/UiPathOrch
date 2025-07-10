---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPersonalWorkspace

## SYNOPSIS
UiPath Orchestrator から個人用ワークスペースを取得します。

## SYNTAX

`
Get-OrchPersonalWorkspace [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
`

## DESCRIPTION
Get-OrchPersonalWorkspace コマンドレットは、UiPath Orchestrator から個人用ワークスペース情報を取得します。個人用ワークスペースは、特定のユーザーに割り当てられる個別の自動化環境で、開発、テスト、および個人用自動化アクティビティのための隔離された空間を提供します。

個人用ワークスペースは、共有組織リソースに影響を与えることなく、ユーザーが自動化プロジェクトを独立して開発、テスト、管理できる専用環境を提供することで、個人の生産性を向上させます。各ワークスペースは特定のユーザーに関連付けられ、開発活動の分離を提供します。

このコマンドレットは、テナントエンティティ上で動作し、ワークスペースの所有権、活動状態、最後のログイン時刻、および探索ユーザーに関する情報を提供します。個人用ワークスペースは、プロセステスト、個人自動化、隔離された開発環境など、さまざまな自動化開発シナリオをサポートします。

-Name と -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /odata/PersonalWorkspaces

OAuth 必須スコープ: OR.Folders または OR.Folders.Read

必要な権限: Folders.View

## EXAMPLES

### Example 1
`powershell
PS Orch1:\> Get-OrchPersonalWorkspace
`

Orch1 テナントのすべての個人用ワークスペースを取得します。

### Example 2
`powershell
PS Orch1:\> Get-OrchPersonalWorkspace *workspace*
`

ワイルドカードパターンマッチングと位置パラメーターを使用して、名前に "workspace" を含む個人用ワークスペースを取得します。

### Example 3
`powershell
PS C:\> Get-OrchPersonalWorkspace -Path Orch1:, Orch2:
`

クロステナント分析のために、Orch1 と Orch2 の両方のテナントから個人用ワークスペースを取得します。-Path パラメーターにより、任意の場所からの実行が可能であることを示します。

### Example 4
`powershell
PS Orch1:\> Get-OrchPersonalWorkspace | ConvertTo-Json -Depth 2
`

すべての個人用ワークスペースを取得し、所有権と活動状態を含むワークスペースプロパティの詳細な分析のために、その構造を JSON 形式で表示します。

### Example 5
`powershell
PS Orch1:\> Get-OrchPersonalWorkspace | Where-Object {.IsActive -eq True}
`

パイプラインフィルタリングを使用して、アクティブな個人用ワークスペースのみを取得します。

### Example 6
`powershell
PS Orch1:\> Get-OrchPersonalWorkspace | Select-Object Path, Name, OwnerName, IsActive, LastLogin
`

マルチテナント識別のために Path を最初に表示し、選択されたプロパティを持つ個人用ワークスペースを取得します。

## PARAMETERS

### -Name
取得する個人用ワークスペースの名前を指定します。パターンマッチングのためのワイルドカード (* と ?) をサポートします。[Ctrl+Space] または [Tab] を押すことで自動補完を使用できます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All workspaces
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
個人用ワークスペースを検索するテナントパスを指定します。テナントドライブ名 (例: Orch1:, Orch2:) を使用します。指定しない場合は、現在のテナントが使用されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current tenant
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -ProgressAction
コマンドレットによって生成される進行状況の更新に対して PowerShell が応答する方法を指定します。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### None

## OUTPUTS

### UiPath.PowerShell.Entities.PersonalWorkspace

## NOTES

このコマンドレットは、テナントエンティティ上で動作し、Orchestrator 環境内の任意の場所から実行できます。

**個人用ワークスペース情報:**
- Id: ワークスペースの一意識別子
- Key: API 操作用のワークスペースキー
- Name: 個人用ワークスペースの表示名
- IsActive: ワークスペースが現在アクティブかどうか
- OwnerId: ワークスペース所有者の一意識別子
- OwnerName: ワークスペース所有者の表示名
- LastLogin: ワークスペースの最後のアクセス時刻のタイムスタンプ
- ExploringUserIds: 探索アクセス権を持つユーザーIDの配列

**ワークスペースの状態:**
- Active: ワークスペースが現在利用可能
- Inactive: ワークスペースが無効または停止中
- Exploring: ワークスペースに探索アクセス権を持つ追加ユーザーが存在

**一般的な使用例:**
- 組織全体の個人用ワークスペースの使用状況を監視
- クリーンアップまたは再アクティブ化のために非アクティブなワークスペースを特定
- ワークスペースの所有権とアクセスパターンの追跡
- 個人用ワークスペースの割り当てと使用状況の監査
- ワークスペースの活動と関与に関するレポートの生成

**管理シナリオ:**
- 個人用ワークスペースリソースの容量計画
- ユーザーのオンボーディングとワークスペースプロビジョニングの追跡
- アーカイブまたは削除対象のワークスペースの特定
- 協調的なワークスペース使用の監視（探索ユーザー）

**パス選択に関する重要な注意:**
結果で Select-Object を使用する場合は、各ワークスペースがどのテナントに属するかを識別するために、常に Path を最初のプロパティとして含めてください。これは、複数のテナントにわたってワークスペースを管理する際に不可欠です。

詳細な所有権およびアクセス情報を含む完全なワークスペースオブジェクト構造を調べるには、ConvertTo-Json を使用してください。

主要エンドポイント: GET /odata/PersonalWorkspaces
OAuth 必須スコープ: OR.Folders または OR.Folders.Read
必要な権限: Units.View

## RELATED LINKS

[Remove-OrchPersonalWorkspace](Remove-OrchPersonalWorkspace.md)

[Enable-OrchPersonalWorkspace](Enable-OrchPersonalWorkspace.md)

[Disable-OrchPersonalWorkspace](Disable-OrchPersonalWorkspace.md)
