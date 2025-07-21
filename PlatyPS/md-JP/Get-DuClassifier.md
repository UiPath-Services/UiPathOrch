---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuClassifier

## SYNOPSIS
UiPath OrchestratorからDocument Understanding分類器を取得します。

## SYNTAX

```
Get-DuClassifier [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-DuClassifierコマンドレットは、UiPath OrchestratorからDocument Understanding分類器を取得します。分類器は、Document Understandingプロジェクト内でドキュメントを分類・カテゴライズするために使用されるAIモデルで、自動化されたドキュメントタイプ識別と処理ワークフローを可能にします。

各分類器には、Id、Name、Status（Available、Training等）、および同期・非同期分類操作用のAPIエンドポイントなどの情報が含まれています。分類器はDocument Understandingプロジェクト内で組織化され、インテリジェントなドキュメント処理自動化の基盤を提供します。

このコマンドレットはUiPathOrchDuドライブコンテキスト内で動作し、特定のDocument Understandingプロジェクトフォルダーへの移動が必要です。**重要**: このコマンドレットは実行前にUiPathOrchDuドライブ上のDocument Understandingプロジェクトフォルダー（例：Orch1Du:\ProjectName）への移動が必要です。

主要エンドポイント: GET /du_/api/framework/projects/{projectId}/classifiers

OAuth必須スコープ: OR.ML または OR.ML.Read

必要な権限: ML.View

## EXAMPLES

### Example 1
```powershell
PS Orch1Du:\Predefined> Get-DuClassifier
```

現在のPredefinedプロジェクトからすべての分類器を取得し、id、name、statusの情報を表示します。

### Example 2
```powershell
PS Orch1Du:\Predefined> Get-DuClassifier | ConvertTo-Json -Depth 3
```

APIエンドポイント（detailsUrl、syncUrl、asyncUrl）を含む詳細な分類器プロパティをJSON形式で表示します。

### Example 3
```powershell
PS C:\> Get-DuClassifier -Path Orch1Du:\MyProject -Id "*generative*"
```

MyProject Document UnderstandingプロジェクトからIDに"generative"を含む分類器を取得します。

### Example 4
```powershell
PS Orch1Du:\> Get-DuClassifier -Recurse | Where-Object {$_.status -eq "Available"}
```

すべてのDocument Understandingプロジェクトにわたって、利用可能なすべての分類器を取得します。

### Example 5
```powershell
PS Orch1Du:\Predefined> Get-DuClassifier | Select-Object name, status, @{Name="HasSyncAPI";Expression={$_.syncUrl -ne $null}}
```

分類器の名前、ステータス、および同期APIエンドポイントが利用可能かどうかを表示します。

### Example 6
```powershell
PS Orch1Du:\> Get-DuClassifier -Recurse | Group-Object status
```

Document Understandingプロジェクト全体ですべての分類器をステータス別にグループ化します。

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
検索操作にターゲットプロジェクトフォルダーとそのすべてのサブプロジェクトを含めます。包括的な分類器の発見に不可欠です。

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

### UiPath.PowerShell.Entities.DuClassifier
## NOTES
このコマンドレットはUiPathOrchDuドライブ上のDocument Understandingプロジェクト内で動作します。実行前に特定のプロジェクトフォルダーへの移動が必要です。分類器は、インテリジェントなドキュメント処理のためのAIを活用したドキュメント分類機能を提供します。この操作には、Document Understandingプロジェクト内でのML.View権限が必要です。

## RELATED LINKS

[Get-DuDocumentType](Get-DuDocumentType.md)

[Get-DuExtractor](Get-DuExtractor.md)

[Add-DuClassifier](Add-DuClassifier.md)

[Train-DuClassifier](Train-DuClassifier.md)
