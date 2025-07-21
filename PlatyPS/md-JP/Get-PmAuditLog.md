---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmAuditLog

## SYNOPSIS
Platform Managementから監査ログエントリを取得します。

## SYNTAX

```
Get-PmAuditLog [-Skip <UInt64>] [-First <UInt64>] [-OrderAscending] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-PmAuditLogコマンドレットは、UiPath Platform Managementから監査ログエントリを取得します。このコマンドレットは組織レベルで動作し、組織全体の管理活動、ユーザーアクション、およびシステムイベントを追跡します。

これは、Platform Management APIを呼び出す組織エンティティコマンドレットです。複数のテナントが同じ組織に属することができる組織レベルで動作します。-Pathパラメータは、ドライブ名（例：Orch1:、Orch2:）を使用して対象テナントを指定しますが、監査データは組織全体のアクティビティを反映します。

Platform Management監査ログは、ユーザー管理活動、ロール割り当て、ライセンス変更、および組織内のすべてのテナントで実行される管理アクションの包括的な追跡を提供します。これは、コンプライアンス、セキュリティ監査、および管理監督に不可欠です。

プライマリ エンドポイント: GET /api/auditLog/{partitionGlobalId}

OAuth 必要スコープ: OR.Administration または OR.Administration.Read

必要な権限: Administration.View

## EXAMPLES

### Example 1
```powershell
Get-PmAuditLog
```

現在の組織から監査ログエントリを取得します。

### Example 2
```powershell
Get-PmAuditLog -First 100
```

最初の100個の監査ログエントリを取得します（デフォルトで最新順）。

### Example 3
```powershell
Get-PmAuditLog -OrderAscending -First 50
```

昇順（古い順）で最初の50個の監査ログエントリを取得します。

### Example 4
```powershell
Get-PmAuditLog -Path Orch1:, Orch2: -First 200
```

複数のテナントドライブ経由でアクセスして、組織から監査ログエントリを取得します。

### Example 5
```powershell
Get-PmAuditLog -Skip 100 -First 50
```

最初の100個のエントリをスキップして、次の50個の監査ログエントリを取得します（ページネーション）。

### Example 6
```powershell
Get-PmAuditLog | Where-Object {$_.EventType -eq "UserCreated"}
```

ユーザー作成イベントのすべての監査ログエントリを取得します。

### Example 7
```powershell
Get-PmAuditLog -First 1000 | Select-Object Timestamp, UserName, EventType, Details | Export-Csv "AuditLog.csv"
```

最初の1000個の監査ログエントリを取得し、分析用に主要な情報をCSVファイルにエクスポートします。

## PARAMETERS

### -First
指定された数のオブジェクトのみを取得します。
取得するオブジェクトの数を入力してください。

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OrderAscending
createdOnのソート順序を指定します。デフォルトでは、エントリは降順（新しい順）で返されます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
対象テナントドライブの名前を指定します。監査ログデータは、使用するテナントドライブに関係なく組織全体のものです。

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

### -Skip
指定された数のオブジェクトを無視してから、残りのオブジェクトを取得します。
スキップするオブジェクトの数を入力してください。

```yaml
Type: UInt64
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

### System.Nullable`1[[System.UInt64, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
### System.Management.Automation.SwitchParameter
### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmAuditLog
## NOTES

## RELATED LINKS
