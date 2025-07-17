---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchActionCatalog

## SYNOPSIS
Orchestrator からアクションカタログを取得します。

## SYNTAX

```
Get-OrchActionCatalog [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchActionCatalog コマンドレットは、Orchestrator 環境からアクションカタログを取得します。アクションカタログは、プロセスマイニングとタスクマイニングのシナリオ用の自動化アクション、ワークフロー、タスク定義を格納および整理するリポジトリです。

アクションカタログは、再利用可能な自動化コンポーネントの構造化されたストレージを提供し、アクションライブラリの一元管理を可能にします。暗号化、保持ポリシー、フォルダーによる階層組織などの機能をサポートします。

このコマンドレットはフォルダーエンティティで動作し、再帰的な横断をサポートして、異なるフォルダー階層全体でアクションカタログを検索します。各アクションカタログには、保持ポリシー、暗号化ステータス、フォルダー組織などのメタデータが含まれます。

主要エンドポイント: GET /odata/TaskCatalogs

OAuth 必須スコープ: OR.Tasks または OR.Tasks.Read

必要なアクセス許可: [PLACEHOLDER - 必要なアクセス許可を文書化予定]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\MyWorkspace> Get-OrchActionCatalog
```

現在のフォルダーからすべてのアクションカタログを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchActionCatalog -Recurse
```

すべてのフォルダーから再帰的にすべてのアクションカタログを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchActionCatalog -Path Orch1:\Production Process*
```

Production フォルダーから名前が "Process" で始まるアクションカタログを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchActionCatalog -Recurse | Select-Object Path, Name, FoldersCount, RetentionAction
```

すべてのアクションカタログを再帰的に取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS C:\> Get-OrchActionCatalog -Path Orch1:\Production,Orch1:\Development -Recurse -Depth 2
```

Production および Development フォルダーから最大深度 2 レベルでアクションカタログを取得します。

## PARAMETERS

### -Depth
再帰操作の最大深度レベルを指定します。-Recurse が指定されている場合に、再帰検索の深度を制限するためにこのパラメーターを使用します。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
取得するアクションカタログの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのアクションカタログが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
アクションカタログを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにする場合にこのパラメーターを使用します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
サブフォルダーからアクションカタログを再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から開始してフォルダー階層全体を横断します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.Bucket
## NOTES

このコマンドレットはフォルダーエンティティで動作します。つまり、特定のフォルダーに移動するか、-Path パラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

大きなフォルダー階層で -Recurse パラメーターを使用する場合、操作の完了にかなりの時間がかかる場合があります。必要に応じて -Depth を使用して横断範囲を制限することを検討してください。

アクションカタログは以下の重要な機能をサポートします:
- 機密性の高い自動化コンポーネントの暗号化
- 設定可能なアクション（削除、アーカイブ）による保持ポリシー
- フォルダー構造による階層組織
- 再利用可能な自動化アクションの一元管理

フォルダーエンティティコマンドレットでは、パラメーターの配置が重要です。適切な自動補完機能のために、コマンドレット名の直後に -Path、-Recurse、-Depth パラメーターを配置してください。

アクションカタログオブジェクトには保持設定や組織情報などの詳細なメタデータが含まれているため、ConvertTo-Json を使用してアクションカタログオブジェクトの完全な構造を調べてください。

主要エンドポイント: GET /odata/TaskCatalogs
OAuth 必須スコープ: OR.Tasks または OR.Tasks.Read
必要なアクセス許可: TaskCatalogs.View

## RELATED LINKS

[Remove-OrchActionCatalog](Remove-OrchActionCatalog.md)
[Copy-OrchActionCatalog](Copy-OrchActionCatalog.md)
[Get-OrchFolder](Get-OrchFolder.md)
