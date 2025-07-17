---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
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
指定されたプロセスによって参照されるプロセスパッケージを指定されたバージョンに更新します。バージョンが指定されていない場合、最新バージョンに更新されます。

-Path および -Id パラメーターには、ワイルドカードを含むカンマ区切りのテキストを使用して複数の値を指定できます。さらに、[Ctrl+Space] または [Tab] を押すことで、これらの値のオートコンプリートを使用できます。

-Path、-Recurse、-Depth パラメーターを指定する場合は、コマンドレット名の直後に配置してください。この配置により、後続のパラメーターのオートコンプリートが正しく機能します。

プライマリ エンドポイント: POST /odata/Releases({processId})/UiPath.Server.Configuration.OData.UpdateToLatestPackageVersion?mergePackageTags=false

OAuth 必要なスコープ: OR.Execution

必要な権限: Processes.Edit

## EXAMPLES

### Example 1
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\ -Recurse * -WhatIf
```

Orch1 テナント内のすべての古いプロセスバージョンを最新バージョンに更新します。問題がないことを確認したら、`-WhatIf` パラメーターを削除してコマンドを再実行してください。

### Example 2
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess
```

`Orch1:\Shared` フォルダー内の `MyProcess` プロセスのパッケージバージョンを最新バージョンに更新します。

### Example 3
```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess 1.0.3
```

`Orch1:\Shared` フォルダー内の `MyProcess` プロセスのパッケージバージョンをバージョン `1.0.3` に更新します。

### Example 4
```powershell
PS Orch1:\Shared> Update-OrchProcessVersion -Path Orch1:\Shared MyProcess
```

現在のフォルダー内の `MyProcess` プロセスのパッケージバージョンを最新バージョンに更新します。

## PARAMETERS

### -Depth
対象フォルダーへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダーは含まれません。

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
バージョンを更新するプロセスの名前を指定します。

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
対象フォルダーを指定します。指定されていない場合は、現在のフォルダーが対象になります。

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
操作に対象フォルダーとそのすべてのサブフォルダーを含めることを指定します。

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
プロセスを更新するバージョンを指定します。省略した場合、プロセスは最新バージョンに更新されます。

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
コマンドレットを実行する前に確認を求めます。

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
コマンドレットが実行された場合の動作を表示します。
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
このコマンドレットは、共通パラメータをサポートしています: -Debug、-ErrorAction、-ErrorVariable、-InformationAction、-InformationVariable、-OutVariable、-OutBuffer、-PipelineVariable、-Verbose、-WarningAction、および-WarningVariable。詳細については、[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216)を参照してください。

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
