---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchFolderMachineAccountMapping

## SYNOPSIS
フォルダスコープのマシンに対してマシンアカウントマッピングを有効にします。

## SYNTAX

```
Enable-OrchFolderMachineAccountMapping [-Name] <String[]> [[-UserName] <String[]>] [-Path <String[]>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enable-OrchFolderMachineAccountMappingコマンドレットは、特定のフォルダ内でマシンとユーザーアカウント間のマッピングを有効にします。これは、特定のユーザーがフォルダのコンテキストでマシンに接続できるようにする関連付けを作成します。

これはフォルダエンティティコマンドレットです。このコマンドレットを使用するには、まずSet-Location（cd コマンド）を使用してターゲットフォルダに移動するか、-Path、-Recurse、または-Depthパラメーターを使用してターゲットフォルダを指定する必要があります。

アカウントマッピングが有効になると、指定されたユーザーは、フォルダコンテキストを通じて指定されたマシンに専用接続を確立でき、自動化リソースへの制御されたアクセスを提供します。

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetMachineRobots(folderId={folderId},machineId={machineId}), POST /odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots

OAuth required scopes: OR.Robots

Required permissions: Robots.Edit

## EXAMPLES

### Example 1
`powershell
Enable-OrchFolderMachineAccountMapping Machine01 john.doe -WhatIf
`

現在のフォルダでMachine01とユーザーjohn.doeの間のアカウントマッピングを有効にする場合の動作を表示します。

### Example 2
`powershell
Enable-OrchFolderMachineAccountMapping Machine01 john.doe
`

現在のフォルダでMachine01とユーザーjohn.doeの間のアカウントマッピングを有効にします。

### Example 3
`powershell
Enable-OrchFolderMachineAccountMapping *Prod* automation.user
`

"Prod"を含むすべてのマシンとautomation.userアカウントのアカウントマッピングを有効にします。

### Example 4
`powershell
Enable-OrchFolderMachineAccountMapping -Path Orch1:\Development Machine01, Machine02 developer1, developer2
`

DevelopmentフォルダでMachine01、Machine02のマシンとdeveloper1、developer2のユーザーの間のアカウントマッピングを有効にします。

### Example 5
`powershell
Enable-OrchFolderMachineAccountMapping -Recurse *Robot* service.account -Confirm
`

"Robot"を含むすべてのマシンとservice.accountの間のアカウントマッピングを現在のフォルダとすべてのサブフォルダで確認付きで有効にします。

### Example 6
`powershell
Get-OrchMachine -Status Available | Enable-OrchFolderMachineAccountMapping -UserName qa.tester
`

すべての利用可能なマシンとqa.testerアカウントの間のアカウントマッピングを有効にします。マシン名はByPropertyNameバインディングを使用してパイプライン経由で渡されます。

## PARAMETERS

### -Confirm
コマンドレットの実行前に確認メッセージを表示します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

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
ターゲットフォルダへの再帰の深度を指定します。深度0は現在の場所のみを示し、サブフォルダは含まれません。

`yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

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
アカウントマッピングを有効にするマシンの名前を指定します。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

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
ターゲットフォルダを指定します。指定されていない場合、現在のフォルダが対象になります。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

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
操作がターゲットフォルダとそのすべてのサブフォルダを含むことを指定します。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

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
アカウントマッピングを有効にするユーザー名を指定します。複数のユーザーを指定して、複数のマシンとのマッピングを作成できます。

`yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

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
コマンドレットが実行された場合に何が起こるかを表示します。
コマンドレットは実行されません。

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

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

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

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

## RELATED LINKS
