---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmProjectPermission

## SYNOPSIS
UiPath Test Managerでプロジェクトのユーザー権限を取得します。

## SYNTAX

```
Get-TmProjectPermission [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-TmProjectPermissionコマンドレットは、Test Managerプロジェクトのユーザー権限情報を取得します。これには、どのユーザーが特定のプロジェクトにアクセスできるか、割り当てられたロール、所有者ステータス、および権限変更履歴に関する詳細が含まれます。

このコマンドレットは、UiPathOrchTmプロバイダーで動作し、フォルダーエンティティ操作です。Set-Location（cd）を使用して特定のTest Managerプロジェクトフォルダーに移動するか、-Pathまたは-Recurseパラメータを使用して対象フォルダーを指定する必要があります。

このコマンドレットは、UiPathOrchTmプロバイダーのPSDriveで動作します。構成ファイルのスコープに「TM.」が含まれている場合、UiPathOrchTmプロバイダーのPSDriveが自動的に追加されます。これはGet-PSDriveコマンドレットで確認できます。構成ファイルは、Edit-OrchConfigコマンドレットで開くことができます。

プライマリ エンドポイント: [PLACEHOLDER - GET /testmanager_/api/v2/{projectId}/permissions/project]

OAuth 必要スコープ: [PLACEHOLDER - TM.Projects or TM.Projects.Read]

必要な権限: [PLACEHOLDER - ProjectSettings.Update]

## EXAMPLES

### Example 1: 現在のフォルダーからプロジェクト権限を取得
```powershell
PS Orch1Tm:\TestProject> Get-TmProjectPermission
```

特定のTest Managerプロジェクトフォルダーに移動し、そのプロジェクトのユーザー権限を取得します。

### Example 2: 再帰的にプロジェクト権限を取得
```powershell
PS Orch1Tm:\> Get-TmProjectPermission -Recurse
```

ルートフォルダーから再帰的にすべてのTest Managerプロジェクトのユーザー権限を取得します。

### Example 3: 複数のパスからプロジェクト権限を取得
```powershell
PS C:\> Get-TmProjectPermission -Path Orch1Tm:\TestProject, Orch1Tm:\MyProject
```

特定のパスを使用して、複数のTest Managerプロジェクトからユーザー権限を取得します。

### Example 4: 権限の詳細を取得して構造を調べる
```powershell
PS Orch1Tm:\> Get-TmProjectPermission -Recurse | Select-Object -First 1 | ConvertTo-Json -Depth 5
```

最初の権限レコードを取得し、詳細な分析のために完全なオブジェクト構造をJSON形式で表示します。

### Example 5: 最近の権限変更を確認
```powershell
PS Orch1Tm:\> Get-TmProjectPermission -Recurse | Where-Object {$_.lastUpdated -gt (Get-Date).AddDays(-30)}
```

過去30日間に更新されたプロジェクト権限を取得し、最近の権限変更を追跡します。

## PARAMETERS

### -Path
Test Managerプロジェクトの対象フォルダーパスを指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。現在の場所を変更せずに特定のプロジェクトフォルダーを対象にしたい場合に、このパラメータを使用します。

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
すべてのサブフォルダーで再帰的にプロジェクト権限を検索します。指定された場合、コマンドレットはTest Managerプロジェクト階層全体を検索します。

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
スクリプト、コマンドレット、またはプロバイダーによって生成される進捗更新にPowerShellがどのように応答するかを指定します。

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
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメータをサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TmProjectPermission
## NOTES
- このコマンドレットは、UiPathOrchTmプロバイダー内のフォルダーエンティティで動作します
- Test Manager機能へのアクセスと、構成での適切なTMスコープが必要です
- 最適なパフォーマンスを得るために、-Pathと-Recurseパラメータをコマンドレット名の直後に配置して、適切な自動補完を有効にしてください
- 一般的なロールには、「Project Owner」、「Test Manager」、および「Viewer」が含まれます
- ユーザーオブジェクトには、メール、表示名、ID情報を含む、割り当てられたユーザーに関する詳細情報が含まれています
- isOwnerプロパティは、ユーザーがプロジェクトの所有者権限を持っているかどうかを示します
- フィルタリング操作を使用して、権限パターンを分析し、セキュリティコンプライアンス問題を特定します

## RELATED LINKS

[Get-TmProjectSetting]()

[Get-TmTestCase]()

[Get-TmTestSet]()

[Get-TmRequirement]()
