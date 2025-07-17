---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuExtractor

## SYNOPSIS
UiPath OrchestratorからDocument Understandingの抽出器を取得します。

## SYNTAX

```
Get-DuExtractor [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
`Get-DuExtractor`コマンドレットは、OrchestratorのUiPath Document Understandingプロジェクトから抽出器情報を取得します。抽出器は、ドキュメント処理ワークフロー中にドキュメントから特定のデータフィールドを抽出する機械学習モデルまたはルールベースのコンポーネントです。

このコマンドレットはDocument Understandingプロジェクト内のフォルダーエンティティで動作し、ターゲットプロジェクトフォルダーへの移動か、-Path、-Recurseパラメーターを使用したターゲットフォルダーの指定が必要です。プロジェクト内で作成されたカスタム抽出器と、使用可能な事前訓練済み抽出器の両方を取得できます。

-Nameおよび-Pathパラメーターには、ワイルドカードを含むコンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値のオートコンプリートを使用できます。

主要エンドポイント: GET /odata/ML/Extractors

OAuth必須スコープ: OR.ML または OR.ML.Read

必要な権限: ML.Extractor.View

## EXAMPLES

### Example 1
```powershell
PS Orch1Du:\MyProject> Get-DuExtractor
```

現在のDocument Understandingプロジェクト内のすべての抽出器を取得します。

### Example 2
```powershell
PS Orch1Du:\> Get-DuExtractor -Recurse
```

すべてのDocument Understandingプロジェクトから再帰的にすべての抽出器を取得します。

### Example 3
```powershell
PS Orch1Du:\MyProject> Get-DuExtractor "Invoice*"
```

ワイルドカードパターンマッチングを使用して、名前が"Invoice"で始まる抽出器を取得します。

### Example 4
```powershell
PS Orch1Du:\> Get-DuExtractor -Recurse | Where-Object {$_.ExtractorType -eq "MachineLearning"} | Select-Object Path, Name, ExtractorType, Status
```

すべてのプロジェクトからすべての機械学習抽出器を取得し、主要なプロパティを表示します。各抽出器がどのプロジェクトに属するかを識別するために、Pathが最初に選択されていることに注意してください。

### Example 5
```powershell
PS Orch1Du:\MyProject> Get-DuExtractor | Where-Object {$_.Status -eq "Published"}
```

現在のプロジェクトから公開済みの抽出器のみを取得します。

### Example 6
```powershell
PS Orch1Du:\> Get-DuExtractor -Path "Orch1Du:\InvoiceProject", "Orch1Du:\ContractProject"
```

現在の場所を変更せずに、特定のDocument Understandingプロジェクトから抽出器を取得します。

### Example 7
```powershell
PS Orch1Du:\MyProject> Get-DuExtractor | ConvertTo-Json -Depth 3
```

すべての抽出器を取得し、詳細なプロパティを含む完全なオブジェクト構造を表示します。

### Example 8
```powershell
PS Orch1Du:\> Get-DuExtractor -Recurse | Group-Object ExtractorType | Select-Object Name, Count
```

抽出器をタイプ別にグループ化し、すべてのプロジェクトにわたって各タイプの数を表示します。

## PARAMETERS

### -Name
取得する抽出器の名前を指定します。パターンマッチング用のワイルドカード（*および?）をサポートします。[Ctrl+Space]または[Tab]を押すことでオートコンプリートを使用できます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All extractors
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
抽出器を検索するDocument Understandingプロジェクトパスを指定します。ワイルドカードと複数の値をサポートします。指定されていない場合、現在の場所が使用されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
抽出器を取得するときに、すべてのサブプロジェクトからの抽出器を含めます。これはフォルダーエンティティ操作パラメーターです。

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

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新にPowerShellがどのように応答するかを指定します。

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

### UiPath.PowerShell.Entities.DuExtractor
## NOTES

このコマンドレットはDocument Understandingプロジェクト内のフォルダーエンティティで動作し、以下のいずれかが必要です：
- Set-Location（cd）を使用したターゲットプロジェクトへの移動、または
- -Pathまたは-Recurseパラメーターを使用したターゲットプロジェクトの指定

**重要:** 最適なPowerShell IntelliSenseサポートのために、複数のパラメーターを使用する場合は、他のパラメーターの前に-Pathまたは-Recurseを指定してください。

**抽出器情報:**
- Name: 抽出器の表示名
- ExtractorType: 抽出器のタイプ（MachineLearning、Regexなど）
- Status: 現在のステータス（Published、Draft、Trainingなど）
- CreatedBy: 抽出器を作成したユーザー
- CreatedDate: 抽出器が作成された日時
- Version: 抽出器の現在のバージョン

**一般的な抽出器タイプ:**
- MachineLearning: ラベル付きデータで訓練されたAIベースの抽出器
- Regex: 正規表現を使用するルールベースの抽出器
- FormExtractor: 構造化フォーム用の抽出器
- KeywordBased: 特定のキーワードを探す抽出器

**一般的なステータス値:**
- Published: 抽出器は本番環境で使用準備完了
- Draft: 抽出器は開発中
- Training: 機械学習抽出器が訓練中
- Failed: 訓練または展開が失敗

**使用例:**
- プロジェクト間での抽出器開発の監視
- ドキュメント処理ワークフロー用の利用可能な抽出器の識別
- 機械学習抽出器の訓練ステータスの確認
- 組織全体での抽出器使用状況とパフォーマンスの監査

**パス選択に関する重要な注意:**
フォルダーエンティティでSelect-Objectを使用する場合は、各抽出器がどのプロジェクトに属するかを識別するために、常にPathを最初のプロパティとして含めてください。これは、複数のDocument Understandingプロジェクト間で抽出器を管理するために不可欠です。

詳細な設定とパフォーマンスメトリクスを含む完全な抽出器オブジェクト構造を探索するには、ConvertTo-Jsonを使用してください。

## RELATED LINKS

[Get-DuClassifier](Get-DuClassifier.md)

[Get-DuDocumentType](Get-DuDocumentType.md)
