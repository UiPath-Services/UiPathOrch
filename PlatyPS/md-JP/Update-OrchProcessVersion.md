---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: uiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchProcessVersion

## SYNOPSIS
プロセスのバージョンを更新します。

## SYNTAX

### ReleaseName (Default)
```
Update-OrchProcessVersion [[-Name] <String[]>] [[-Version] <String>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ReleaseId
```
Update-OrchProcessVersion [-Id <Int64[]>] [[-Version] <String>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
指定したプロセスが参照するプロセスパッケージを、指定のバージョンに更新します。バージョンを指定しない場合には、最新バージョンに更新します。

-Path と -Id パラメータには、ワイルドカードを含むテキストをカンマ区切りで複数指定できます。また、これらの値は [Ctrl+Space] もしくは [Tab] を押下することで自動補完入力できます。

-Path、-Recurse、-Depth パラメータを指定するときは、これらをコマンドレット名の直後に指定してください。これにより、後続のパラメータの自動補完が適切に動作するようになります。

主に呼び出すエンドポイント: POST /odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersion?mergePackageTags=false

OAuth に必要なスコープ: OR.Execution

必要な権限: Processes.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\ -Recurse * -WhatIf
```

Orch1: テナントにある、最新になっていないプロセスのバージョンをすべて最新にします。問題ないことを確認したら、-WhatIf を外して再実行してください。

### Example 2
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess
```

Orch1:\Shared フォルダにある MyProcess プロセスのパッケージバージョンを最新にします。

### Example 3
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess 1.0.3
```

Orch1:\Shared フォルダにある MyProcess プロセスのパッケージバージョンを 1.0.3 にします。

### Example 4
```powershell
PS Orch1:\Shared> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess
```

カレントフォルダの MyProcess プロセスのパッケージバージョンを最新にします。

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
バージョンを更新するプロセスの Name を指定します。

```yaml
Type: String[]
Parameter Sets: ReleaseName
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
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
Accept pipeline input: True (ByPropertyName)
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

### -Version
プロセスをどのバージョンに更新するかを指定します。省略すると、最新バージョンに更新されます。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Confirm
コマンドレットを実行する前に、あなたの確認を求めます。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
コマンドレットを実行すると、何が起こるかを表示します。
コマンドレットは実行されません。

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
{{ Fill Id Description }}

```yaml
Type: Int64[]
Parameter Sets: ReleaseId
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
