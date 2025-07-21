---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmGroupMember

## SYNOPSIS
UiPath Platform Managementからグループメンバーを取得します。

## SYNTAX

```
Get-PmGroupMember [[-GroupName] <String[]>] [-Path <String[]>] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmGroupMemberコマンドレットは、UiPath Platform Managementからグループメンバーシップ情報を取得します。このコマンドレットは、指定されたグループのメンバーであるすべてのユーザー、ロボットアカウント、およびその他のディレクトリエンティティに関する詳細を返します。グループ名でのフィルタリングをサポートし、一括操作のために結果をCSV形式でエクスポートできます。

-Pathパラメータは、対象ドライブを指定します。指定されていない場合、現在のドライブが対象となります。

このコマンドレットは、Platform Management APIにアクセスし、すべてのUiPath Orchestratorドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

プライマリ エンドポイント: GET /api/Group/{tenantId}/{groupId}

OAuth 必要スコープ: PM.Group または PM.Group.Read

必要な権限: [PLACEHOLDER - Platform Management group read permissions]

## EXAMPLES

### Example 1: すべてのグループメンバーを取得
```powershell
PS Orch1:\> Get-PmGroupMember
```

現在のPlatform Managementインスタンス内のすべてのグループからメンバーを取得します。

### Example 2: 特定のグループのメンバーを取得
```powershell
PS Orch1:\> Get-PmGroupMember Admin*, Everyone
```

名前が"Admin"で始まるグループまたは"Everyone"と正確に一致するグループからメンバーを取得します。

### Example 3: グループメンバーの詳細を取得して構造を調べる
```powershell
PS Orch1:\> Get-PmGroupMember Administrators | Select-Object -First 1 | ConvertTo-Json -Depth 5
```

最初のグループメンバーを取得し、詳細な分析のために完全なオブジェクト構造をJSON形式で表示します。

### Example 4: グループメンバーをCSVにエクスポート
```powershell
PS Orch1:\> Get-PmGroupMember Administrators -ExportCsv C:\temp\AdminMembers.csv
```

Administratorsグループのすべてのメンバーを、一括操作またはレポート用にCSVファイルにエクスポートします。エクスポートされたCSVは、Import-Csv | Add-PmGroupMemberを使用してインポートできます。

### Example 5: 特定のドライブからグループメンバーを取得
```powershell
PS C:\> Get-PmGroupMember -Path Orch1:, Orch2: Administrators
```

指定された複数のOrchestratorドライブからグループメンバーシップ情報を取得します。

## PARAMETERS

### -CsvEncoding
-ExportCsvを使用する際の、エクスポートされるCSVファイルのエンコーディングを指定します。このパラメータは、出力ファイルの文字エンコーディングを制御します。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
グループメンバーデータをCSVとしてエクスポートするファイルパスを指定します。エクスポートされたCSVには、Add-PmGroupMemberとの一括インポート操作に適した列が含まれています。これは、グループメンバーシップ操作に適した適切な形式が含まれているため、Select-Object + Export-Csvを使用するよりも信頼性があります。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupName
メンバーを取得するグループの名前を指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。指定されていない場合、すべてのグループからメンバーを返します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
対象ドライブの名前を指定します。指定されていない場合、現在のドライブが対象となります。Platform Management APIは、すべてのOrchestratorドライブで動作します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.DirectoryUser
### UiPath.PowerShell.Entities.DirectoryGroup
### UiPath.PowerShell.Entities.DirectoryRobotUser
### UiPath.PowerShell.Entities.DirectoryApplication
## NOTES
- グループメンバーには、ユーザー、ロボットアカウント、ネストされたグループ、およびアプリケーションが含まれる場合があります
- -ExportCsvパラメータは、一括グループメンバーシップ操作用のインポート準備ができたCSVファイルを作成します
- メンバータイプは、objectTypeプロパティ（DirectoryUser、DirectoryRobotUser、DirectoryGroup、DirectoryApplication）によって示されます
- フィルタリング操作を使用して、グループメンバーシップパターンを分析し、セキュリティコンプライアンス問題を特定します
- Platform Managementコマンドレット（"Pm"で始まる）は、組織横断的なグループメンバーシップ情報を提供します
- sourceプロパティは、ディレクトリソース（例："local"、Azure Active Directoryの場合は"aad"）を示します

## RELATED LINKS

[Add-PmGroupMember]()

[Remove-PmGroupMember]()

[Get-PmGroup]()

[Move-PmGroupMember]()
