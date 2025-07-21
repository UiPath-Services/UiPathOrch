---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchBucketItem

## SYNOPSIS
ストレージバケット内に保存されたアイテム（ファイルとディレクトリ）を取得します。

## SYNTAX

```
Get-OrchBucketItem [[-Name] <String[]>] [[-FullPath] <String[]>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchBucketItem コマンドレットは、UiPath Orchestrator のストレージバケット内に保存されたアイテム（ファイルとディレクトリ）を取得します。ストレージバケットは自動化プロセス用の外部ストレージプロバイダーとして機能し、このコマンドレットはこれらのバケット内のファイルとディレクトリ構造の可視性を提供します。

各バケットアイテムには、FullPath（バケット内のファイル/ディレクトリパス）、ContentType（ファイルの MIME タイプ）、Size（ファイルのバイト単位のサイズ）、IsDirectory（アイテムがディレクトリかどうかを示すブール値）などのプロパティが含まれます。

バケットアイテムは特定のフォルダー内に存在するフォルダーエンティティです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定してください。

このコマンドレットを使用すると、ストレージバケット内のファイルとディレクトリの内容を参照し、自動化プロセスで利用可能なリソースを把握できます。-Recurse パラメーターを使用してネストされたディレクトリを含めることができ、-Depth でディレクトリ横断の深度を制御できます。

主要エンドポイント: GET /odata/BucketItems

OAuth 必須スコープ: OR.Administration または OR.Administration.Read

必要なアクセス許可: Buckets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\MyWorkspace> Get-OrchBucketItem
```

現在のフォルダーのすべてのストレージバケットからアイテムを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchBucketItem -Recurse
```

すべてのフォルダーから再帰的にバケットアイテムを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchBucketItem -FullPath "*documents*" -Path Orch1:\Production
```

Production フォルダーのバケットから、フルパスに "documents" を含むアイテムを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchBucketItem -Recurse | Where-Object IsDirectory -eq $false | Select-Object Path, FullPath, Size, ContentType
```

すべてのファイル（ディレクトリ以外）を再帰的に取得し、主要なプロパティを表示します。

### Example 5
```powershell
PS C:\> Get-OrchBucketItem -Path Orch1:\Production,Orch1:\Development -Recurse -Depth 2
```

Production および Development フォルダーから最大深度 2 レベルでバケットアイテムを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchBucketItem -Name "*.pdf", "*.xlsx" -Recurse
```

すべてのフォルダーから PDF と Excel ファイルを再帰的に検索します。

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

### -FullPath
バケット内のアイテムのフルパスを指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。特定のパスまたはパスパターンのアイテムを検索する場合に使用します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
取得するバケットアイテムの名前を指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。複数の名前が指定された場合、一致するすべてのアイテムが返されます。

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
バケットアイテムを検索するフォルダーパスを指定します。パターンマッチング用のワイルドカード文字（* および ?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにする場合にこのパラメーターを使用します。

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
サブフォルダーからバケットアイテムを再帰的に取得します。指定すると、コマンドレットは指定されたパスまたは現在の場所から開始してフォルダー階層全体を横断します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.BlobFile
## NOTES

このコマンドレットはフォルダーエンティティで動作します。つまり、特定のフォルダーに移動するか、-Path パラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

大きなフォルダー階層で -Recurse パラメーターを使用する場合、操作の完了にかなりの時間がかかる場合があります。必要に応じて -Depth を使用して横断範囲を制限することを検討してください。

バケットアイテムは外部ストレージ内のファイルとディレクトリを表し、自動化プロセスでアクセス可能なリソースの可視性を提供します。IsDirectory プロパティを使用してファイルとディレクトリを区別し、Size と ContentType プロパティを使用してファイルの詳細を確認できます。

主要エンドポイント: GET /odata/BucketItems
OAuth 必須スコープ: OR.Administration または OR.Administration.Read
必要なアクセス許可: Buckets.View

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)

[New-OrchBucket](New-OrchBucket.md)

[Remove-OrchBucket](Remove-OrchBucket.md)

[Copy-OrchBucket](Copy-OrchBucket.md)
