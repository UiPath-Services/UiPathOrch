---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchFolderMachineAccountMapping

## SYNOPSIS
フォルダスコープマシンのマシンアカウントマッピングを無効にします。

## SYNTAX

```
Disable-OrchFolderMachineAccountMapping [-Name] <String[]> [[-UserName] <String[]>] [-Path <String[]>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Disable-OrchFolderMachineAccountMapping コマンドレットは、特定のフォルダ内でマシンとユーザーアカウント間のマッピングを無効にします。これにより、特定のユーザーがフォルダコンテキストでマシンに接続できるようにする関連付けが削除されます。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、最初に Set-Location (cd) を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定する必要があります。

アカウントマッピングが無効になると、ユーザーはフォルダコンテキストを通じて指定されたマシンへの専用接続を確立できなくなります。

主要エンドポイント: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId}), POST /odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots

OAuth 必要なスコープ: OR.Robots

必要な権限: Robots.Edit

## EXAMPLES

### Example 1
```powershell
Disable-OrchFolderMachineAccountMapping Machine01 -WhatIf
```

現在のフォルダでMachine01のすべてのアカウントマッピングを無効にした場合に何が起こるかを表示します。

### Example 2
```powershell
Disable-OrchFolderMachineAccountMapping Machine01 john.doe
```

現在のフォルダでMachine01とユーザーjohn.doe間のアカウントマッピングを無効にします。

### Example 3
```powershell
Disable-OrchFolderMachineAccountMapping *Dev* *test*
```

"Dev"を含むすべてのマシンと"test"を含むすべてのユーザーのアカウントマッピングを無効にします。

### Example 4
```powershell
Disable-OrchFolderMachineAccountMapping -Path Orch1:\Development Machine01, Machine02 -Confirm
```

確認を求めながら、DevelopmentフォルダでMachine01とMachine02のすべてのアカウントマッピングを無効にします。

### Example 5
```powershell
Disable-OrchFolderMachineAccountMapping -Recurse *Template*
```

現在のフォルダおよびすべてのサブフォルダで"Template"を含むすべてのマシンのアカウントマッピングを無効にします。

### Example 6
```powershell
Get-OrchMachine *Robot* | Disable-OrchFolderMachineAccountMapping -UserName admin.user
```

"Robot"を含むすべてのマシンとadmin.userアカウント間のアカウントマッピングを無効にします。マシン名はByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

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

### -Depth
ターゲットフォルダへの再帰の深さを指定します。深さが0の場合は、現在のフォルダのみが対象となり、サブフォルダは含まれません。

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
アカウントマッピングを無効にするマシンの名前を指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
ターゲットフォルダを指定します。指定しない場合、現在のフォルダがターゲットになります。

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
操作にターゲットフォルダとそのすべてのサブフォルダを含めることを指定します。

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

### -UserName
アカウントマッピングを無効にするユーザー名を指定します。指定しない場合、指定されたマシンのすべてのアカウントマッピングが無効になります。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -WhatIf
コマンドレットを実行した場合の動作を示します。
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

### None
## OUTPUTS

### System.Object
## NOTES
これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、最初に Set-Location (cd) を使用してターゲットフォルダに移動するか、-Path、-Recurse、または -Depth パラメーターを使用してターゲットフォルダを指定する必要があります。

マシンアカウントマッピングは、特定のユーザーがフォルダコンテキストを通じてマシンに接続できるようにします。マッピングを無効にすると、この関連付けが削除されます。効率的な一括操作にはワイルドカードを使用し、実際の実行前のテストには -WhatIf を使用してください。

## RELATED LINKS

[Enable-OrchFolderMachineAccountMapping](Enable-OrchFolderMachineAccountMapping.md)

[Get-OrchFolderMachineAccountMapping](Get-OrchFolderMachineAccountMapping.md)
