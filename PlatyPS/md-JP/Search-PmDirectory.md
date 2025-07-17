---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Search-PmDirectory

## SYNOPSIS
Platform Managementでディレクトリユーザーとグループを検索します。

## SYNTAX

```
Search-PmDirectory [-Name] <String> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Search-PmDirectory コマンドレットは、名前パターンに基づいてUiPath Platform Managementでディレクトリユーザーとグループを検索します。このコマンドレットは、指定された検索語で始まる名前を検索することで、ディレクトリ全体のユーザー、グループ、ロボットユーザーを見つける方法を提供します。

検索結果には、ユーザー（DirectoryUser）、グループ（DirectoryGroup）、ロボットユーザー（DirectoryRobotUser）などのディレクトリオブジェクトに関する包括的な情報が含まれ、識別子、表示名、メールアドレス、ソース情報も含まれます。

これは、任意のUiPathOrchドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作するPlatform Management APIコマンドレットで、自動化ワークフローと管理タスクでのユーザーとグループの発見に不可欠な機能を提供します。

検索は「前方一致」パターンを使用して実行され、名前や識別子の開始部分がわかっている場合にディレクトリオブジェクトを効率的に見つけることができます。

主要エンドポイント：GET /api/Directory/Search/{partitionGlobalId}?startsWith={userName}

OAuth必須スコープ：[PLACEHOLDER - Platform Management directory search scopes]

必須権限：[PLACEHOLDER - Directory search permissions]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Search-PmDirectory y
```

名前が "y" で始まるすべてのディレクトリオブジェクト（ユーザー、グループ、ロボットユーザー）を検索します。

### Example 2
```powershell
PS Orch1:\> Search-PmDirectory user@example.com
```

"user@example.com" と完全に一致するか、それで始まるディレクトリオブジェクトを検索します。

### Example 3
```powershell
PS Orch1:\> Search-PmDirectory admin | Where-Object objectType -eq "DirectoryGroup"
```

"admin" で始まるディレクトリオブジェクトを検索し、グループのみを表示するようにフィルタリングします。

### Example 4
```powershell
PS Orch1:\> $result = Search-PmDirectory user@example.com
PS Orch1:\> $result | ConvertTo-Json -Depth 3
```

ディレクトリオブジェクトを検索し、すべてのプロパティを探索するために完全な構造をJSON形式で表示します。

### Example 5
```powershell
PS Orch1:\> Search-PmDirectory y | Select-Object identityName, displayName, objectType
```

"y" で始まるディレクトリオブジェクトを検索し、主要なプロパティを表示します。

## PARAMETERS

### -Name
検索する名前パターンを指定します。検索では、この値で始まる名前を持つディレクトリオブジェクトを見つけます。このパラメーターは必須で、ユーザー、グループ、ロボットユーザーの検索をサポートします。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
対象ドライブの名前を指定します。指定しない場合、現在のドライブが対象になります。このパラメーターは、パイプライン入力を受け入れます。これはPlatform Management APIコマンドレットなので、任意のUiPathOrchドライブで動作します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String
### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmDirectoryEntityInfo
## NOTES
- このコマンドレットは、Platform Management APIを使用し、任意のUiPathOrchドライブで動作します
- 検索は完全一致ではなく「前方一致」パターンを使用します
- オブジェクトタイプには、DirectoryUser（個々のユーザー）、DirectoryGroup（グループ）、DirectoryRobotUser（ロボットアカウント）が含まれます
- typeプロパティには、オブジェクトタイプに対応する数値が含まれます
- sourceプロパティは、ディレクトリオブジェクトの起源を示します（通常は「local」）
- 完全なオブジェクト構造を探索し、利用可能なすべてのプロパティを理解するには、ConvertTo-Jsonを使用してください
- このコマンドレットは、自動化スクリプトと管理タスクでのユーザー/グループの発見に役立ちます

## RELATED LINKS

[Get-PmUser](Get-PmUser.md)
[Get-PmGroup](Get-PmGroup.md)
[Search-OrchDirectory](Search-OrchDirectory.md)
[about_UiPathOrch](about_UiPathOrch.md)
