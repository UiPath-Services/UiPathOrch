---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchBucketItem

## SYNOPSIS
UiPath Orchestratorストレージバケット内のファイルやアイテムを削除します。

## SYNTAX

```
Remove-OrchBucketItem [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [[-Name] <String[]>] [[-FullPath] <String[]>] [-Confirm] [-WhatIf] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

Remove-OrchBucketItemコマンドレットは、UiPath Orchestratorストレージバケット内に保存されたファイルやアイテムを削除します。このコマンドレットは、単一ファイルの削除、複数ファイルの削除、ワイルドカードパターン、および再帰的な削除操作をサポートしています。

バケット内のアイテムを削除する前に、Set-Location (cd) を使用してターゲットフォルダーに移動するか、-Pathパラメータを使用してターゲットフォルダーを指定する必要があります。フォルダーコンテキストなしでこのコマンドレットを実行しようとすると、エラーが発生します。削除操作は元に戻すことができないため、実行前に-WhatIfパラメータを使用して操作をプレビューすることを強く推奨します。

ストレージバケットは、自動化プロセスがファイルの保存、取得、管理を実行時に行うためのファイルストレージ機能を提供します。このコマンドレットは、クリーンなストレージ環境の維持と自動化ワークフローにおけるファイルライフサイクルの管理に不可欠です。

プライマリエンドポイント: DELETE /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.DeleteFile

必要なOAuthスコープ: OR.Administration

必要な権限: Buckets.View および BlobFiles.Delete

## EXAMPLES

### 例 1: ワイルドカードパターンによる基本的なファイル削除とプレビュー
```powershell
PS Orch1:\Testing> Remove-OrchBucketItem TestBucket *.log,*.txt -WhatIf
```

複数のファイルタイプを対象とするワイルドカードパターンを使用して、実行前に削除操作をプレビューする方法を示しています。

### 例 2: -Path パラメータによるクロスフォルダー削除
```powershell
PS C:\> Remove-OrchBucketItem -Path Orch1:\Development,Orch1:\Shared *TempBucket *.txt
```

-Pathパラメータを使用して複数のフォルダー内のバケットを同時に対象とするクロスフォルダー操作を実演しています。

### 例 3: 確認付きの再帰削除
```powershell
PS Orch1:\Projects> Remove-OrchBucketItem -Recurse -Name ProjectBucket -Confirm
```

現在のフォルダー内で "ProjectBucket" という名前のストレージバケットから、確認プロンプト付きですべてのアイテムを再帰的に削除します。

## PARAMETERS

### -Path <String[]>
アイテムを削除するバケットが含まれるターゲットフォルダーのパスを指定します。現在の場所を変更せずに特定のフォルダー内のバケットアイテムを削除する場合に使用します。

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

### -Recurse [<SwitchParameter>]
サブディレクトリ内のアイテムを再帰的に削除します。深い階層構造を持つバケットコンテンツをクリーンアップする必要がある場合に使用します。

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

### -Depth <UInt32>
再帰削除操作の最大深度を指定します。-Recurseが使用されている場合にのみ有効です。このパラメータは、深くネストされたアイテムの意図しない削除を防ぐのに役立ちます。

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

### -Name <String[]>
アイテムを削除するストレージバケットの名前を指定します。ワイルドカードパターンをサポートし、複数のバケットを同時に対象とできます。

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

### -FullPath <String[]>
削除するバケット内の特定ファイルのフルパスを指定します。特定の条件に一致するファイルの一括削除のためのワイルドカードパターンをサポートします。

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

### -Confirm [<SwitchParameter>]
コマンドレット実行前に確認を求めます。削除操作は元に戻すことができないため、重要なデータを扱う場合はこのパラメータの使用を推奨します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf [<SwitchParameter>]
実際の削除を実行せずに、コマンドレットが実行された場合の動作を表示します。削除操作の影響を事前に確認する際に使用します。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction <ActionPreference>
コマンド実行中の進捗情報の表示方法を制御します。大量のアイテムを削除する場合は、'SilentlyContinue'で進捗表示を抑制できます。

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

## INPUTS

### System.String[]
バケット名やファイルパスをこのコマンドレットにパイプできます。

### UiPath.PowerShell.Entities.BucketItem
Get-OrchBucketItemからのバケットアイテムオブジェクトを直接このコマンドレットにパイプできます。

## OUTPUTS

### None
このコマンドレットは出力を生成しません。削除結果を確認するにはGet-OrchBucketItemを使用してください。

## RELATED LINKS

[Get-OrchBucketItem](Get-OrchBucketItem.md)
[Import-OrchBucketItem](Import-OrchBucketItem.md)
[Export-OrchBucketItem](Export-OrchBucketItem.md)
[New-OrchBucket](New-OrchBucket.md)
[Remove-OrchBucket](Remove-OrchBucket.md)
[UiPath Orchestrator ドキュメント](https://docs.uipath.com/)

