---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchFolderMachine

## SYNOPSIS
フォルダマシン割り当てを宛先フォルダにコピーします。

## SYNTAX

```
Copy-OrchFolderMachine [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copy-OrchFolderMachine コマンドレットは、UiPath Orchestrator テナント内またはテナント間で、ソースフォルダから宛先フォルダにマシン割り当てをコピーします。このコマンドレットは、マシン対フォルダの関係を複製し、宛先フォルダで同じマシンが利用可能になることを保証します。

このコマンドレットは、テナント内コピー（同じテナント内）とテナント間コピー（異なるテナント間）の両方をサポートしています。これは、異なる環境間で一貫したマシン可用性を維持したり、並列フォルダ構造を設定したりするのに役立ちます。

-Name パラメーターを使用して割り当てをコピーするマシンを指定し、-Destination パラメーターを使用してターゲットフォルダを指定します。このコマンドレットは、マシン自体ではなく、マシンの割り当てをコピーします。

これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。-Recurse パラメーターを使用すると、すべてのサブフォルダからマシン割り当てをコピーし、宛先でフォルダ構造を維持します。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachinesForFolder(key={key}), POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth 必要スコープ: OR.Folders

必要な権限: Units.View, SubFolders.View, Units.Edit, SubFolders.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchFolderMachine Robot01 Orch1:\Production
```

現在のフォルダ（Development）から同じテナント内の Production フォルダに Robot01 マシン割り当てをコピーします。

### Example 2
```powershell
PS C:\> Copy-OrchFolderMachine -Path Orch1:\Development SharedBot Orch2:\Production
```

テナント間マシン割り当てコピーを実証して、Orch1:\Development から Orch2:\Production に SharedBot マシン割り当てをコピーします。

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchFolderMachine Robot*, Bot* Orch1:\Production -WhatIf
```

現在のフォルダから Production フォルダに Robot または Bot で始まる名前の複数のマシン割り当てをコピーする場合に何が起こるかを表示します。

### Example 4
```powershell
PS C:\> Copy-OrchFolderMachine -Path Orch1:\Development *Template* Orch2:\Production
```

ワイルドカードを使用して、Orch1:\Development から Orch2:\Production に名前に Template を含むすべてのマシン割り当てをコピーします。

### Example 5
```powershell
PS Orch1:\> Copy-OrchFolderMachine -Recurse *Production* Orch2:\Finance -WhatIf
```

すべてのサブフォルダから Production を含むすべてのマシン割り当てを再帰的に Orch2:\Finance にコピーする場合に何が起こるかを表示します。

### Example 6
```powershell
PS Orch1:\Development> Get-OrchFolderMachine *Shared* | Copy-OrchFolderMachine -Destination Orch2:\Production
```

名前に Shared を含むすべてのマシン割り当てを取得し、パイプライン入力を使用して Orch2:\Production にコピーします。

## PARAMETERS

### -Destination
マシン割り当てをコピーする宛先フォルダを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
割り当てをコピーするマシンの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
ソースフォルダを指定します。指定しない場合、現在のフォルダがソースとして使用されます。

```yaml
Type: String
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

### -Confirm
コマンドレットの実行前に確認を求めます。

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
コマンドレットを実行した場合に何が起こるかを表示します。
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

### -Depth
-Recurse パラメーターを使用する際に含めるサブフォルダレベルの最大数を指定します。

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

### -Recurse
マシン割り当てをすべてのサブフォルダから再帰的にコピーし、宛先でフォルダ構造を維持することを指定します。

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

### System.Object
## NOTES
これはフォルダエンティティコマンドレットです。まず Set-Location コマンドレット（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定してください。

このコマンドレットはマシン自体ではなく、マシンの割り当てをコピーします。テナント間でコピーする場合、マシンはターゲットテナントに既に存在している必要があります。

## RELATED LINKS

[Get-OrchFolderMachine](Get-OrchFolderMachine.md)

[Add-OrchFolderMachine](Add-OrchFolderMachine.md)

[Remove-OrchFolderMachine](Remove-OrchFolderMachine.md)
