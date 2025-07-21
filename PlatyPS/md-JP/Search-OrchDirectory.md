---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Search-OrchDirectory

## SYNOPSIS
ディレクトリを検索します。

## SYNTAX

```
Search-OrchDirectory [-Name] <String> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
`Search-OrchDirectory` コマンドレットは、UiPath Orchestrator のディレクトリサービス内のディレクトリユーザーとグループを検索します。このコマンドレットは、Orchestrator のディレクトリコンテキスト内で指定された検索用語で始まる名前を検索することにより、ユーザーとグループを見つける方法を提供します。

検索結果には、ユーザーやロボットアカウントなどのディレクトリオブジェクトと、それらの識別子、表示名、ドメイン、タイプ情報が含まれます。このコマンドレットは、Platform Management ディレクトリ検索と比較して異なる結果を含む可能性がある Orchestrator のディレクトリサービススコープ内で特に検索を行います。

検索は"で始まる"パターンを使用して実行され、ドメインコンテキスト（通常、ローカルユーザーの場合は"autogen"）を含むため、Orchestrator の認証およびユーザー管理コンテキスト内でディレクトリオブジェクトを見つけるのに適しています。

このコマンドレットは、ユーザー発見、管理タスク、および Orchestrator 環境内でディレクトリオブジェクトを見つけて識別する必要がある統合シナリオに役立ちます。

プライマリ エンドポイント: GET /api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All

OAuth 必要なスコープ: OR.Users または OR.Users.Read

必要な権限: Users.View または Units.Edit または SubFolders.Edit

OAuth 必要なスコープ: OR.Users.Read

必要な権限: (Users.View または Units.Edit または SubFolders.Edit)

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
{{ Fill Name Description }}

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
対象ドライブの名前を指定します。指定されていない場合は、現在のドライブが対象になります。

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

### UiPath.PowerShell.Entities.DirectoryObject
## NOTES

## RELATED LINKS
