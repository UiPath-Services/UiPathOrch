---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuRole

## SYNOPSIS
UiPath Orchestrator から Document Understanding ロールを取得します。

## SYNTAX

`
Get-DuRole [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
`

## DESCRIPTION
Get-DuRole コマンドレットは、UiPath Orchestrator から Document Understanding ロール情報を取得します。Document Understanding ロールは、ドキュメント処理プロジェクト、分類器、抽出器、およびドキュメントタイプを扱うユーザーの権限とアクセスレベルを定義します。

このコマンドレットは、ロールの説明、作成詳細、権限タイプ（BUILTIN または カスタム）、および詳細なアクション権限を含む包括的なロール情報を返します。各ロールには、プロジェクトの読み取り、分類器の管理、ドキュメントタイプの更新、およびプロジェクトパフォーマンスの監視などの様々な Document Understanding 操作に対する細かい権限を定義する特定の actionDetails が含まれています。

このコマンドレットは、Document Understanding ドライブ（Orch1Du:）上で動作し、ワイルドカードを使用したロール名によるフィルタリングをサポートします。

-Name と -Path パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント: GET /{partitionGlobalId}/pap_/api/roles?scopeType=project&serviceName=DocumentUnderstanding

OAuth 必須スコープ: [PLACEHOLDER - Document Understanding ロールスコープ]

必要な権限: [PLACEHOLDER - Document Understanding ロール権限]

## EXAMPLES

### Example 1
`powershell
PS C:\> Set-Location Orch1Du:\
PS Orch1Du:\> Get-DuRole
`

現在のインスタンスで利用可能なすべての Document Understanding ロールを取得します。

### Example 2
`powershell
PS Orch1Du:\> Get-DuRole -Name "*Developer*"
`

名前に "Developer" を含む Document Understanding ロールを取得します。

### Example 3
`powershell
PS Orch1Du:\>  = Get-DuRole -Name "DU Data Annotator"
PS Orch1Du:\> .actionDetails | Select-Object name, description | Format-Table
`

DU Data Annotator ロールを取得し、その特定のアクション権限をフォーマットされたテーブルで表示します。

### Example 4
`powershell
PS Orch1Du:\> Get-DuRole | Where-Object type -eq "BUILTIN"
`

（カスタムロールとは対照的に）組み込みの Document Understanding ロールのみを取得します。

### Example 5
`powershell
PS Orch1Du:\>  = Get-DuRole | Select-Object -First 1
PS Orch1Du:\>  | ConvertTo-Json -Depth 3
`

最初のロールを取得し、詳細な actionDetails を含むすべてのプロパティを調べるために、その完全な構造を JSON 形式で表示します。

## PARAMETERS

### -Name
取得する Document Understanding ロールの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space] または [Tab] を押すことで自動補完を使用できます。指定しない場合、すべてのロールが返されます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All roles
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Path
ターゲットとする Document Understanding ドライブの名前を指定します。指定しない場合は、現在のドライブがターゲットになります。このパラメーターは、複数の Document Understanding インスタンスを指定するためのパイプライン入力を受け取ります。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新 (Write-Progress コマンドレットによって生成される進行状況バーなど) に対して PowerShell が応答する方法を指定します。有効な値は次のとおりです: SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### CommonParameters
このコマンドレットは共通パラメーターをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### System.String[]

## OUTPUTS

### UiPath.PowerShell.Entities.DuRole

## NOTES
- このコマンドレットは Document Understanding ドライブ（Orch1Du:）上で動作します
- 一般的な組み込みロールには次があります: DU Data Annotator、DU Developer、DU Model Trainer、DU Project Administrator、および DU Viewer
- actionDetails プロパティには、特定の Document Understanding 操作に対する細かい権限が含まれています
- ロールタイプには、BUILTIN（事前定義されたシステムロール）と管理者が作成したカスタムロールがあります
- 各アクション詳細は、namespace（DOCUMENTUNDERSTANDING）、リソースアクション（Read、Update、Create、Delete）、およびリソースグループを指定します
- 完全な actionDetails 構造を調べ、特定の権限を理解するには、ConvertTo-Json を使用してください

## RELATED LINKS

[Get-DuUser](Get-DuUser.md)
[Add-DuUser](Add-DuUser.md)
[Remove-DuRoleFromDuUser](Remove-DuRoleFromDuUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
