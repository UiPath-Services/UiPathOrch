---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchTestSet

## SYNOPSIS
テストセットを取得します。

## SYNTAX

```
Get-OrchTestSet [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Get-OrchTestSet コマンドレットは、UiPath Orchestratorからテストセットを取得します。テストセットは、自動化プロセスを検証するために一緒に実行できるテストケースのコレクションです。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、最初にSet-Location（cd）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメータを使用してターゲットフォルダを指定する必要があります。

パラメータが指定されていない場合、現在のフォルダ内のすべてのテストセットが返されます。

主要エンドポイント: GET /odata/TestSets?$filter=(SourceType eq 'User')&$expand=Environment

OAuth必須スコープ: OR.TestSets または OR.TestSets.Read

必要な権限: TestSets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchTestSet
```

現在のフォルダ内のすべてのテストセットを取得します。

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchTestSet RegressionTests
```

位置パラメータを使用して、現在のフォルダから"RegressionTests"という名前のテストセットを取得します。

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchTestSet *Smoke*
```

ワイルドカードパターンマッチングを使用して、名前に"Smoke"を含むすべてのテストセットを取得します。

### Example 4
```powershell
PS Orch1:\> Get-OrchTestSet -Recurse
```

現在のフォルダとそのすべてのサブフォルダから再帰的にすべてのテストセットを取得します。

### Example 5
```powershell
PS C:\> Get-OrchTestSet -Path Orch1:\Production, Orch1:\Shared UITests
```

任意の場所からの実行を示して、ProductionフォルダとSharedフォルダの両方から"UITests"テストセットを取得します。

## PARAMETERS

### -Depth
再帰操作の最大深度レベルを指定します。深度0は現在の場所のみを示します。高い値はより多くのサブフォルダレベルを含みます。

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
取得するテストセットの名前を指定します。ワイルドカード文字（*および?）をサポートし、パターンマッチングに使用できます。

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
テストセットを検索するターゲットフォルダを指定します。指定されていない場合は、現在のフォルダがターゲットになります。ワイルドカードをサポートします。

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
このコマンドレットによって生成される進行状況の更新にPowerShellがどのように応答するかを決定します。デフォルト値はContinueです。

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
ターゲットフォルダとそのすべてのサブフォルダを操作に含めることを指定します。

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

### UiPath.PowerShell.Entities.TestSet
## NOTES

主要エンドポイント: GET /odata/TestSets
OAuth必須スコープ: OR.TestSets または OR.TestSets.Read
必要な権限: TestSets.View

## RELATED LINKS
