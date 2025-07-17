---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Find-OrchFolderNoUserAssigned

## SYNOPSIS
UiPath Orchestrator内でユーザーが直接割り当てられていないフォルダを検索します。

## SYNTAX

```
Find-OrchFolderNoUserAssigned [[-Path] <String>] [-IncludeInherited] [<CommonParameters>]
```

## DESCRIPTION
Find-OrchFolderNoUserAssigned コマンドレットは、UiPath Orchestrator内でユーザーが直接割り当てられていないフォルダを検索します。これは、孤立したフォルダの識別、セキュリティ監査の実施、組織全体の適切なフォルダアクセス管理の確保に役立ちます。

このコマンドレットは、フォルダ構造とユーザー割り当てを分析して、ユーザーがアクセスできないフォルダや管理者による注意が必要なフォルダを特定します。デフォルトでは、直接のユーザー割り当てのみを考慮しますが、オプションで親フォルダからの継承された権限も含めることができます。

このコマンドレットはテナントエンティティで動作し、Orchestratorドライブ内のどの場所からでも実行できます。未割り当てフォルダの性質を理解するのに役立つ、フォルダタイプ、フィードタイプ、プロビジョンタイプなどの包括的なフォルダ情報を提供します。

主要エンドポイント：GET /odata/Folders（カスタムフィルタリングロジック付き）

OAuth必須スコープ：OR.Folders または OR.Folders.Read

必須権限：Folders.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned
```

現在のテナント内で、直接のユーザー割り当てがないすべてのフォルダを検索します。

### Example 2
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned -IncludeInherited
```

親フォルダから権限を継承している可能性のあるフォルダを含めて、ユーザー割り当てがないフォルダを検索します。

### Example 3
```powershell
PS C:\> Find-OrchFolderNoUserAssigned Orch1:
```

Orch1テナント内で、パスを明示的に指定してユーザー割り当てがないフォルダを検索します。

### Example 4
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {$_.FolderType -eq "Standard"} | Select-Object Path, DisplayName, FolderType, ProvisionType
```

未割り当てフォルダを検索し、標準フォルダのみをフィルタリングして、主要なプロパティを表示します。各フォルダの場所を識別するため、Pathが最初に選択されることに注意してください。

### Example 5
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Group-Object FolderType | Select-Object Name, Count
```

未割り当てフォルダをタイプ別にグループ化し、各タイプの数を表示します。

### Example 6
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {$_.ProvisionType -eq "Manual"} | Format-Table DisplayName, FolderType, Description
```

手動でプロビジョンされたユーザー割り当てのないフォルダを検索し、フォーマットされたテーブルで表示します。

### Example 7
```powershell
PS Orch1:\> $unassignedFolders = Find-OrchFolderNoUserAssigned
PS Orch1:\> $unassignedFolders | ConvertTo-Json -Depth 3
```

すべての未割り当てフォルダを取得し、詳細な分析のために完全なオブジェクト構造を表示します。

### Example 8
```powershell
PS Orch1:\> Find-OrchFolderNoUserAssigned | Where-Object {$_.FeedType -eq "Processes"} | Select-Object Path, DisplayName, Description, Id
```

プロセスタイプのユーザー割り当てがないフォルダを検索し、主要な識別情報を表示します。

## PARAMETERS

### -Path
検索するテナントパスを指定します。指定しない場合、現在のテナントが使用されます。テナントドライブ名（例：Orch1:、Orch2:）を使用してください。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: Current tenant
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeInherited
指定すると、親フォルダからユーザー権限を継承している可能性のあるフォルダも分析に含めます。デフォルトでは、直接のユーザー割り当てのみが考慮されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.Folder
## NOTES

**重要な管理用途：**
- **セキュリティ監査**：ユーザーがアクセスできない可能性のあるフォルダを特定
- **クリーンアップ操作**：安全に削除できる孤立したフォルダを検索
- **アクセス管理**：すべてのフォルダに適切なユーザー割り当てがあることを確認
- **コンプライアンス**：組織のアクセスポリシーが適切に実装されていることを確認

**返されるフォルダ情報：**
- Path：テナント内のフォルダへのフルパス
- DisplayName：フォルダの人間が読める名前
- Id：フォルダの一意識別子
- FolderType：フォルダのタイプ（Standard、Personal、Solution など）
- FeedType：フィードタイプ（Processes、FolderHierarchy、PersonalWorkspace など）
- ProvisionType：フォルダの作成方法（Manual、Automatic）
- PermissionModel：フォルダに適用された権限モデル

**一般的なフォルダタイプ：**
- Standard：通常の組織フォルダ
- Personal：ユーザー固有の個人ワークスペースフォルダ
- Solution：UiPath Solutionに関連するフォルダ
- Department：部署フォルダ

**一般的なフィードタイプ：**
- Processes：自動化プロセスを格納するためのフォルダ
- FolderHierarchy：組織のための構造フォルダ
- PersonalWorkspace：個人ユーザーワークスペースフォルダ

**解釈ガイドライン：**
- 割り当てのない個人ワークスペースフォルダは正常な場合があります（ユーザー固有）
- 割り当てのない標準プロセスフォルダは通常、注意が必要です
- Solutionフォルダは専門的な権限モデルを持つ場合があります
- 自動的にプロビジョンされたフォルダはシステム生成の可能性があります

**ベストプラクティス：**
- アクセス管理レビューの一環として、このコマンドレットを定期的に実行
- 潜在的なセキュリティ問題について、割り当てのないStandardフォルダを調査
- 完全な権限の全体像を理解するため、-IncludeInheritedの使用を検討
- 将来の参照のために、意図的に未割り当てのフォルダを文書化

**Path選択に関する重要な注意事項：**
結果でSelect-Objectを使用する際は、各フォルダがどのテナントとどの場所に属しているかを特定するため、常にPathを最初のプロパティとして含めてください。これは、複数のテナントにわたってフォルダを管理する際に不可欠です。

詳細な権限と構成情報を含む完全なフォルダオブジェクト構造を探索するには、ConvertTo-Jsonを使用してください。

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Add-OrchFolderUser](Add-OrchFolderUser.md)

[Get-OrchFolderUsage](Get-OrchFolderUsage.md)
