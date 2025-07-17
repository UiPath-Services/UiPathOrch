---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchApiTrigger

## SYNOPSIS
API トリガーを取得します。

## SYNTAX

```
Get-OrchApiTrigger [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
API トリガーは、特定のフォルダー内に存在するフォルダーエンティティです。最初に Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダーに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダーを指定してください。

ターゲットフォルダー内の指定された名前の API トリガーに関する情報を出力します。ターゲットフォルダーは、-Path、-Recurse、-Depth パラメーターを使用して指定できます。これらが指定されていない場合、現在の場所がターゲットフォルダーとして使用されます。API トリガー名が指定されていない場合、ターゲットフォルダー内のすべての API トリガーを出力します。

-Path および -Name パラメーターの複数の値は、ワイルドカードを含むコンマ区切りのテキストを使用して指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値のオートコンプリートを使用できます。

-Path、-Recurse、-Depth パラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターのオートコンプリートが正しく機能することが保証されます。

主要エンドポイント: GET /odata/HttpTriggers

OAuth 必須スコープ: OR.Triggers.Read

必要なアクセス許可: Folders.View（フォルダーにアクセスするため）、Triggers.View（API トリガー情報を取得するため）

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchApiTrigger
```

現在の場所である 'Shared' フォルダー内のすべての API トリガーを表示します。

### Example 2
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse
```

現在のフォルダーとそのすべてのサブフォルダー内のすべての API トリガーを表示します。ルートフォルダーで実行すると、そのテナント内のすべてのフォルダーにわたるすべての API トリガーが表示されます。

### Example 3
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse *api*
```

現在のフォルダーとそのすべてのサブフォルダーから、名前に 'api' を含む API トリガーを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchApiTrigger -Path Orch1:\Production TestTrigger
```

Production フォルダーから 'TestTrigger' という名前の API トリガーを取得します。

### Example 5
```powershell
PS C:\> Get-OrchApiTrigger -Path Orch1:\,Orch2:\ -Recurse
```

Orch1: および Orch2: 内のすべての API トリガーを表示します。

### Example 6
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse | ConvertTo-Json -Depth 2
```

すべての API トリガーを取得し、Release や MachineRobots などのネストされたプロパティを含む完全な構造を表示します。

### Example 7
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | Export-Csv c:apiTriggers.csv
```

出力を CSV ファイルにエクスポートします。CSV ファイルは C: ドライブの現在の場所に配置されます。含める列を指定するために `select` と組み合わせて CSV 形式をカスタマイズできます。`ii c:` を試して C: ドライブの現在の場所を開いてください。

### Example 8
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | ConvertTo-Json
```

出力を JSON 形式に変換し、Orchestrator からのデータの生のビューを提供します。

### Example 9
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse -Name *test*,*prod*
```

すべてのフォルダーから再帰的に、名前に 'test' または 'prod' を含む API トリガーを取得します。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深度を指定します。深度 0 は現在の場所のみを示し、サブフォルダーは含まれません。

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
取得する API トリガーの名前を指定します。

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
ターゲットフォルダーを指定します。指定されていない場合、現在のフォルダーがターゲットになります。

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
{{ ProgressAction の説明を入力 }}

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
操作にターゲットフォルダーとそのすべてのサブフォルダーを含める必要があることを指定します。

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

### CommonParameters
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.HttpTrigger
## NOTES
呼び出されるメインエンドポイント: GET /odata/HttpTriggers

必要なスコープ: OR.Folders.Read

主要エンドポイント: GET /odata/HttpTriggers
OAuth 必須スコープ: OR.Triggers.Read
必要なアクセス許可: Folders.View、Triggers.View

## RELATED LINKS
