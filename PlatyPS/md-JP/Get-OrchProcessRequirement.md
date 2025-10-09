---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchProcessRequirement

## SYNOPSIS
プロセスの依存リソース（Requirements）を取得します。

## SYNTAX

```
Get-OrchProcessRequirement [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

指定されたフォルダ内のプロセスが依存している外部リソース（プロセス、アセット、キュー、トリガー、タスクカタログ、接続など）の情報を取得します。
各依存リソースの検証状態（Success、NotFound、Unknown）も確認できます。

プライマリエンドポイント: GET /odata/Releases({release.Id})/UiPath.Server.Configuration.OData.GetResources(processKey='{release.ProcessKey}:{release.ProcessVersion}')

必要なOAuthスコープ:

必要な権限:

## EXAMPLES

### Example 1: すべての依存リソースを取得
```powershell
PS Orch1:\> Get-OrchProcessRequirement -Recurse
```

現在のフォルダとそのサブフォルダ内のすべてのプロセスの依存リソースを取得します。

### Example 2: プロセス名でフィルタリングして出力
```powershell
PS Orch1:\> Get-OrchProcessRequirement -Path Shared *Queue*
```

'Shared' フォルダ内で、ワイルドカードパターン '*Queue*' に一致するプロセスの依存リソースを取得します。

### Example 3: 欠落している依存関係を検索
```powershell
PS Orch1:\> Get-OrchProcessRequirement -Recurse | Where-Object ValidationResult -eq 'NotFound'
```

すべてのプロセスで欠落している依存リソースを特定します。デプロイメント問題のトラブルシューティングに有用です。

## PARAMETERS

### -Depth
検索する階層の深さを指定します。0は現在のフォルダのみ、1は1階層下まで検索します。

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

### -Name
プロセス名を指定します。ワイルドカード文字（*、?）がサポートされています。配列として複数の名前を指定することもできます。

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
検索対象のフォルダパスを指定します。ワイルドカード文字がサポートされています。UiPathOrch プロバイダのパス（例: Orch1:\FolderName）を指定します。

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
指定されたフォルダとそのすべてのサブフォルダを再帰的に検索します。このパラメータを指定しない場合、現在のフォルダのみが検索されます。

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

### -ProgressAction
PowerShell の進行状況表示の動作を制御します。

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
このコマンドレットは、共通パラメーター（-Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、-WarningVariable）をサポートしています。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216) を参照してください。

## INPUTS

### System.String[]

## OUTPUTS

### UiPath.PowerShell.Entities.SubtypedPackageResource

## NOTES

このコマンドレットは、プロセスが依存している以下の種類のリソースを取得できます：
- Process: 他のプロセス
- Asset: アセット
- Queue: キュー
- TaskCatalog: タスクカタログ
- TimeTrigger: 時間トリガー
- Connection: 接続（外部サービスとの統合）

各リソースには ValidationResult プロパティがあり、以下のいずれかの値を持ちます：
- Success: リソースが正常に検証され、フォルダ内に存在します
- NotFound: リソースが見つかりませんでした
- Unknown: リソースの検証状態が不明です

-Path、-Recurse、または -Depth パラメータのいずれかを指定する必要があります。
いずれも指定しない場合は、Set-Location コマンドレットで対象フォルダに移動してから実行してください。

## RELATED LINKS
