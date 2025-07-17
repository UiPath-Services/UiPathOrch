---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-TmProjectSetting

## SYNOPSIS
UiPath Test Managerでプロジェクトの設定を取得します。

## SYNTAX

```
Get-TmProjectSetting [-Path <String[]>] [-Recurse] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-TmProjectSettingコマンドレットは、Test Managerプロジェクトの構成設定を取得します。これらの設定には、プロジェクトプレフィックス、テストステップの最大数、タイムゾーン構成、およびプロジェクト識別子が含まれます。

このコマンドレットは、UiPathOrchTmプロバイダーで動作し、フォルダーエンティティ操作です。Set-Location（cd）を使用して特定のTest Managerプロジェクトフォルダーに移動するか、-Pathまたは-Recurseパラメータを使用して対象フォルダーを指定する必要があります。

このコマンドレットは、UiPathOrchTmプロバイダーのPSDriveで動作します。構成ファイルのスコープに"TM."が含まれている場合、UiPathOrchTmプロバイダーのPSDriveが自動的に追加されます。これはGet-PSDriveコマンドレットで確認できます。構成ファイルは、Edit-OrchConfigコマンドレットで開くことができます。

プライマリ エンドポイント: [PLACEHOLDER - GET /testmanager_/api/v2/{projectId}/projectsettings]

OAuth 必要スコープ: [PLACEHOLDER - TM.Projects or TM.Projects.Read]

必要な権限: [PLACEHOLDER - Project.Read]

## EXAMPLES

### Example 1: 現在のフォルダーからプロジェクト設定を取得
```powershell
PS Orch1Tm:\TestProject> Get-TmProjectSetting
```

特定のTest Managerプロジェクトフォルダーに移動し、その設定を取得します。

### Example 2: 再帰的にプロジェクト設定を取得
```powershell
PS Orch1Tm:\> Get-TmProjectSetting -Recurse
```

ルートフォルダーから再帰的にすべてのTest Managerプロジェクトの設定を取得します。

### Example 3: 複数のパスからプロジェクト設定を取得
```powershell
PS C:\> Get-TmProjectSetting -Path Orch1Tm:\TestProject, Orch1Tm:\MyProject
```

特定のパスを使用して、複数のTest Managerプロジェクトから設定を取得します。

### Example 4: タイムゾーンでプロジェクトをフィルタリング
```powershell
PS Orch1:\> Get-TmProjectSetting -Recurse | Where-Object {$_.projectTimeZone -eq "UTC"} | Select-Object Path, projectPrefix, projectTimeZone, maxNumberOfTestSteps
```

すべてのプロジェクト設定を取得し、UTCタイムゾーンで構成されたプロジェクトをフィルタリングして、主要な構成情報を表示します。

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
すべてのサブフォルダーで再帰的にプロジェクト設定を検索します。指定された場合、コマンドレットはTest Managerプロジェクト階層全体を検索します。

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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.TmProjectSettings
## NOTES
- このコマンドレットは、UiPathOrchTmプロバイダー内のフォルダーエンティティで動作します
- Test Manager機能へのアクセスと、構成での適切なTMスコープが必要です
- 最適なパフォーマンスを得るために、-Pathと-Recurseパラメータをコマンドレット名の直後に配置して、適切な自動補完を有効にしてください
- プロジェクト設定には、最大テストステップ制限やタイムゾーン設定などの構成が含まれます
- projectPrefixは、プロジェクト内のテストケースの一意識別子を生成するために使用されます
- このコマンドレットを使用する前に、Get-PSDriveを使用してUiPathOrchTmプロバイダーが利用可能であることを確認してください

## RELATED LINKS

[Get-TmProjectPermission]()

[Get-TmTestCase]()

[Get-TmTestSet]()

[Get-TmRequirement]()
