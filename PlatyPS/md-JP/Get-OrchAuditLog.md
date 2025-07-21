---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAuditLog

## SYNOPSIS
監視とコンプライアンス目的で UiPath Orchestrator から監査ログを取得します。

## SYNTAX

### Filter (Default)
```
Get-OrchAuditLog [[-Last] <String>] [[-Component] <String[]>] [[-UserName] <String[]>] [[-Action] <String[]>]
 [-ExecutionTimeAfter <DateTime>] [-ExecutionTimeBefore <DateTime>] [-ExpandEntity] [-ExpandDetails]
 [-Skip <UInt64>] [-First <UInt64>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

### Id
```
Get-OrchAuditLog [-Id <String[]>] [-ExpandEntity] [-ExpandDetails] [-Skip <UInt64>] [-First <UInt64>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchAuditLog コマンドレットは UiPath Orchestrator から監査ログを取得し、ユーザーアクティビティ、システム変更、管理操作に関する詳細情報を提供します。監査ログは、セキュリティ監視、コンプライアンスレポート、システムアクティビティのトラブルシューティングに不可欠です。

監査ログは、ユーザーアクション、コンポーネント変更、タイムスタンプ、および Orchestrator 内で実行された操作のコンテキスト詳細を含む包括的な情報をキャプチャします。このコマンドレットは、ユーザー名、コンポーネント、アクション、時間範囲などのさまざまな条件でフィルタリングして、特定の監査イベントに焦点を当てることができます。

監査ログは、テナント全体のスコープで動作するテナントエンティティです。ドライブ名（例：Orch1:、Orch2:）でターゲットテナントを指定するには、-Path パラメーターを使用します。

このコマンドレットはテナントレベルで動作し、組織全体の監査イベントへのアクセスを提供します。フィルタリングパラメーターを使用して結果を絞り込み、クエリのパフォーマンスを向上させます。-ExpandEntity および -ExpandDetails パラメーターは、ログされたイベントに関する追加のコンテキスト情報を提供します。

このコマンドレットは、広範囲な検索のためのフィルターベースのクエリと、特定の監査ログエントリを取得するための ID ベースのクエリの両方をサポートします。大きな結果セットを効率的に管理するには、ページネーションパラメーター（-Skip、-First）を使用します。

主要エンドポイント: GET /odata/AuditLogs

OAuth 必須スコープ: OR.Audit または OR.Audit.Read

必要なアクセス許可: Audit.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchAuditLog -Last Day
```

現在のテナントで過去 24 時間のすべての監査ログを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchAuditLog -Component Queues,Jobs -Action Create,Delete
```

Queues および Jobs コンポーネントでの作成および削除アクションの監査ログを取得します。

### Example 3
```powershell
PS C:\> Get-OrchAuditLog -Path Orch1:, Orch2: -UserName admin*
```

複数のテナントから "admin" で始まる名前のユーザーの監査ログを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchAuditLog -Last Day | Select-Object -First 1 | ConvertTo-Json -Depth 3
```

最近の監査ログを取得し、ネストされた Entities および CustomDataExpanded を含む完全な構造を表示します。

## PARAMETERS

### -Action
監査ログをフィルタリングするアクションタイプを指定します。一般的なアクションには、Create、Update、Delete、Login、Logout、Start、Stop などがあります。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Component
監査ログをフィルタリングするコンポーネントタイプを指定します。例には、Users、Roles、Jobs、Processes、Assets、Machines などがあります。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExecutionTimeAfter
監査ログをフィルタリングするための最早実行時刻を指定します。このタイムスタンプ以降のログのみが返されます。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExecutionTimeBefore
監査ログをフィルタリングするための最遅実行時刻を指定します。このタイムスタンプ以前のログのみが返されます。

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExpandEntity
監査ログエントリのエンティティ情報を展開し、影響を受けたオブジェクトに関する追加の詳細を提供します。

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

### -Id
取得する特定の監査ログエントリの ID を指定します。既知のログエントリのターゲット検索に使用します。

```yaml
Type: String[]
Parameter Sets: Id
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Last
最近のログの期間を指定します。有効な値: Hour、Day、Week、Month、3Months、6Months、Year、3Years。

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットテナントドライブを指定します。指定されていない場合、現在のテナントがターゲットになります。テナントレベルの監査ログアクセス用。

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

### -ProgressAction
{{ ProgressAction の説明を入力 }}

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

### -UserName
監査ログをフィルタリングするユーザー名を指定します。柔軟なユーザーフィルタリング用のワイルドカードパターンをサポートします。

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
結果セットの先頭からスキップする監査ログエントリの数を指定します。ページネーションに役立ちます。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
結果セットの先頭から返す監査ログエントリの最大数を指定します。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExpandDetails
監査ログエントリの詳細情報を展開し、ログされたアクションに関する包括的なコンテキストを提供します。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.AuditLog
## NOTES
このコマンドレットは、組織全体の監査ログにアクセスするためのテナントレベルエンティティ操作です。監査ログは、セキュリティ監視、コンプライアンスレポート、トラブルシューティングに不可欠です。フィルタリングパラメーターを使用してクエリのパフォーマンスを向上させ、関連するイベントに焦点を当てます。-ExpandEntity および -ExpandDetails パラメーターは追加のコンテキストを提供しますが、パフォーマンスに影響を与える可能性があります。メモリ使用量と応答時間を管理するために、大きな結果セットにはページネーションの使用を検討してください。

主要エンドポイント: GET /odata/AuditLogs
OAuth 必須スコープ: OR.Audit または OR.Audit.Read
必要なアクセス許可: Audit.View

## RELATED LINKS

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)

[Get-OrchUser](Get-OrchUser.md)

[Get-OrchRole](Get-OrchRole.md)

[Get-OrchJob](Get-OrchJob.md)
