---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLog

## SYNOPSIS
フィルタリング機能を使用してUiPath Orchestratorから実行ログを取得します。

## SYNTAX

```
Get-OrchLog [-Last <String>] [-TimeStampAfter <DateTime>] [-TimeStampBefore <DateTime>] [[-Level] <String>]
 [-Machine <String>] [-ProcessName <String>] [-WindowsIdentity <String[]>] [-Skip <UInt64>] [-First <UInt64>]
 [-JobKey <String>] [-OrderBy <String>] [-OrderAscending] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchLogコマンドレットは、包括的なフィルタリング機能を使用してUiPath Orchestratorから実行ログを取得します。ログは、オートメーション実行、エラー、およびシステムアクティビティに関する詳細な情報を提供します。このコマンドレットは、過度なデータ取得を防ぐために少なくとも1つのフィルターパラメーターが必要です。

ログには、Level（重要度）、TimeStamp、Machine、WindowsIdentity、ProcessName、JobKey、および詳細なメッセージ内容などの情報が含まれています。このコマンドレットは、時間範囲、ログレベル、マシン、プロセス、およびジョブキーを含むさまざまなフィルタリングオプションをサポートしています。

このコマンドレットはフォルダーエンティティ操作として動作し、適切なフォルダーコンテキストへの移動または-Pathパラメーターを使用したターゲットフォルダーの指定が必要です。大きな結果セットを効率的に管理するには、ページネーションパラメーター（Skip、First）を使用してください。

**重要**: このコマンドレットは実行するために少なくとも1つのフィルターパラメーター（TimeStampAfter、Last、Level、Machine、ProcessName、またはJobKeyなど）が必要です。フィルターなしでは、警告とともにキャッシュされた内容を出力します。

プライマリエンドポイント: GET /odata/Logs

OAuth必須スコープ: OR.Monitoring または OR.Monitoring.Read

必要な権限: Logs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day -First 10
```

過去1日から最初の10件のログを取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchLog -TimeStampAfter (Get-Date).AddHours(-2) -Level Error, Fatal
```

過去2時間からすべてのエラーと致命的ログを取得します。

### Example 3
```powershell
PS C:\> Get-OrchLog -Path Orch1:\Production -ProcessName InvoiceProcess -First 20
```

ProductionフォルダーのInvoiceProcessの最初の20件のログを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchLog -Recurse -Last Week -Machine Robot01 -Level Warn, Error
```

そのテナント内のすべてのフォルダーにわたって、Robot01からの過去1週間の警告とエラーログを取得します。

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchLog -JobKey 12345678-1234-1234-1234-123456789012 -OrderBy TimeStamp -OrderAscending
```

特定のジョブキーのログを取得し、タイムスタンプの昇順で並べ替えます。

### Example 6
```powershell
PS C:\> Get-OrchLog -Path Orch1:\Shared -Last Month -WindowsIdentity DOMAIN\robotuser -Skip 100 -First 50
```

過去1か月間の特定のWindows IDのログを取得し、最初の100件の結果をスキップして次の50件を取得します。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深さを指定します。深さ0は現在の場所のみを示します。より高い値はより多くのサブフォルダーレベルを含みます。

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
フィルターするログレベルを指定します。一般的な値には、Trace、Debug、Info、Warn、Error、Fatalがあります。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
検索するターゲットフォルダーを指定します。指定されていない場合、現在のフォルダーコンテキストが使用されます。パス指定が必要なフォルダーエンティティ操作用。

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

### -Recurse
検索操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。包括的なログ発見に不可欠です。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeStampAfter
ログをフィルタリングするための最も早いタイムスタンプを指定します。この時刻以降のログのみが返されます。

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TimeStampBefore
ログをフィルタリングするための最も遅いタイムスタンプを指定します。この時刻以前のログのみが返されます。

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
結果セットの開始からスキップするログエントリの数を指定します。ページネーションに役立ちます。

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

### -First
結果セットの開始から返すログエントリの最大数を指定します。

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

### -Last
最近のログの期間を指定します。有効な値：Hour、Day、Week、Month、3Months、6Months、Year、3Years。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Machine
ログをフィルタリングするマシン名を指定します。特定のロボットまたはマシンからログを取得するために使用します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WindowsIdentity
ログをフィルタリングするWindows IDを指定します。特定のユーザーアカウントのログを取得するために使用します。

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

### -ProcessName
ログをフィルタリングするプロセス名を指定します。特定のオートメーションプロセスのログを取得するために使用します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OrderAscending
結果を昇順（true）または降順（false）で並べ替えるかどうかを指定します。

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

### -OrderBy
結果を並べ替えるフィールドを指定します（例：TimeStamp、Level、Machine）。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -JobKey
ログをフィルタリングするジョブキーを指定します。特定のオートメーション実行のログを取得するために使用します。

```yaml
Type: String
Parameter Sets: (All)
Aliases: Key

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
このコマンドレットは共通パラメーターをサポートします: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Log
## NOTES
このコマンドレットは、過度なデータ取得を防ぐために少なくとも1つのフィルターパラメーターが必要なフォルダーエンティティ操作です。フィルターパラメーターが指定されていない場合、コマンドレットは警告とともにキャッシュされた内容を出力します。大きな結果セットを管理するには、ページネーションパラメーター（Skip、First）を使用してください。一般的なフィルターパターンには、時間範囲（Last、TimeStampAfter）、重要度レベル（Level）、および特定の実行コンテキスト（Machine、ProcessName、JobKey）があります。この操作には、ターゲットフォルダーでのLogs.View権限が必要です。

プライマリエンドポイント: GET /odata/RobotLogs
OAuth必須スコープ: OR.Monitoring または OR.Monitoring.Read
必要な権限: Logs.View

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchAuditLog](Get-OrchAuditLog.md)

[Clear-OrchLog](Clear-OrchLog.md)

[Export-OrchLog](Export-OrchLog.md)
