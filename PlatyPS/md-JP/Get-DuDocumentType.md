---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuDocumentType

## SYNOPSIS
UiPath OrchestratorからDocument Understandingドキュメントタイプを取得します。

## SYNTAX

```
Get-DuDocumentType [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-DuDocumentTypeコマンドレットは、UiPath OrchestratorからDocument Understandingドキュメントタイプを取得します。ドキュメントタイプは、Document Understandingプロジェクト内で請求書、レシート、フォーム、およびその他のビジネスドキュメントなどの特定のドキュメントカテゴリの構造と処理ルールを定義します。

各ドキュメントタイプには、Id、Name、および完全なドキュメントタイプ定義にアクセスするためのdetailsUrlなどの情報が含まれています。ドキュメントタイプは、特定のドキュメントカテゴリのフィールド抽出ルール、検証ロジック、および処理ワークフローを定義するテンプレートとして機能します。

このコマンドレットはUiPathOrchDuドライブコンテキスト内で動作し、特定のDocument Understandingプロジェクトフォルダーへの移動が必要です。システムには、請求書、レシート、税務フォーム、IDカード、銀行取引明細書、および専門フォームを含む一般的なビジネスドキュメント用の多数の事前定義されたドキュメントタイプが含まれています。

**重要**: このコマンドレットは実行前にUiPathOrchDuドライブ上のDocument Understandingプロジェクトフォルダー（例：Orch1Du:\ProjectName）への移動が必要です。

主要エンドポイント: GET /du_/api/framework/projects/{projectId}/document-types

OAuth必須スコープ: OR.ML または OR.ML.Read

必要な権限: ML.View

## EXAMPLES

### Example 1
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType
```

現在のPredefinedプロジェクトからすべてのドキュメントタイプを取得し、請求書、レシート、税務フォームなどを含む利用可能なドキュメントタイプの広範なリストを表示します。

### Example 2
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType | Where-Object {$_.name -like "*Invoice*"}
```

事前定義されたコレクションから請求書に関連するすべてのドキュメントタイプを取得します。

### Example 3
```powershell
PS C:\> Get-DuDocumentType -Path Orch1Du:\MyProject -Id "*tax*"
```

MyProject Document UnderstandingプロジェクトからIDに"tax"を含むドキュメントタイプを取得します。

### Example 4
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType | ConvertTo-Json -Depth 3 | Select-Object -First 1
```

APIエンドポイントとプロジェクトコンテキストを含む詳細なドキュメントタイププロパティをJSON形式で表示します。

### Example 5
```powershell
PS Orch1Du:\Predefined> Get-DuDocumentType | Where-Object {$_.name -match "Japan|China"} | Select-Object name, id
```

特定の地域バリアント（日本、中国のローカライズされたドキュメント）のドキュメントタイプを取得します。

### Example 6
```powershell
PS Orch1Du:\> Get-DuDocumentType -Recurse | Group-Object Project
```

すべてのドキュメントタイプを含まれるDocument Understandingプロジェクトでグループ化します。

## PARAMETERS

### -Name
{{ Fill Name Description }}

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
検索するターゲットDocument Understandingプロジェクトフォルダーを指定します。UiPathOrchDuドライブパス（例：Orch1Du:\ProjectName）を参照する必要があります。

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
検索操作にターゲットプロジェクトフォルダーとそのすべてのサブプロジェクトを含めます。包括的なドキュメントタイプの発見に不可欠です。

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
{{ Fill ProgressAction Description }}

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

### UiPath.PowerShell.Entities.DuDocumentType
## NOTES
このコマンドレットはUiPathOrchDuドライブ上のDocument Understandingプロジェクト内で動作します。実行前に特定のプロジェクトフォルダーへの移動が必要です。ドキュメントタイプは、特定のドキュメントカテゴリの構造、フィールド抽出ルール、および処理ロジックを定義します。Predefinedプロジェクトには、一般的なビジネスシナリオ用の多数の事前構築されたドキュメントタイプが含まれています。専門的な処理要件にはカスタムドキュメントタイプを作成できます。この操作には、Document Understandingプロジェクト内でのML.View権限が必要です。

## RELATED LINKS

[Get-DuClassifier](Get-DuClassifier.md)

[Get-DuExtractor](Get-DuExtractor.md)

[Add-DuDocumentType](Add-DuDocumentType.md)

[Set-DuDocumentType](Set-DuDocumentType.md)
