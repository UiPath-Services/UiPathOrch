---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchBucket

## SYNOPSIS
UiPath Orchestrator フォルダーで構成されたストレージバケットを取得します。

## SYNTAX

```
Get-OrchBucket [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchBucket コマンドレットは、UiPath Orchestrator フォルダー内で構成されたストレージバケットを取得します。ストレージバケットは自動化プロセス用の外部ストレージ機能を提供し、自動化ワークフローで使用されるファイル、ドキュメント、その他のアーティファクトの安全な保存と取得を可能にします。

ストレージバケットは Orchestrator と統合された外部ストレージプロバイダーとして機能し、さまざまなクラウドストレージサービスとファイルシステムをサポートします。各バケットには、Name、Description、Identifier（GUID）、StorageProvider、StorageContainer、Options、FoldersCount などのプロパティが含まれます。

このコマンドレットはフォルダーエンティティ操作として動作し、適切なフォルダーコンテキストへの移動または -Path パラメーターを使用したターゲットフォルダーの指定が必要です。サブフォルダー内のバケットを含めるには -Recurse パラメーターを使用し、再帰レベルを制御するには -Depth を使用します。

プライマリエンドポイント: GET /odata/Buckets

OAuth 必須スコープ: OR.Administration または OR.Administration.Read

必要なアクセス許可: Buckets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\MyWorkspace> Get-OrchBucket
```

現在のフォルダーからすべてのストレージバケットを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchBucket -Recurse
```

すべてのフォルダーから再帰的にストレージバケットを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchBucket -Path Orch1:\Production DataStorage*
```

Production フォルダーから名前が "DataStorage" で始まるバケットを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchBucket -Recurse | Select-Object Path, Name, StorageProvider, FoldersCount
```

すべてのバケットを再帰的に取得し、Path を最初に表示して主要なプロパティを表示します。

### Example 5
```powershell
PS C:\> Get-OrchBucket -Path Orch1:\Production,Orch1:\Development -Recurse -Depth 2
```

Production および Development フォルダーから最大深度 2 レベルでバケットを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchBucket -ExportCsv "buckets-report.csv" -CsvEncoding UTF8
```

すべてのバケット情報を UTF8 エンコーディングで CSV ファイルにエクスポートします。

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
取得するストレージバケットの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのバケットが返されます。

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
ストレージバケットを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにする場合にこのパラメーターを使用します。

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

### -Recurse
サブフォルダーからストレージバケットを再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から開始してフォルダー階層全体を横断します。

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

### -CsvEncoding
CSV エクスポート時の文字エンコーディングを指定します。-ExportCsv パラメーターと組み合わせて使用します。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
バケット情報をエクスポートする CSV ファイルのパスを指定します。このパラメーターが指定された場合、通常のオブジェクト出力の代わりに CSV ファイルが作成されます。

```yaml
Type: String
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

### UiPath.PowerShell.Entities.Bucket
## NOTES

このコマンドレットはフォルダーエンティティで動作します。つまり、特定のフォルダーに移動するか、-Path パラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

大きなフォルダー階層で -Recurse パラメーターを使用する場合、操作の完了にかなりの時間がかかる場合があります。必要に応じて -Depth を使用して横断範囲を制限することを検討してください。

ストレージバケットは外部ストレージ統合の重要な構成要素であり、自動化プロセスでのファイル管理とデータ交換を可能にします。各バケットは特定のストレージプロバイダー（Azure Blob Storage、Amazon S3、ファイルシステムなど）に関連付けられています。

プライマリエンドポイント: GET /odata/Buckets
OAuth 必須スコープ: OR.Administration または OR.Administration.Read
必要なアクセス許可: Buckets.View

## RELATED LINKS

[New-OrchBucket](New-OrchBucket.md)

[Remove-OrchBucket](Remove-OrchBucket.md)

[Copy-OrchBucket](Copy-OrchBucket.md)

[Get-OrchBucketItem](Get-OrchBucketItem.md)
