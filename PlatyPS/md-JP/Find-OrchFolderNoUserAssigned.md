---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Find-OrchFolderNoUserAssigned

## SYNOPSIS
UiPath Orchestrator で直接ユーザーが割り当てられていないフォルダーを検索します。

## SYNTAX

`
Find-OrchFolderNoUserAssigned [[-Path] <String>] [-IncludeInherited] [<CommonParameters>]
`

## DESCRIPTION
Find-OrchFolderNoUserAssigned コマンドレットは、UiPath Orchestrator 内で直接ユーザーが割り当てられていないフォルダーを検索します。これは、孤立したフォルダーの特定、セキュリティ監査の実施、および組織全体での適切なフォルダーアクセス管理の確保に役立ちます。

このコマンドレットは、フォルダー構造とユーザー割り当てを分析して、ユーザーがアクセスできないフォルダーや管理者の注意が必要なフォルダーを特定します。デフォルトでは、直接ユーザー割り当てのみを考慮しますが、オプションで親フォルダーからの継承された権限を含めることができます。

このコマンドレットは、テナントエンティティ上で動作し、Orchestrator ドライブ内の任意の場所から実行できます。管理者が割り当てられていないフォルダーの性質を理解できるよう、フォルダータイプ、フィードタイプ、プロビジョンタイプなどの包括的なフォルダー情報を提供します。

主要エンドポイント: GET /odata/Folders (カスタムフィルタリングロジック付き)

OAuth 必須スコープ: OR.Folders または OR.Folders.Read

必要な権限: Folders.View

## EXAMPLES

### Example 1
`powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned
`

現在のテナントで直接ユーザーが割り当てられていないすべてのフォルダーを検索します。

### Example 2
`powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned -IncludeInherited
`

親フォルダーから権限を継承している可能性があるフォルダーを含めて、ユーザーが割り当てられていないフォルダーを検索します。

### Example 3
`powershell
PS C:\> Find-OrchFolderNoUserAssigned Orch1:
`

パスを明示的に指定して、Orch1 テナントでユーザーが割り当てられていないフォルダーを検索します。

### Example 4
`powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {.FolderType -eq "Standard"} | Select-Object Path, DisplayName, FolderType, ProvisionType
`

割り当てられていないフォルダーを検索し、Standard フォルダーのみでフィルタリングし、キープロパティを表示します。各フォルダーの場所を識別するために、Path が最初に選択されていることに注意してください。

### Example 5
`powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Group-Object FolderType | Select-Object Name, Count
`

割り当てられていないフォルダーをタイプごとにグループ化し、各タイプの数を表示します。

### Example 6
`powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {.ProvisionType -eq "Manual"} | Format-Table DisplayName, FolderType, Description
`

ユーザーが割り当てられていない手動プロビジョニングされたフォルダーを検索し、フォーマットされたテーブルで表示します。

### Example 7
`powershell
PS Orch1:\>  = Find-OrchFolderNoUserAssigned
PS Orch1:\>  | ConvertTo-Json -Depth 3
`

すべての割り当てられていないフォルダーを取得し、詳細な分析のために完全なオブジェクト構造を表示します。

### Example 8
`powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {.FeedType -eq "Processes"} | Select-Object Path, DisplayName, Description, Id
`

ユーザーが割り当てられていないプロセスタイプのフォルダーを検索し、主要な識別情報を表示します。

## PARAMETERS

### -Path
検索するテナントパスを指定します。指定しない場合は、現在のテナントが使用されます。テナントドライブ名 (例: Orch1:, Orch2:) を使用します。

`yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: Current tenant
Accept pipeline input: False
Accept wildcard characters: False
`

### -IncludeInherited
指定した場合、分析に親フォルダーからユーザー権限を継承している可能性があるフォルダーを含めます。デフォルトでは、直接ユーザー割り当てのみが考慮されます。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.Folder

## NOTES

**重要な管理用途:**
- **セキュリティ監査**: ユーザーがアクセスできない可能性があるフォルダーを特定
- **クリーンアップ操作**: 安全に削除できる孤立したフォルダーを発見
- **アクセス管理**: すべてのフォルダーに適切なユーザー割り当てがあることを確認
- **コンプライアンス**: 組織のアクセスポリシーが適切に実装されていることを確認

**返されるフォルダー情報:**
- Path: テナント内のフォルダーへのフルパス
- DisplayName: フォルダーの人間が読める名前
- Id: フォルダーの一意識別子
- FolderType: フォルダーのタイプ（Standard、Personal、Solution など）
- FeedType: フィードタイプ（Processes、FolderHierarchy、PersonalWorkspace など）
- ProvisionType: フォルダーの作成方法（Manual、Automatic）
- PermissionModel: フォルダーに適用される権限モデル

**一般的なフォルダータイプ:**
- Standard: 通常の組織フォルダー
- Personal: ユーザー固有の個人用ワークスペースフォルダー
- Solution: UiPath Solutions に関連するフォルダー
- Department: 部門フォルダー

**一般的なフィードタイプ:**
- Processes: 自動化プロセスを格納するためのフォルダー
- FolderHierarchy: 組織のための構造的フォルダー
- PersonalWorkspace: 個人ユーザーのワークスペースフォルダー

**解釈ガイドライン:**
- 割り当てのない個人用ワークスペースフォルダーは通常（ユーザー固有）
- 割り当てのない Standard プロセスフォルダーは通常注意が必要
- Solution フォルダーは特殊な権限モデルを持つ場合がある
- 自動プロビジョニングされたフォルダーはシステム生成の可能性がある

**ベストプラクティス:**
- アクセス管理レビューの一環として、このコマンドレットを定期的に実行
- 潜在的なセキュリティ問題について、割り当てのない Standard フォルダーを調査
- 完全な権限の全体像を理解するために -IncludeInherited の使用を検討
- 将来の参照のために、意図的に割り当てられていないフォルダーを文書化

**パス選択に関する重要な注意:**
結果で Select-Object を使用する場合は、各フォルダーがどのテナントと場所に属するかを識別するために、常に Path を最初のプロパティとして含めてください。これは、複数のテナントにわたってフォルダーを管理する際に不可欠です。

詳細な権限と構成情報を含む完全なフォルダーオブジェクト構造を調べるには、ConvertTo-Json を使用してください。

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Add-OrchFolderUser](Add-OrchFolderUser.md)

[Get-OrchFolderUsage](Get-OrchFolderUsage.md)
