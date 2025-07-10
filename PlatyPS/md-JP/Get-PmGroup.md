---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmGroup

## SYNOPSIS
UiPath Platform Managementからグループを取得します。

## SYNTAX

```
Get-PmGroup [[-GroupName] <String[]>] [-Path <String[]>] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmGroupコマンドレットは、UiPath Platform Managementからグループ情報を取得します。グループは、ロールと権限を集約して割り当てることができるユーザーのコレクションです。このコマンドレットは、グループメンバーシップ、作成時刻、ライセンス割り当て、およびその他のグループ関連メタデータに関する詳細を提供します。

-Pathパラメータは、対象ドライブを指定します。指定されていない場合、現在のドライブが対象となります。

このコマンドレットは、Platform Management APIにアクセスし、すべてのUiPath Orchestratorドライブ（Orch1:、Orch1Tm:、Orch1Du:）で動作します。

プライマリ エンドポイント: GET /api/Group/{tenantId}

OAuth 必要スコープ: PM.Group または PM.Group.Read

必要な権限: [PLACEHOLDER - Platform Management group read permissions]

## EXAMPLES

### Example 1: すべてのグループを取得
```powershell
PS Orch1:\> Get-PmGroup
```

現在のPlatform Managementインスタンスからすべてのグループを取得します。

### Example 2: 特定のドライブからグループを取得
```powershell
PS C:\> Get-PmGroup -Path Orch1:, Orch2:
```

指定された複数のOrchestratorドライブからグループ情報を取得します。

### Example 3: 名前で複数のグループを取得
```powershell
PS Orch1:\> Get-PmGroup Administrators, "Automation Users", *Developers*
```

指定された複数のグループの情報を取得します。

### Example 4: グループの詳細を取得して構造を調べる
```powershell
PS Orch1:\> Get-PmGroup | Select-Object -First 1 | ConvertTo-Json -Depth 5
```

最初のグループを取得し、詳細な分析のために完全なオブジェクト構造をJSON形式で表示します。

### Example 5: インポート用にグループをCSVにエクスポート
```powershell
PS Orch1:\> Get-PmGroup -ExportCsv C:\temp\groups.csv
```

すべてのグループを、Import-CsvおよびNew-PmGroupとの一括グループ作成に使用できるCSVファイルにエクスポートします。

### Example 6: CSVからグループをインポート
```powershell
PS C:\> Import-Csv C:\temp\groups.csv | New-PmGroup -WhatIf
```

Get-PmGroup -ExportCsvでエクスポートされたCSVファイルからグループ定義をインポートし、新しいグループを作成します。

### Example 7: 作成日でグループをフィルタリング
```powershell
PS Orch1:\> Get-PmGroup | Where-Object {$_.creationTime -gt (Get-Date).AddDays(-30)} | Select-Object displayName, creationTime
```

すべてのグループを取得し、過去30日間に作成されたものをフィルタリングして、名前と作成時刻を表示します。

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
グループデータをCSVとしてエクスポートするファイルパスを指定します。エクスポートされたCSVには、New-PmGroupのパラメータ名と一致する列が含まれており、一括インポート操作に適しています。これは、適切なIDから名前への変換が含まれているため、Select-Object + Export-Csvを使用するよりも信頼性があります。

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
取得するグループの名前を指定します。パターンマッチングのためのワイルドカード文字（*および?）をサポートします。複数のグループ名を配列として指定できます。

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
このコマンドレットは、-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariableの共通パラメータをサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmGroup
## NOTES
- このコマンドレットは、Platform Management APIにアクセスし、すべてのUiPathドライブタイプで動作します
- -ExportCsvパラメータは、New-PmGroupパラメータと一致する適切な列名を持つインポート準備ができたCSVファイルを作成します
- 一括操作の場合、-ExportCsvに続けてImport-Csv | New-PmGroupを使用することは、Select-Object + Export-Csvよりも信頼性があります
- フォルダやテナント間でのグループのコピーには、より良い結果を得るためにCSVエクスポート/インポートではなくCopy-PmGroupの使用を検討してください
- members配列には、グループメンバーシップに関する詳細情報が含まれています
- Platform Managementコマンドレット（「Pm」で始まる）は、組織横断的なグループ情報を提供します
- グループタイプとmappingRoleプロパティは、グループの目的とロール割り当てに関する情報を提供します

## RELATED LINKS

[New-PmGroup]()

[Copy-PmGroup]()

[Remove-PmGroup]()

[Add-PmGroupMember]()

[Remove-PmGroupMember]()
