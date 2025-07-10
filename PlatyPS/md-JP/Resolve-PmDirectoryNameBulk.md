---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Resolve-PmDirectoryNameBulk

## SYNOPSIS
UiPath Platform Management でディレクトリエンティティ名を詳細情報に解決します。

## SYNTAX

```
Resolve-PmDirectoryNameBulk [-EntityType] <String> [-Name] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Resolve-PmDirectoryNameBulk コマンドレットは、UiPath Platform Management でディレクトリエンティティ名（ユーザー、グループ、またはアプリケーション）を詳細なディレクトリ情報に解決します。このコマンドレットは、複数のエンティティ名を一括解決して、識別子、表示名、組織情報、ソース詳細などの包括的なディレクトリの詳細を取得するのに役立ちます。

このコマンドレットは Platform Management API にアクセスし、すべての UiPath Orchestrator ドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

プライマリ エンドポイント: POST /api/Directory/BulkResolveByName/{partitionGlobalId}

OAuth 必要なスコープ: [PLACEHOLDER]

必要な権限: [PLACEHOLDER]

## EXAMPLES

### Example 1: 複数のユーザーを名前で解決
```powershell
PS Orch1:\> Resolve-PmDirectoryNameBulk User user1@example.com, user2@example.com
```

複数のユーザー名を単一の操作で解決して、ディレクトリ情報を取得します。

### Example 2: 複数のグループを解決
```powershell
PS Orch1:\> Resolve-PmDirectoryNameBulk Group Administrators, Users, Everyone
```

複数のグループ名を解決して、ディレクトリ情報を取得します。

### Example 3: 解決して構造を検査
```powershell
PS Orch1:\> Resolve-PmDirectoryNameBulk User user@example.com | ConvertTo-Json -Depth 5
```

ユーザー名を解決し、詳細な分析のために完全なオブジェクト構造を JSON 形式で表示します。

### Example 4: 複数のドライブにわたって解決
```powershell
PS C:\> Resolve-PmDirectoryNameBulk -Path Orch1:, Orch2: User user@example.com
```

指定された複数の Orchestrator ドライブにわたってユーザー名を解決します。

## PARAMETERS

### -EntityType
解決するディレクトリエンティティのタイプを指定します。有効な値には「User」、「Group」、「Application」が含まれます。このパラメーターは、返されるディレクトリオブジェクトのタイプを決定します。

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

### -Name
解決するディレクトリエンティティの名前を指定します。このパラメーターは、一括解決操作のために名前の配列を受け入れます。名前はディレクトリエンティティ名と正確に一致する必要があります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。Platform Management API はすべての Orchestrator ドライブで動作します。

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

### -ProgressAction
スクリプト、コマンドレット、またはプロバイダーによって生成される進行状況の更新に PowerShell がどのように応答するかを指定します。

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
このコマンドレットは共通パラメーター（-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable）をサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String
### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.DirectoryUser
### UiPath.PowerShell.Entities.DirectoryGroup
### UiPath.PowerShell.Entities.DirectoryApplication
## NOTES
- このコマンドレットは、複数のエンティティを解決する際のパフォーマンスを向上させるために一括解決操作用に設計されています
- source プロパティはディレクトリソース（例：「local」、Azure Active Directory の場合は「aad」）を示します
- エンティティタイプは「User」、「Group」、または「Application」として正確に指定する必要があります
- 解決を成功させるには、名前がディレクトリエンティティ名と正確に一致する必要があります
- Platform Management コマンドレット（「Pm」で始まる）は、組織間のディレクトリ情報を提供します
- エンティティ名を識別子を含む完全なディレクトリ表現に変換する必要がある場合は、このコマンドレットを使用してください

## RELATED LINKS

[Search-PmDirectory]()

[Get-PmUser]()

[Get-PmGroup]()
