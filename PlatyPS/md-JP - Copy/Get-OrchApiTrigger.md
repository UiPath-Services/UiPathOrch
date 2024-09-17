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
指定した名前の API トリガーを、ターゲットフォルダーから取得します。ターゲットフォルダーは、-Path、-Recurse、-Depth パラメーターで指定します。これらを指定しない場合は、現在のフォルダーをターゲットとします。API トリガーの名前を指定しない場合は、ターゲットフォルダーに含まれる API トリガーをすべて出力します。

-Path と -Name パラメータには、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。また、これらの値は [Ctrl+Space] もしくは [Tab] を押下することで自動補完入力できます。

-Path、-Recurse、-Depth パラメータを指定するときは、これらをコマンドレット名の直後に指定してください。これにより、後続のパラメータの自動補完が適切に動作するようになります。

主に呼び出すエンドポイント: GET /odata/HttpTriggers

必要なスコープ: (undocumented)

必要な権限:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchApiTrigger
```

現在のフォルダーである 'Shared' フォルダーに含まれる API トリガーをすべて表示します。

### Example 2
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse
```

現在のフォルダーと、そのすべてのサブフォルダーに含まれる API トリガーをすべて表示します。ルートフォルダーで実行すると、そのテナントのすべてのフォルダーに含まれる API トリガーをすべて表示します。

### Example 3
```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse <API trigger names>
```

現在のフォルダーと、そのすべてのサブフォルダーに含まれる API トリガーのうち、指定の名前をもつものを表示します。API トリガー名には、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。API トリガー名は、[Ctrl+Space] もしくは [Ctrol+Tab] を押下して自動補完できます。

### Example 4
```powershell
PS Orch1:\> Get-OrchApiTrigger -Path <folder names> <API trigger names>
```

指定のフォルダーに含まれる API トリガーのうち、指定の名前をもつものを表示します。フォルダー名には、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。フォルダー名は、[Ctrl+Space] もしくは [Ctrol+Tab] を押下して自動補完できます。

### Example 5
```powershell
PS C:\> Get-OrchApiTrigger -Recurse -Path Orch1:\,Orch2:\
```

Orch1: と Orch2: に含まれる API トリガーをすべて表示します。

### Example 6
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | select Path,Id,Name
```

指定された列のみを出力します。列名には、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。列名は、[Ctrl+Space] もしくは [Ctrol+Tab] を押下して自動補完できます。

### Example 7
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | Export-Csv c:apiTriggers.csv
```

出力を CSV ファイルにエキスポートします。CSV ファイルは、C: ドライブの現在のフォルダーに出力されます。`select` と組み合わせると、CSV の書式を自由にカスタマイズできます。C: ドライブの現在のフォルダーを開くには、`ii c:' と入力します。

### Example 8
```powershell
PS C:\> Get-OrchApiTrigger -Recurse | ConvertTo-Json
```

出力を JSON 形式に変換します。Orchestrator が出力した生の結果を確認できます。

## PARAMETERS

### -Depth
ターゲットフォルダーへの再帰の深さを指定します。深さが0の場合は、現在のフォルダーのみが対象となり、サブフォルダーは含まれません。

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
Accept pipeline input: False
Accept wildcard characters: True
```

### -Path
ターゲットとするフォルダーを指定します。指定しない場合は、現在のフォルダーをターゲットとします。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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
ターゲットフォルダーのサブフォルダーも、ターゲットとして含めることを指定します。

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.HttpTrigger
## NOTES

## RELATED LINKS
