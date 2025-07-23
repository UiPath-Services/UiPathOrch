---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-DuRole

## SYNOPSIS
UiPath OrchestratorからDocument Understandingロールを取得します。

## SYNTAX

```
Get-DuRole [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-DuRole コマンドレットは、UiPath OrchestratorからDocument Understandingロール情報を取得します。Document Understandingロールは、ドキュメント処理プロジェクト、分類器、抽出器、およびドキュメントタイプを扱うユーザーの権限とアクセスレベルを定義します。

このコマンドレットは、ロールの説明、作成の詳細、権限タイプ（BUILTIN vs カスタム）、詳細なアクション権限を含む包括的なロール情報を返します。各ロールには、プロジェクトの読み取り、分類器の管理、ドキュメントタイプの更新、プロジェクトのパフォーマンス監視など、さまざまなDocument Understanding操作に対する細かい権限を定義する具体的なactionDetailsが含まれます。

このコマンドレットは、Document Understandingドライブ（Orch1Du:）で動作し、ワイルドカードを使用したロール名によるフィルタリングをサポートします。

-Nameと-Pathパラメーターの複数の値は、ワイルドカードを含むカンマ区切りテキストを使用して指定できます。さらに、[Ctrl+Space]または[Tab]を押すことで、これらの値の自動補完を使用できます。

主要エンドポイント：GET /{partitionGlobalId}/pap_/api/roles?scopeType=project&serviceName=DocumentUnderstanding

OAuth必須スコープ：[PLACEHOLDER - Document Understandingロールスコープ]

必須権限：[PLACEHOLDER - Document Understandingロール権限]

## EXAMPLES

### Example 1
```powershell
PS C:\> cd Orch1Du:\
PS Orch1Du:\> Get-DuRole
```

現在のインスタンスで利用可能なすべてのDocument Understandingロールを取得します。

### Example 2
```powershell
PS Orch1Du:\> Get-DuRole *developer*
```

名前に "Developer" を含むDocument Understandingロールを取得します。

### Example 3
```powershell
PS Orch1Du:\> $role = Get-DuRole "DU Data Annotator"
PS Orch1Du:\> $role.actionDetails | Select-Object name, description | Format-Table
```

DU Data Annotatorロールを取得し、その具体的なアクション権限をフォーマットされたテーブルで表示します。

### Example 4
```powershell
PS Orch1Du:\> Get-DuRole | Where-Object type -eq "BUILTIN"
```

組み込みのDocument Understandingロールのみを取得します（カスタムロールとは対照的に）。

### Example 5
```powershell
PS Orch1Du:\> $role = Get-DuRole | Select-Object -First 1
PS Orch1Du:\> $role | ConvertTo-Json -Depth 3
```

最初のロールを取得し、詳細なactionDetailsを含むすべてのプロパティを探索するために、完全な構造をJSON形式で表示します。

## PARAMETERS

### -Name
取得するDocument Understandingロールの名前を指定します。ワイルドカードと複数の値をサポートします。[Ctrl+Space]または[Tab]を押すことで自動補完を使用できます。指定しない場合、すべてのロールが返されます。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All roles
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象のDocument Understandingドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、複数のDocument Understandingインスタンスを指定するためのパイプライン入力を受け入れます。

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

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新（Write-Progressコマンドレットによって生成される進行状況バーなど）にPowerShellがどのように応答するかを指定します。有効な値は：SilentlyContinue、Stop、Continue、Inquire、Ignore、Suspend。

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

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.DuRole
## NOTES
- このコマンドレットは、Document Understandingドライブ（Orch1Du:）で動作します
- 一般的な組み込みロールには次のものがあります：DU Data Annotator、DU Developer、DU Model Trainer、DU Project Administrator、DU Viewer
- actionDetailsプロパティには、具体的なDocument Understanding操作に対する細かい権限が含まれます
- ロールタイプには、BUILTIN（事前定義されたシステムロール）と管理者によって作成されたカスタムロールが含まれます
- 各アクション詳細は、namespace（DOCUMENTUNDERSTANDING）、リソースアクション（Read、Update、Create、Delete）、リソースグループを指定します
- 完全なactionDetails構造を探索し、具体的な権限を理解するには、ConvertTo-Jsonを使用してください

## RELATED LINKS

[Get-DuUser](Get-DuUser.md)
[Add-DuUser](Add-DuUser.md)
[Remove-DuRoleFromDuUser](Remove-DuRoleFromDuUser.md)
[about_UiPathOrch](about_UiPathOrch.md)
