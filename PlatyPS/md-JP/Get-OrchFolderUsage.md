---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchFolderUsage

## SYNOPSIS
UiPath Orchestratorフォルダー内のエンティティ数を示すフォルダー使用統計を取得します。

## SYNTAX

```
Get-OrchFolderUsage [[-Path] <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
`Get-OrchFolderUsage` コマンドレットは、UiPath Orchestratorフォルダーの包括的な使用統計を取得し、各フォルダー内に含まれるプロセス、ロボット、キュー、アセット、その他のリソースなどのさまざまなエンティティの数を表示します。

このコマンドレットは、自動化リソースがフォルダー階層全体でどのように分散されているかを表示することで、リソース管理、容量計画、組織分析に貴重な洞察を提供します。使用率の高いフォルダー、未使用のリソースを特定し、リソース配分を最適化するのに役立ちます。

このコマンドレットはフォルダーエンティティで動作し、フォルダー階層全体を分析するための再帰的トラバーサルをサポートします。エンティティタイプ、数、フォルダーパスを含む詳細な統計を返すため、監査およびレポート目的に有用です。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetEntityUsage(folderId={folderId})

OAuth必要スコープ: OR.Folders または OR.Folders.Read

必要な権限: Folders.View

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-OrchFolderUsage
```

現在のフォルダーの使用統計を取得します。

### Example 2
```powershell
PS C:\> Get-OrchFolderUsage -Recurse
```

現在の場所から始まって、すべてのフォルダーの使用統計を再帰的に取得します。

### Example 3
```powershell
PS C:\> Get-OrchFolderUsage -Path Orch1:\Production -Recurse
```

Productionフォルダー下のすべてのフォルダーの使用統計を再帰的に取得します。

### Example 4
```powershell
PS C:\> Get-OrchFolderUsage -Recurse -Depth 2
```

現在の場所から最大2レベルの深さまでのフォルダーの使用統計を取得します。

### Example 5
```powershell
PS C:\> Get-OrchFolderUsage -Recurse | Select-Object Path, Type, Count | Sort-Object Count -Descending
```

使用統計を取得し、数の降順で並べ替えて表示します。

### Example 6
```powershell
PS C:\> Get-OrchFolderUsage -Recurse | ConvertTo-Json -Depth 3
```

使用統計を取得し、詳細分析のために完全なオブジェクト構造をJSON形式で表示します。

### Example 7
```powershell
PS C:\> Get-OrchFolderUsage -Recurse | Where-Object {$_.Type -eq "QueueItem" -and $_.Count -gt 1000} | Select-Object Path, Count
```

容量計画とキュー管理のために、キューアイテム数が多い（1000を超える）フォルダーを見つけます。

### Example 8
```powershell
PS C:\> Get-OrchFolderUsage -Recurse | Group-Object Category | Select-Object Name, Count, @{Name="TotalItems";Expression={(.Group | Measure-Object Count -Sum).Sum}}
```

使用統計をカテゴリ別にグループ化し、すべてのフォルダーにわたる各カテゴリの総アイテム数を表示します。これは、タイプ別のリソース分布の概要を取得するのに便利です。

## PARAMETERS

### -Depth
再帰操作の最大深度レベルを指定します。-Recurseが指定されているときに、再帰検索がどの程度深く進むべきかを制限するためにこのパラメーターを使用します。

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

### -Path
使用情報を検索するフォルダーパスを指定します。パターンマッチングのためにワイルドカード文字（*と?）をサポートします。現在の場所を変更せずに特定のフォルダーをターゲットにしたい場合に、このパラメーターを使用します。

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

### -ProgressAction
このコマンドの処理中にスクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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
サブフォルダーから使用統計を再帰的に取得します。指定すると、コマンドレットは指定されたパスから始まるフォルダー階層全体をトラバースします。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
### System.Nullable`1[[System.Int64, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
### System.String
## OUTPUTS

### UiPath.PowerShell.Entities.EntitySummary
## NOTES

このコマンドレットはフォルダーエンティティで動作するため、特定のフォルダーに移動するか、-Pathパラメーターを使用して目的の場所をターゲットにする必要がある場合があります。

大きなフォルダー階層で-Recurseパラメーターを使用する場合、操作が完了するまでに相当な時間がかかる場合があります。必要に応じて、-Depthを使用してトラバーサルスコープを制限することを検討してください。

使用統計は、リソース分布パターンを特定するのに役立ち、以下の目的に価値があります：
- 容量計画とリソース最適化
- 未使用または利用率の低いフォルダーの特定
- リソースの成長と使用傾向の監視
- 組織分析とレポート

パフォーマンス分析とレポートについては、結果をCSV形式でエクスポートするか、Select-Objectを使用して特定のメトリクスに焦点を当てることを検討してください。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetEntitiesSummary
OAuth必要スコープ: OR.Folders または OR.Folders.Read
必要な権限: Units.View

## RELATED LINKS

[Get-OrchFolder](Get-OrchFolder.md)
[Get-OrchProcess](Get-OrchProcess.md)
[Get-OrchQueue](Get-OrchQueue.md)
[Get-OrchAsset](Get-OrchAsset.md)
