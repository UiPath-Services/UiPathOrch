---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAsset

## SYNOPSIS
UiPath Orchestrator からアセットを取得します。

## SYNTAX

```
Get-OrchAsset [[-Name] <String[]>] [-ValueType <String[]>] [-ExpandUserValues] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ExportCsv <String>] [-ExportCredentialCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
UiPath Orchestrator フォルダーからアセット情報を取得します。アセットは、ロボットが設定、認証情報、その他の自動化データを格納するために使用するデータストアです。

このコマンドレットは、アセット名と値の型によるフィルタリング、ロボットごとのアセットのユーザー固有値の展開、結果の CSV ファイルへのエクスポートをサポートします。特定のフォルダーで動作するか、フォルダー階層全体で再帰的に動作できます。

-Name および -Path パラメーターの複数の値は、ワイルドカードを含むコンマ区切りのテキストを使用して指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値のオートコンプリートを使用できます。

-Path、-Recurse、-Depth パラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターのオートコンプリートが正しく機能することが保証されます。

プライマリエンドポイント: GET /odata/Assets

OAuth 必須スコープ: OR.Assets または OR.Assets.Read

必要なアクセス許可: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchAsset
```

現在のフォルダーからアセットを取得します。

### Example 2
```powershell
PS Orch1:\> Get-OrchAsset -Recurse
```

すべてのフォルダーから再帰的にアセットを取得します。

### Example 3
```powershell
PS Orch1:\> Get-OrchAsset -Recurse *Config*
```

すべてのフォルダーから名前に "Config" を含むアセットを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchAsset -ValueType Credential -Recurse
```

すべてのフォルダーから認証情報アセットを取得します。

### Example 5
```powershell
PS Orch1:\> Get-OrchAsset -ExpandUserValues -Recurse
```

ロボットごとのアセットに対してユーザー固有値を展開したアセットを取得します。

### Example 6
```powershell
PS Orch1:\> Get-OrchAsset -Recurse | Where-Object {$_.ValueScope -eq "PerRobot"}
```

すべてのフォルダーからロボットごとのスコープのアセットを取得します。

### Example 7
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExportCsv C:\Reports\Assets.csv
```

すべてのアセットを UTF-8 BOM エンコーディングで CSV にエクスポートします。エクスポートされた CSV は、Import-Csv | Set-OrchAsset を使用してインポートできます。

### Example 8
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExportCredentialCsv C:\Reports\Credentials.csv
```

認証情報アセットを別の CSV ファイルにエクスポートします。エクスポートされた CSV は、Import-Csv | Set-OrchCredentialAsset を使用してインポートできます。

## PARAMETERS

### -CsvEncoding
CSV エクスポートのエンコーディングを指定します。デフォルトは Excel 互換性のための BOM 付き UTF-8 です。

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
フォルダー再帰の深度を指定します。深度 0 は現在のフォルダーのみをターゲットとします。

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

### -ExportCredentialCsv
認証情報アセットを BOM 付き UTF-8 エンコーディングで CSV ファイルにエクスポートします。インポート用に Set-OrchCredentialAsset と併用できます。

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

### -ExportCsv
結果を BOM 付き UTF-8 エンコーディングで CSV ファイルにエクスポートします。内部 ID を人間が読める名前に自動変換します。インポート用に Set-OrchAsset と併用できます。

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

### -Name
取得するアセットの名前を指定します。ワイルドカードと複数の値をサポートします。

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
ターゲットフォルダーを指定します。複数のフォルダーにはコンマ区切りの値を使用します。ワイルドカードをサポートします。指定されていない場合、現在のフォルダーをターゲットとします。

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

### -ProgressAction
コマンドレット実行中の進行状況情報の表示方法を制御します。

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
操作にターゲットフォルダーとそのすべてのサブフォルダーを含めます。

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

### -ValueType
取得するアセットの値の型を指定します。有効な値: Text、Bool、Integer、Credential。

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

### -ExpandUserValues
ロボットごとのアセットのユーザー固有値を展開し、各ユーザー/マシンの組み合わせに割り当てられた実際の値を表示します。

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Asset
## NOTES
アセットエンティティはフォルダースコープです。フォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定する必要があります。

アセットは、Global（すべてのユーザーに同じ値）または PerRobot（ユーザー/マシンごとに異なる値）の ValueScope を持つことができます。PerRobot アセットの実際の割り当て値を表示するには、-ExpandUserValues を使用してください。

-ExportCsv および -ExportCredentialCsv パラメーターは、内部 ID の代わりに人間が読める名前を持つインポート準備済み CSV ファイルを作成します。

プライマリエンドポイント: GET /odata/Assets
OAuth 必須スコープ: OR.Assets または OR.Assets.Read
必要なアクセス許可: Assets.View

## RELATED LINKS

[Set-OrchAsset](Set-OrchAsset.md)

[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)

[Add-OrchAssetLink](Add-OrchAssetLink.md)

[Remove-OrchAsset](Remove-OrchAsset.md)
