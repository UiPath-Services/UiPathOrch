---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchQueue

## SYNOPSIS
キューを作成します。

## SYNTAX

```
New-OrchQueue [-Name] <String[]> [-Description <String>] [-AcceptAutomaticallyRetry <String>]
 [-RetryAbandonedItems <String>] [-MaxNumberOfRetries <Int32>] [-EnforceUniqueReference <String>]
 [-Encrypted <String>] [-Release <String>] [-SlaInMinutes <Int32>] [-RiskSlaInMinutes <Int32>]
 [-SpecificDataJsonSchema <String>] [-OutputDataJsonSchema <String>] [-AnalyticsDataJsonSchema <String>]
 [-RetentionAction <String>] [-RetentionPeriod <Int32>] [-RetentionBucket <String>]
 [-StaleRetentionAction <String>] [-StaleRetentionPeriod <Int32>] [-StaleRetentionBucket <String>]
 [-Tags <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestratorフォルダに新しいキューを作成します。キューは、ロボットが組織的な方法で情報やワークアイテムを交換できるデータリポジトリで、有人と無人の両方のオートメーションシナリオをサポートします。

このコマンドレットは、CSVインポート機能をサポートしています。インポート可能なCSVの形式は、Get-OrchQueue -Recurse -ExportCsvを使用して取得できます。これにより、一括キュー作成と設定が可能になります。

キューエンティティはフォルダスコープです。フォルダに移動するか、-Pathパラメータを使用してターゲットフォルダを指定する必要があります。

プライマリ エンドポイント: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue, GET /odata/Releases, GET /odata/Buckets

OAuth 必要なスコープ: OR.Queues.Write

必要な権限: Queues.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> New-OrchQueue InvoiceQueue -WhatIf
```

実際に作成せずに、キューを作成する場合の結果を表示します。

### Example 2
```powershell
PS Orch1:\Shared> New-OrchQueue InvoiceQueue -Description "Queue for invoice processing"
```

説明付きの基本的なキューを作成します。

### Example 3
```powershell
PS Orch1:\Shared> New-OrchQueue PaymentQueue -MaxNumberOfRetries 5 -Encrypted true
```

カスタムリトライ設定で暗号化されたキューを作成します。

### Example 4
```powershell
PS Orch1:\Shared> New-OrchQueue OrderQueue -SlaInMinutes 240 -RiskSlaInMinutes 480
```

標準およびリスク処理用のSLA設定でキューを作成します。

### Example 5
```powershell
PS Orch1:\> New-OrchQueue -Path Orch1:\Shared, Orch1:\Finance TestQueue
```

複数のフォルダにキューを作成します。

### Example 6
```powershell
PS Orch1:\Shared> Import-Csv queues.csv | New-OrchQueue
```

パイプライン入力を使用してCSVファイルから複数のキューを作成します。

### Example 7
```powershell
PS Orch1:\Shared> New-OrchQueue CriticalQueue -EnforceUniqueReference true -Tags Priority, Urgent
```

一意参照の強制とタグ付きでキューを作成します。

## PARAMETERS

### -AcceptAutomaticallyRetry
作成するキューのAcceptAutomaticallyRetryを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AnalyticsDataJsonSchema
作成するキューのAnalyticsDataJsonSchemaを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
キューを作成する前に確認を求めます。複数のキューを作成する場合に推奨されます。

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

### -Description
キューの目的と使用方法を説明する説明を指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Encrypted
キューデータを暗号化するかどうかを指定します。有効な値：true、false。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EnforceUniqueReference
キューアイテムが一意の参照を持つ必要があるかどうかを指定します。有効な値：true、false。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MaxNumberOfRetries
失敗したキューアイテムの最大リトライ試行回数を指定します。デフォルトは通常1です。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
作成するキューの名前を指定します。一括作成用に複数の値をサポートしています。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OutputDataJsonSchema
作成するキューのOutputDataJsonSchemaを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ターゲットフォルダを指定します。複数のフォルダにはカンマ区切り値を使用します。ワイルドカードをサポートしています。指定されていない場合は、現在のフォルダをターゲットにします。

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

### -Release
作成するキューのReleaseを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -RetentionAction
保持期間が終了したときに取るアクションを指定します。有効な値には、Delete、Moveが含まれます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RetentionBucket
作成するキューのRetentionBucketを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -RetentionPeriod
完了したキューアイテムの保持期間を日数で指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RetryAbandonedItems
作成するキューのRetryAbandonedItemsを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RiskSlaInMinutes
リスク処理シナリオのサービスレベル合意時間を分単位で指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SlaInMinutes
標準処理のサービスレベル合意時間を分単位で指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SpecificDataJsonSchema
作成するキューのSpecificDataJsonSchemaを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Tags
キューの分類と整理のためのタグを指定します。

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

### -WhatIf
実際にキューを作成せずに、コマンドレットを実行した場合の結果を表示します。安全性検証のために推奨されます。

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

### -ProgressAction
コマンドレット実行中の進捗情報の表示方法を制御します。

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

### -StaleRetentionAction
保持期間が終了したときの古いキューアイテムに対するアクションを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StaleRetentionBucket
{{ Fill StaleRetentionBucket Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -StaleRetentionPeriod
古いキューアイテムの保持期間を日数で指定します。

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition
## NOTES
キューエンティティはフォルダスコープです。フォルダに移動するか、-Pathパラメータを使用してターゲットフォルダを指定する必要があります。

このコマンドレットは、一括キュー作成用にImport-Csv | New-OrchQueueを使用したCSVインポート機能をサポートしています。正しいCSV形式を生成するには、Get-OrchQueue -ExportCsvを使用してください。

複数のキューを作成する場合は特に、実際の実行前にキュー作成をプレビューするために-WhatIfを使用してください。

キュー設定には、オートメーションワークフローを管理するためのリトライポリシー、暗号化設定、SLA設定、および保持ポリシーが含まれます。

## RELATED LINKS

[Get-OrchQueue](Get-OrchQueue.md)

[Update-OrchQueue](Update-OrchQueue.md)

[Remove-OrchQueue](Remove-OrchQueue.md)

[Copy-OrchQueue](Copy-OrchQueue.md)
